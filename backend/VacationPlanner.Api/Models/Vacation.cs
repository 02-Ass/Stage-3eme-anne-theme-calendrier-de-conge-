using System.ComponentModel.DataAnnotations.Schema;

namespace VacationPlanner.Api.Models;

public class Vacation
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    /// <summary>
    /// Количество дней отпуска — не хранится в БД, а всегда выводится из дат
    /// начала/окончания (включительно). Это единственный источник истины:
    /// клиент присылает только даты, поле "дни" на форме — производное и
    /// пересчитывается на фронтенде по правилам ТЗ (см. utils/dateMath.ts).
    /// </summary>
    [NotMapped]
    public int Days => EndDate.DayNumber - StartDate.DayNumber + 1;
}
