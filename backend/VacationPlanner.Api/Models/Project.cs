namespace VacationPlanner.Api.Models;

/// <summary>
/// Проект, на котором может быть занят один или несколько сотрудников.
/// Создаётся "по требованию", когда пользователь вводит новое имя проекта
/// в форме сотрудника — отдельного экрана управления проектами по ТЗ не предусмотрено.
/// </summary>
public class Project
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public ICollection<EmployeeProject> EmployeeProjects { get; set; } = new List<EmployeeProject>();
}
