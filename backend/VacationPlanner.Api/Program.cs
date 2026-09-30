using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using VacationPlanner.Api.Data;
using VacationPlanner.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// --- Сервисы ---------------------------------------------------------

builder.Services.AddControllers(options =>
{
    // Отключает авторизацию для всех запросов
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.Authorization.AllowAnonymousFilter());
});

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
    };
});

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Vacation Planner API",
        Version = "v1",
        Description = "API для планирования отпусков сотрудников отдела",
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")
                       ?? "Host=localhost;Port=5432;Database=VacationPlanner;Username=postgres;Password=12345"));

builder.Services.AddHttpContextAccessor();
// builder.Services.AddScoped<ICurrentUserContext, CurrentUserContext>(); // Старую строку комментируем
builder.Services.AddScoped<ICurrentUserContext>(_ => new FakeUserContext()); // Ставим заглушку

builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
builder.Services.AddHostedService<VacationReminderService>();

// ПРИНУДИТЕЛЬНО отключили авторизацию для запуска на новом ноутбуке
var authDisabled = true; 

builder.Services.AddAuthorization(options =>
{
    // Полностью открываем все стандартные политики
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAssertion(_ => true)
        .Build();

    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAssertion(_ => true)
        .Build();

    options.AddPolicy("Manager", policy => policy.RequireAssertion(ctx => true));
});

var allowedOrigin = builder.Configuration["Cors:AllowedOrigin"] ?? "http://localhost:5173";
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy => policy
        .WithOrigins(allowedOrigin, "http://127.0.0.1:5173") 
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var app = builder.Build();

// --- Миграции / создание БД при старте --------------------------------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (app.Configuration.GetValue<bool>("Database:UseMigrations"))
    {
        db.Database.Migrate();
    }
    else
    {
        db.Database.EnsureCreated();
    }
}

app.Logger.LogWarning("Auth:Disabled=true — авторизация ОТКЛЮЧЕНА强制. Только для локальной разработки!");

// --- Middleware pipeline -----------------------------------------------

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler();
}

app.UseCors("Frontend");

// Отключили проверку прав на уровне конвейера обработки запросов
// app.UseAuthentication();
// app.UseAuthorization();

app.MapControllers();

app.Run();

// Класс-заглушка для обхода пустой авторизации Keycloak
// Класс-заглушка для обхода пустой авторизации Keycloak
public class FakeUserContext : ICurrentUserContext
{
    public string? UserId => "123456"; 
    public string? KeycloakUserId => "123456"; // Добавили это свойство, чтобы исправить ошибку компиляции
    public string? Email => "sidikabdraman@yandex.ru";
    public string? FullName => "Азиз";
    public bool IsManager => true; 
}

public partial class Program
{
}
