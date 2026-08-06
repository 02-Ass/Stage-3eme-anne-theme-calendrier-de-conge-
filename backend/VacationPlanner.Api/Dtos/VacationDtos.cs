namespace VacationPlanner.Api.Dtos;

public record VacationDto(
    int Id,
    int EmployeeId,
    DateOnly StartDate,
    DateOnly EndDate,
    int Days);

/// <summary>
/// Дата начала и дата окончания — обязательные, каноничные поля.
/// Количество дней на форме — производное значение, пересчитываемое на
/// фронтенде (см. правило в ТЗ п. "Таблица отпусков"), поэтому в запросе
/// на сервер оно не передаётся: сервер всегда вычисляет Days из дат.
/// </summary>
public record VacationUpsertRequest(
    int EmployeeId,
    DateOnly StartDate,
    DateOnly EndDate);
