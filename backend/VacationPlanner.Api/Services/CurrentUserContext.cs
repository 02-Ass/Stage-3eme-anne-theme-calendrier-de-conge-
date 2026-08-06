using Microsoft.AspNetCore.Http;

namespace VacationPlanner.Api.Services;

/// <summary>
/// Информация о текущем пользователе запроса: менеджер он или обычный
/// сотрудник, и его идентификатор Keycloak (claim "sub"), по которому
/// контроллеры находят связанную запись Employee.
/// </summary>
public interface ICurrentUserContext
{
    bool IsManager { get; }
    string? KeycloakUserId { get; }
}

public class CurrentUserContext : ICurrentUserContext
{
    public CurrentUserContext(IHttpContextAccessor accessor, IConfiguration configuration)
    {
        // Если авторизация отключена (Auth:Disabled=true, локальная разработка
        // без Keycloak) — считаем пользователя менеджером, чтобы весь
        // функционал был доступен без лишних препятствий.
        var authDisabled = configuration.GetValue<bool>("Auth:Disabled");
        if (authDisabled)
        {
            IsManager = true;
            KeycloakUserId = null;
            return;
        }

        var user = accessor.HttpContext?.User;
        IsManager = user?.IsInRole("manager") ?? false;
        KeycloakUserId = user?.FindFirst("sub")?.Value;
    }

    public bool IsManager { get; }
    public string? KeycloakUserId { get; }
}
