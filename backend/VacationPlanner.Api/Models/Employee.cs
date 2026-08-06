namespace VacationPlanner.Api.Models;

public class Employee
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public DateOnly HireDate { get; set; }

    /// <summary>Цвет отображения сотрудника в календаре, в формате HEX (#RRGGBB).</summary>
    public string Color { get; set; } = "#1677ff";

    /// <summary>
    /// Email для отправки напоминаний об отпуске. Необязательное поле —
    /// без него сотрудник просто не попадёт в рассылку.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Идентификатор пользователя Keycloak (claim "sub" в токене), который
    /// связывает запись сотрудника с его логином. Используется, чтобы
    /// сотрудник с обычной ролью видел/редактировал только свои отпуска.
    /// Заполняется вручную менеджером (Keycloak → Users → нужный
    /// пользователь → поле "ID" вверху страницы).
    /// </summary>
    public string? KeycloakUserId { get; set; }

    public ICollection<EmployeeProject> EmployeeProjects { get; set; } = new List<EmployeeProject>();

    public ICollection<Vacation> Vacations { get; set; } = new List<Vacation>();
}
