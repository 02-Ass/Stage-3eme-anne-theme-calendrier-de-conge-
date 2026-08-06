namespace VacationPlanner.Api.Services;

/// <summary>
/// Чистое бизнес-правило без зависимостей от БД: пересекаются ли два
/// периода (границы включительно). Вынесено отдельно от
/// VacationsController, чтобы правило можно было протестировать
/// unit-тестом напрямую, без поднятия базы данных.
/// </summary>
public static class VacationOverlapChecker
{
    public static bool Overlaps(DateOnly aStart, DateOnly aEnd, DateOnly bStart, DateOnly bEnd) =>
        aStart <= bEnd && aEnd >= bStart;
}
