using System.Net;
using System.Net.Mail;

namespace VacationPlanner.Api.Services;

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body, CancellationToken ct = default);
}

/// <summary>
/// Отправляет письма через обычный SMTP (System.Net.Mail — часть .NET,
/// дополнительных NuGet-пакетов не требует). Настройки — в appsettings,
/// секция "Smtp". Если Smtp:Host пуст (по умолчанию), письма не
/// отправляются, а только логируются — это нормально для локальной
/// разработки без настроенной почты.
/// </summary>
public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        var host = _configuration["Smtp:Host"];
        if (string.IsNullOrWhiteSpace(host))
        {
            _logger.LogInformation(
                "Smtp:Host не задан — письмо для {To} не отправлено (только залогировано). Тема: {Subject}",
                to, subject);
            return;
        }

        var port = _configuration.GetValue<int?>("Smtp:Port") ?? 587;
        var username = _configuration["Smtp:Username"];
        var password = _configuration["Smtp:Password"];
        var from = _configuration["Smtp:From"];
        if (string.IsNullOrWhiteSpace(from))
        {
            from = string.IsNullOrWhiteSpace(username) ? "noreply@vacation-planner.local" : username;
        }
        var enableSsl = _configuration.GetValue<bool?>("Smtp:EnableSsl") ?? true;

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = enableSsl,
            Credentials = string.IsNullOrWhiteSpace(username) ? null : new NetworkCredential(username, password),
        };

        using var message = new MailMessage(from, to, subject, body);
        await client.SendMailAsync(message, ct);
    }
}
