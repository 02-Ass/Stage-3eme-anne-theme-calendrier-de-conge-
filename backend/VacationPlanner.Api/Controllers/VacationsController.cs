using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VacationPlanner.Api.Data;
using VacationPlanner.Api.Dtos;
using VacationPlanner.Api.Models;
using VacationPlanner.Api.Services;

namespace VacationPlanner.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VacationsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserContext _currentUser;

    public VacationsController(AppDbContext db, ICurrentUserContext currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Возвращает отпуска. Видно всем авторизованным пользователям
    /// независимо от роли — календарь должен показывать всех, чтобы
    /// были заметны пересечения. Ограничения по роли действуют только
    /// на создание/изменение/удаление (см. ниже).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<VacationDto>>> GetAll([FromQuery] int? year)
    {
        var query = _db.Vacations.AsQueryable();

        if (year is not null)
        {
            var yearStart = new DateOnly(year.Value, 1, 1);
            var yearEnd = new DateOnly(year.Value, 12, 31);
            query = query.Where(v => v.StartDate <= yearEnd && v.EndDate >= yearStart);
        }

        var vacations = await query.OrderBy(v => v.StartDate).ToListAsync();
        return Ok(vacations.Select(ToDto));
    }

    /// <summary>Отпуска, начинающиеся в ближайшие daysAhead дней — для колокольчика уведомлений.</summary>
    [HttpGet("upcoming")]
    public async Task<ActionResult<IEnumerable<VacationDto>>> GetUpcoming([FromQuery] int daysAhead = 14)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var horizon = today.AddDays(daysAhead);

        var vacations = await _db.Vacations
            .Where(v => v.StartDate >= today && v.StartDate <= horizon)
            .OrderBy(v => v.StartDate)
            .ToListAsync();

        return Ok(vacations.Select(ToDto));
    }

    /// <summary>Выгрузка отпусков за год в CSV (открывается в Excel). Только для менеджеров.</summary>
    [HttpGet("export")]
    [Authorize(Policy = "Manager")]
    public async Task<IActionResult> Export([FromQuery] int? year)
    {
        var yearValue = year ?? DateTime.Now.Year;
        var yearStart = new DateOnly(yearValue, 1, 1);
        var yearEnd = new DateOnly(yearValue, 12, 31);

        var vacations = await _db.Vacations
            .Include(v => v.Employee)
            .Where(v => v.StartDate <= yearEnd && v.EndDate >= yearStart)
            .OrderBy(v => v.Employee.FullName).ThenBy(v => v.StartDate)
            .ToListAsync();

        var csv = new StringBuilder();
        csv.AppendLine("Сотрудник;Дата начала;Дата окончания;Дней");
        foreach (var v in vacations)
        {
            csv.AppendLine(
                $"{EscapeCsv(v.Employee.FullName)};{v.StartDate:dd.MM.yyyy};{v.EndDate:dd.MM.yyyy};{v.Days}");
        }

        // GetBytes() сам по себе НЕ добавляет BOM (флаг true у UTF8Encoding
        // влияет только на StreamWriter) — дописываем байты BOM вручную,
        // иначе Excel открывает файл в своей кодировке и кириллица превращается
        // в "кракозябры".
        var utf8 = new UTF8Encoding(true);
        var bytes = utf8.GetPreamble().Concat(utf8.GetBytes(csv.ToString())).ToArray();
        return File(bytes, "text/csv", $"vacations_{yearValue}.csv");
    }

    [HttpPost]
    public async Task<ActionResult<VacationDto>> Create(VacationUpsertRequest request)
    {
        if (request.EndDate < request.StartDate)
        {
            return BadRequest("Дата окончания не может быть раньше даты начала.");
        }

        var employeeExists = await _db.Employees.AnyAsync(e => e.Id == request.EmployeeId);
        if (!employeeExists)
        {
            return BadRequest("Сотрудник не найден.");
        }

        if (!_currentUser.IsManager)
        {
            var myEmployeeId = await GetMyEmployeeIdAsync();
            if (myEmployeeId != request.EmployeeId)
            {
                return Forbid();
            }
        }

        if (await HasOverlapAsync(request.EmployeeId, request.StartDate, request.EndDate))
        {
            return BadRequest("У сотрудника уже есть отпуск, пересекающийся с этими датами.");
        }

        var vacation = new Vacation
        {
            EmployeeId = request.EmployeeId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
        };

        _db.Vacations.Add(vacation);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), new { }, ToDto(vacation));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<VacationDto>> Update(int id, VacationUpsertRequest request)
    {
        if (request.EndDate < request.StartDate)
        {
            return BadRequest("Дата окончания не может быть раньше даты начала.");
        }

        var vacation = await _db.Vacations.FindAsync(id);
        if (vacation is null)
        {
            return NotFound();
        }

        if (!_currentUser.IsManager)
        {
            var myEmployeeId = await GetMyEmployeeIdAsync();
            if (myEmployeeId != vacation.EmployeeId || myEmployeeId != request.EmployeeId)
            {
                return Forbid();
            }
        }

        if (await HasOverlapAsync(request.EmployeeId, request.StartDate, request.EndDate, excludeVacationId: id))
        {
            return BadRequest("У сотрудника уже есть отпуск, пересекающийся с этими датами.");
        }

        vacation.StartDate = request.StartDate;
        vacation.EndDate = request.EndDate;
        await _db.SaveChangesAsync();

        return Ok(ToDto(vacation));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var vacation = await _db.Vacations.FindAsync(id);
        if (vacation is null)
        {
            return NotFound();
        }

        if (!_currentUser.IsManager)
        {
            var myEmployeeId = await GetMyEmployeeIdAsync();
            if (myEmployeeId != vacation.EmployeeId)
            {
                return Forbid();
            }
        }

        _db.Vacations.Remove(vacation);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Проверяет, пересекается ли [start; end] с уже существующими
    /// отпусками ЭТОГО ЖЕ сотрудника (excludeVacationId — чтобы при
    /// редактировании запись не конфликтовала сама с собой). Само
    /// правило пересечения — в VacationOverlapChecker (переиспользуется
    /// в unit-тестах без обращения к БД).
    /// </summary>
    private async Task<bool> HasOverlapAsync(int employeeId, DateOnly start, DateOnly end, int? excludeVacationId = null)
    {
        var employeeVacations = await _db.Vacations
            .Where(v => v.EmployeeId == employeeId)
            .Select(v => new { v.Id, v.StartDate, v.EndDate })
            .ToListAsync();

        return employeeVacations.Any(v =>
            (excludeVacationId is null || v.Id != excludeVacationId.Value) &&
            VacationOverlapChecker.Overlaps(v.StartDate, v.EndDate, start, end));
    }

    private async Task<int?> GetMyEmployeeIdAsync()
    {
        if (_currentUser.KeycloakUserId is null)
        {
            return null;
        }

        return await _db.Employees
            .Where(e => e.KeycloakUserId == _currentUser.KeycloakUserId)
            .Select(e => (int?)e.Id)
            .FirstOrDefaultAsync();
    }

    private static string EscapeCsv(string value) =>
        value.Contains(';') || value.Contains('"')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;

    private static VacationDto ToDto(Vacation v) => new(v.Id, v.EmployeeId, v.StartDate, v.EndDate, v.Days);
}
