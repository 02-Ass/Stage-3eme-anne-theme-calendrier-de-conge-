using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VacationPlanner.Api.Data;
using VacationPlanner.Api.Dtos;

namespace VacationPlanner.Api.Controllers;

/// <summary>
/// Только для чтения: список проектов используется для автодополнения
/// в поле "Проекты" формы сотрудника. Управление проектами отдельным
/// экраном не предусмотрено ТЗ — проекты создаются автоматически.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProjectsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ProjectsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProjectDto>>> GetAll()
    {
        var projects = await _db.Projects
            .OrderBy(p => p.Name)
            .Select(p => new ProjectDto(p.Id, p.Name))
            .ToListAsync();

        return Ok(projects);
    }
}
