using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using VacationPlanner.Api.Data;
using VacationPlanner.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// --- Сервисы ---------------------------------------------------------

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Единый формат ошибок (RFC 9457 ProblemDetails) — им уже пользуется
// [ApiController] для ошибок валидации (400); подключаем и для
// необработанных исключений (500), см. UseExceptionHandler() ниже.
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
                       ?? "Host=localhost;Port=5432;Database=vacationplanner;Username=vacationplanner;Password=vacationplanner"));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserContext, CurrentUserContext>();
builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
builder.Services.AddHostedService<VacationReminderService>();

// Аутентификация через OpenID Connect (Keycloak). Frontend получает
// access_token у Keycloak и передаёт его в заголовке Authorization,
// а API проверяет подпись/издателя по метаданным Keycloak (JWKS).
//
// Auth:Disabled — временный переключатель для локальной разработки без
// настроенного Keycloak: если true, JWT-проверка не регистрируется, а
// политика авторизации по умолчанию пропускает всех. В appsettings.json
// (базовый, "боевой" конфиг) стоит false; в appsettings.Development.json
// стоит true, чтобы `dotnet run` сразу работал без Keycloak. Перед
// показом/сдачей с реальной авторизацией — выставить false.
var authDisabled = builder.Configuration.GetValue<bool>("Auth:Disabled");

if (!authDisabled)
{
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = builder.Configuration["Auth:Authority"];
            options.Audience = builder.Configuration["Auth:Audience"];
            options.RequireHttpsMetadata = builder.Configuration.GetValue<bool>("Auth:RequireHttpsMetadata");

            // Сохраняем оригинальные имена клеймов ("sub", "realm_access" и т.д.),
            // а не переименовываем их по стандартной .NET-таблице соответствий.
            options.MapInboundClaims = false;

            options.Events = new JwtBearerEvents
            {
                // Keycloak кладёт роли в realm_access.roles (вложенный JSON), а не
                // в отдельный плоский claim "role" — стандартный обработчик такое
                // не разбирает. Разбираем вручную и добавляем как Role-клеймы,
                // чтобы заработали [Authorize(Roles = "...")] / User.IsInRole(...).
                OnTokenValidated = context =>
                {
                    var principal = context.Principal;
                    var realmAccessJson = principal?.FindFirst("realm_access")?.Value;

                    if (principal?.Identity is System.Security.Claims.ClaimsIdentity identity
                        && !string.IsNullOrEmpty(realmAccessJson))
                    {
                        try
                        {
                            using var doc = System.Text.Json.JsonDocument.Parse(realmAccessJson);
                            if (doc.RootElement.TryGetProperty("roles", out var roles))
                            {
                                foreach (var role in roles.EnumerateArray())
                                {
                                    var roleName = role.GetString();
                                    if (!string.IsNullOrEmpty(roleName))
                                    {
                                        identity.AddClaim(new System.Security.Claims.Claim(
                                            System.Security.Claims.ClaimTypes.Role, roleName));
                                    }
                                }
                            }
                        }
                        catch (System.Text.Json.JsonException)
                        {
                            // realm_access пришёл не в ожидаемом формате — просто
                            // пропускаем, роли останутся пустыми.
                        }
                    }

                    return Task.CompletedTask;
                },
            };
        });
}

builder.Services.AddAuthorization(options =>
{
    if (authDisabled)
    {
        options.DefaultPolicy = new AuthorizationPolicyBuilder()
            .RequireAssertion(_ => true)
            .Build();
    }

    // При отключённой авторизации (локальная разработка) считаем всех
    // менеджерами — иначе роль-специфичные экраны просто нечем было бы
    // проверять без настроенного Keycloak.
    options.AddPolicy("Manager", policy => policy.RequireAssertion(ctx =>
        authDisabled || ctx.User.IsInRole("manager")));
});

var allowedOrigin = builder.Configuration["Cors:AllowedOrigin"] ?? "http://localhost:5173";
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy => policy
        .WithOrigins(allowedOrigin)
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var app = builder.Build();

// --- Миграции / создание БД при старте --------------------------------
// Database:UseMigrations=false (по умолчанию) — EnsureCreated(),
// мгновенный старт "из коробки", без отдельного шага миграций.
// Database:UseMigrations=true — полноценные EF Core Migrations
// (db.Database.Migrate()). Чтобы включить: один раз сгенерировать
// миграцию (dotnet ef migrations add InitialCreate — см. README,
// раздел "EF Core Migrations"), затем выставить флаг в true.
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

if (authDisabled)
{
    app.Logger.LogWarning("Auth:Disabled=true — авторизация ОТКЛЮЧЕНА. Только для локальной разработки, не для боевого использования!");
}

// --- Middleware pipeline -----------------------------------------------

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // В Development оставляем стандартную подробную страницу ошибок
    // (удобно при отладке); вне Development — единый ProblemDetails-ответ
    // для любого необработанного исключения, без утечки деталей/стектрейса.
    app.UseExceptionHandler();
}

app.UseCors("Frontend");
if (!authDisabled)
{
    app.UseAuthentication();
}
app.UseAuthorization();
app.MapControllers();

app.Run();

// Точка входа генерируется компилятором как internal — открываем её,
// чтобы тестовый проект мог подключить WebApplicationFactory<Program>
// (см. VacationPlanner.Api.Tests/Integration).
public partial class Program
{
}
