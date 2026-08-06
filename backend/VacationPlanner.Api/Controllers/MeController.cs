using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VacationPlanner.Api.Data;
using VacationPlanner.Api.Dtos;
using VacationPlanner.Api.Services;

namespace VacationPlanner.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MeController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserContext _currentUser;

    public MeController(AppDbContext db, ICurrentUserContext currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<MeDto>> Get()
    {
        int? employeeId = null;
        if (_currentUser.KeycloakUserId is not null)
        {
            employeeId = await _db.Employees
                .Where(e => e.KeycloakUserId == _currentUser.KeycloakUserId)
                .Select(e => (int?)e.Id)
                .FirstOrDefaultAsync();
        }

        return Ok(new MeDto(_currentUser.IsManager, employeeId));
    }
}
