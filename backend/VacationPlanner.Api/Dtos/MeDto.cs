namespace VacationPlanner.Api.Dtos;

/// <summary>
/// Информация о текущем пользователе: его роль и (если найдена) связанная
/// запись сотрудника. Фронтенд использует это, чтобы скрывать
/// действия, недоступные обычному сотруднику (не-менеджеру).
/// </summary>
public record MeDto(bool IsManager, int? EmployeeId);
