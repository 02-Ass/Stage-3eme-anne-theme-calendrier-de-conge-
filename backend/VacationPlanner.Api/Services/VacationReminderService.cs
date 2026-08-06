using Microsoft.EntityFrameworkCore;
using VacationPlanner.Api.Data;

namespace VacationPlanner.Api.Services;

/// <summary>
/// Раз в сутки (настраивается) проверяет, у кого отпуск начинается
/// ровно через Notifications:ReminderDaysBefore дней, и отправляет
/// письмо-напоминание. Если у сотрудника не указан email — просто
/// пропускается, ошибки это не вызывает.
/// </summary>
public class VacationReminderService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly IConfiguration _configuration;
    private readonly ILogger<VacationReminderService> _logger;

    public VacationReminderService(
        IServiceProvider services,
        IConfiguration configuration,
        ILogger<VacationReminderService> logger)
    {
        _services = services;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var daysBefore = _configuration.GetValue<int?>("Notifications:ReminderDaysBefore") ?? 3;
        var checkIntervalHours = _configuration.GetValue<double?>("Notifications:CheckIntervalHours") ?? 24;
        var checkInterval = TimeSpan.FromHours(checkIntervalHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SendRemindersAsync(daysBefore, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отправке напоминаний об отпусках");
            }

            try
            {
                await Task.Delay(checkInterval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // приложение останавливается — это ожидаемо
            }
        }
    }

    private async Task SendRemindersAsync(int daysBefore, CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

        var targetDate = DateOnly.FromDateTime(DateTime.Today.AddDays(daysBefore));

        var vacations = await db.Vacations
            .Include(v => v.Employee)
            .Where(v => v.StartDate == targetDate && v.Employee.Email != null)
            .ToListAsync(ct);

        foreach (var vacation in vacations)
        {
            var subject = "Скоро отпуск";
            var body =
                $"Здравствуйте, {vacation.Employee.FullName}!\n\n" +
                $"Напоминаем: ваш отпуск начинается {vacation.StartDate:dd.MM.yyyy} " +
                $"и продлится {vacation.Days} дн. (до {vacation.EndDate:dd.MM.yyyy}).";

            await emailSender.SendAsync(vacation.Employee.Email!, subject, body, ct);
        }

        if (vacations.Count > 0)
        {
            _logger.LogInformation("Отправлено напоминаний об отпуске: {Count}", vacations.Count);
        }
    }
}
