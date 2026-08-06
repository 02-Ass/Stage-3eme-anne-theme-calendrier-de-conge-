using System.ComponentModel.DataAnnotations;

namespace VacationPlanner.Api.Dtos;

public record EmployeeDto(
    int Id,
    string FullName,
    DateOnly HireDate,
    string Color,
    List<string> Projects,
    string? Email,
    string? KeycloakUserId);

public record EmployeeUpsertRequest(
    [Required, MaxLength(200)] string FullName,
    DateOnly HireDate,
    [RegularExpression("^#[0-9A-Fa-f]{6}$")] string Color,
    List<string> Projects,
    [EmailAddress] string? Email,
    string? KeycloakUserId);
