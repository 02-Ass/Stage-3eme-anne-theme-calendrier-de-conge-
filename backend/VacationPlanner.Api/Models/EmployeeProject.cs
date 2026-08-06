namespace VacationPlanner.Api.Models;

/// <summary>Связь "многие-ко-многим" между сотрудниками и проектами.</summary>
public class EmployeeProject
{
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;
}
