using System.Net;
using System.Net.Http.Json;
using VacationPlanner.Api.Dtos;
using Xunit;

namespace VacationPlanner.Api.Tests.Integration;

public class VacationsApiTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public VacationsApiTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<int> CreateEmployeeAsync(string name)
    {
        var request = new EmployeeUpsertRequest(name, new DateOnly(2020, 1, 1), "#1677ff", new List<string>(), null, null);
        var response = await _client.PostAsJsonAsync("/api/employees", request);
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<EmployeeDto>();
        return dto!.Id;
    }

    [Fact]
    public async Task Create_ValidVacation_ReturnsCreatedWithCorrectDays()
    {
        var employeeId = await CreateEmployeeAsync("Отпускник Один");

        var request = new VacationUpsertRequest(employeeId, new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 10));
        var response = await _client.PostAsJsonAsync("/api/vacations", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<VacationDto>();
        Assert.Equal(10, created!.Days); // 1–10 июля включительно = 10 дней
    }

    [Fact]
    public async Task Create_EndBeforeStart_ReturnsBadRequest()
    {
        var employeeId = await CreateEmployeeAsync("Отпускник Два");

        var request = new VacationUpsertRequest(employeeId, new DateOnly(2026, 7, 10), new DateOnly(2026, 7, 1));
        var response = await _client.PostAsJsonAsync("/api/vacations", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_OverlappingWithExistingVacationOfSameEmployee_ReturnsBadRequest()
    {
        var employeeId = await CreateEmployeeAsync("Отпускник Три");

        var first = new VacationUpsertRequest(employeeId, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 10));
        var firstResponse = await _client.PostAsJsonAsync("/api/vacations", first);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        // Пересекается с первым: 5–15 августа против уже занятых 1–10.
        var overlapping = new VacationUpsertRequest(employeeId, new DateOnly(2026, 8, 5), new DateOnly(2026, 8, 15));
        var overlappingResponse = await _client.PostAsJsonAsync("/api/vacations", overlapping);

        Assert.Equal(HttpStatusCode.BadRequest, overlappingResponse.StatusCode);
    }

    [Fact]
    public async Task Create_NonOverlappingVacationsForSameEmployee_BothSucceed()
    {
        var employeeId = await CreateEmployeeAsync("Отпускник Четыре");

        var first = new VacationUpsertRequest(employeeId, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 10));
        var firstResponse = await _client.PostAsJsonAsync("/api/vacations", first);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var second = new VacationUpsertRequest(employeeId, new DateOnly(2026, 9, 20), new DateOnly(2026, 9, 25));
        var secondResponse = await _client.PostAsJsonAsync("/api/vacations", second);
        Assert.Equal(HttpStatusCode.Created, secondResponse.StatusCode);
    }

    [Fact]
    public async Task Update_ToOverlapWithAnotherVacation_ReturnsBadRequest()
    {
        var employeeId = await CreateEmployeeAsync("Отпускник Пять");

        var first = new VacationUpsertRequest(employeeId, new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 5));
        (await _client.PostAsJsonAsync("/api/vacations", first)).EnsureSuccessStatusCode();

        var second = new VacationUpsertRequest(employeeId, new DateOnly(2026, 10, 20), new DateOnly(2026, 10, 25));
        var secondResponse = await _client.PostAsJsonAsync("/api/vacations", second);
        var secondCreated = await secondResponse.Content.ReadFromJsonAsync<VacationDto>();

        // Пытаемся подвинуть второй отпуск так, чтобы он пересёкся с первым.
        var updateRequest = new VacationUpsertRequest(employeeId, new DateOnly(2026, 10, 3), new DateOnly(2026, 10, 8));
        var updateResponse = await _client.PutAsJsonAsync($"/api/vacations/{secondCreated!.Id}", updateRequest);

        Assert.Equal(HttpStatusCode.BadRequest, updateResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_RemovesVacation()
    {
        var employeeId = await CreateEmployeeAsync("Отпускник Шесть");

        var request = new VacationUpsertRequest(employeeId, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 5));
        var createResponse = await _client.PostAsJsonAsync("/api/vacations", request);
        var created = await createResponse.Content.ReadFromJsonAsync<VacationDto>();

        var deleteResponse = await _client.DeleteAsync($"/api/vacations/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var all = await _client.GetFromJsonAsync<List<VacationDto>>("/api/vacations");
        Assert.DoesNotContain(all!, v => v.Id == created.Id);
    }
}
