using System.Net;
using System.Net.Http.Json;
using VacationPlanner.Api.Dtos;
using Xunit;

namespace VacationPlanner.Api.Tests.Integration;

public class EmployeesApiTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public EmployeesApiTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static EmployeeUpsertRequest NewEmployeeRequest(string name) => new(
        name,
        new DateOnly(2020, 1, 1),
        "#1677ff",
        new List<string> { "Проект X" },
        null,
        null);

    private async Task<EmployeeDto> CreateEmployeeAsync(string name)
    {
        var response = await _client.PostAsJsonAsync("/api/employees", NewEmployeeRequest(name));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EmployeeDto>())!;
    }

    [Fact]
    public async Task Create_ThenGetAll_ReturnsCreatedEmployee()
    {
        var created = await CreateEmployeeAsync("Тестов Т.Т.");

        Assert.Equal("Тестов Т.Т.", created.FullName);
        Assert.Contains("Проект X", created.Projects);

        var all = await _client.GetFromJsonAsync<List<EmployeeDto>>("/api/employees");
        Assert.Contains(all!, e => e.Id == created.Id);
    }

    [Fact]
    public async Task Update_ChangesFullName()
    {
        var created = await CreateEmployeeAsync("До изменения");

        var updateRequest = NewEmployeeRequest("После изменения");
        var updateResponse = await _client.PutAsJsonAsync($"/api/employees/{created.Id}", updateRequest);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var updated = await updateResponse.Content.ReadFromJsonAsync<EmployeeDto>();
        Assert.Equal("После изменения", updated!.FullName);
    }

    [Fact]
    public async Task Delete_RemovesEmployee()
    {
        var created = await CreateEmployeeAsync("На удаление");

        var deleteResponse = await _client.DeleteAsync($"/api/employees/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/employees/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Create_WithoutFullName_ReturnsBadRequest()
    {
        var invalidRequest = NewEmployeeRequest(string.Empty);

        var response = await _client.PostAsJsonAsync("/api/employees", invalidRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
