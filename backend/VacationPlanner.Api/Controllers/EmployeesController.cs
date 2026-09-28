using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VacationPlanner.Api.Data;
using VacationPlanner.Api.Dtos;
using VacationPlanner.Api.Models;

namespace VacationPlanner.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous] // ДОБАВИЛИ СЮДА: Отключает любую авторизацию для всего контроллера сотрудников
public class EmployeesController : ControllerBase
{
    private readonly AppDbContext _db;

    public EmployeesController(AppDbContext db)
    {
        _db = db;
    }


    [HttpGet]
    public async Task<ActionResult<IEnumerable<EmployeeDto>>> GetAll()
    {
        var employees = await _db.Employees
            .Include(e => e.EmployeeProjects)
            .ThenInclude(ep => ep.Project)
            .OrderBy(e => e.FullName)
            .ToListAsync();

        return Ok(employees.Select(ToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EmployeeDto>> GetById(int id)
    {
        var employee = await _db.Employees
            .Include(e => e.EmployeeProjects)
            .ThenInclude(ep => ep.Project)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee is null)
        {
            return NotFound();
        }

        return Ok(ToDto(employee));
    }

    [HttpPost]
    [Authorize(Policy = "Manager")]
    public async Task<ActionResult<EmployeeDto>> Create(EmployeeUpsertRequest request)
    {
        var employee = new Employee
        {
            FullName = request.FullName.Trim(),
            HireDate = request.HireDate,
            Color = request.Color,
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            KeycloakUserId = string.IsNullOrWhiteSpace(request.KeycloakUserId) ? null : request.KeycloakUserId.Trim(),
        };

        await SyncProjectsAsync(employee, request.Projects);

        _db.Employees.Add(employee);
        await _db.SaveChangesAsync();

        // перечитываем с уже подгруженными проектами для корректного ответа
        employee = await _db.Employees
            .Include(e => e.EmployeeProjects).ThenInclude(ep => ep.Project)
            .FirstAsync(e => e.Id == employee.Id);

        return CreatedAtAction(nameof(GetById), new { id = employee.Id }, ToDto(employee));
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "Manager")]
    public async Task<ActionResult<EmployeeDto>> Update(int id, EmployeeUpsertRequest request)
    {
        var employee = await _db.Employees
            .Include(e => e.EmployeeProjects)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee is null)
        {
            return NotFound();
        }

        employee.FullName = request.FullName.Trim();
        employee.HireDate = request.HireDate;
        employee.Color = request.Color;
        employee.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        employee.KeycloakUserId = string.IsNullOrWhiteSpace(request.KeycloakUserId) ? null : request.KeycloakUserId.Trim();

        _db.EmployeeProjects.RemoveRange(employee.EmployeeProjects);
        employee.EmployeeProjects.Clear();
        await SyncProjectsAsync(employee, request.Projects);

        await _db.SaveChangesAsync();

        employee = await _db.Employees
            .Include(e => e.EmployeeProjects).ThenInclude(ep => ep.Project)
            .FirstAsync(e => e.Id == id);

        return Ok(ToDto(employee));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "Manager")]
    public async Task<IActionResult> Delete(int id)
    {
        var employee = await _db.Employees.FindAsync(id);
        if (employee is null)
        {
            return NotFound();
        }

        // Каскадное удаление настроено в AppDbContext: вместе с сотрудником
        // удаляются его связи с проектами и все его отпуска.
        _db.Employees.Remove(employee);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private async Task SyncProjectsAsync(Employee employee, List<string> projectNames)
    {
        var distinctNames = projectNames
            .Select(n => n.Trim())
            .Where(n => n.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var name in distinctNames)
        {
            var project = await _db.Projects
                .FirstOrDefaultAsync(p => p.Name.ToLower() == name.ToLower());

            if (project is null)
            {
                project = new Project { Name = name };
                _db.Projects.Add(project);
            }

            employee.EmployeeProjects.Add(new EmployeeProject
            {
                Employee = employee,
                Project = project,
            });
        }
    }

    private static EmployeeDto ToDto(Employee e) => new(
        e.Id,
        e.FullName,
        e.HireDate,
        e.Color,
        e.EmployeeProjects.Select(ep => ep.Project.Name).OrderBy(n => n).ToList(),
        e.Email,
        e.KeycloakUserId);
}
