using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VacationPlanner.Api.Data;

namespace VacationPlanner.Api.Tests.Integration;

/// <summary>
/// Поднимает приложение целиком (как настоящий HTTP-сервер) для
/// интеграционных тестов, но с двумя подменами:
///  - PostgreSQL заменён на EF Core InMemory (не нужен реальный Postgres);
///  - Auth:Disabled=true (не нужен настоящий Keycloak/токен) — тот же
///    флаг, что используется для локальной разработки без Keycloak.
/// Каждый экземпляр фабрики получает свою уникальную базу (по имени),
/// поэтому тестовые классы не мешают друг другу.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    public string DatabaseName { get; } = Guid.NewGuid().ToString();

    public ApiFactory()
    {
        // Program.cs (top-level statements, минимальный хостинг) читает
        // Auth:Disabled ещё ДО того, как отрабатывает ConfigureWebHost/
        // ConfigureAppConfiguration тестовой фабрики — та настройка
        // применяется слишком поздно и не долетает вовремя. Переменные
        // окружения процесса читаются WebApplication.CreateBuilder(args)
        // всегда одними из первых, поэтому именно так — надёжно.
        Environment.SetEnvironmentVariable("Auth__Disabled", "true");
        Environment.SetEnvironmentVariable("Database__UseMigrations", "false");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(DatabaseName));
        });
    }
}