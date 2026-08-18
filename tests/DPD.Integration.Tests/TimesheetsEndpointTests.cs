using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace DPD.Integration.Tests;

/// <summary>
/// Couvre le workflow Time Tracking (Submit -> Approve/Reject -> Lock) et le RBAC associé de bout en bout.
/// </summary>
public class TimesheetsEndpointTests : IClassFixture<DpdWebApplicationFactory>
{
    private readonly DpdWebApplicationFactory _factory;

    public TimesheetsEndpointTests(DpdWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<(Guid ResourceId, Guid StudyId)> GetReferenceIdsAsync(HttpClient client)
    {
        var resources = await client.GetFromJsonAsync<List<IdNameDto>>("/api/reference/resources");
        var studies = await client.GetFromJsonAsync<List<IdNameDto>>("/api/reference/studies");
        return (resources!.First().Id, studies!.First().Id);
    }

    [Fact]
    public async Task SubmitTimesheet_AsScientist_Returns201()
    {
        var client = _factory.CreateClientWithRole("Scientist");
        var (resourceId, studyId) = await GetReferenceIdsAsync(client);

        var response = await client.PostAsJsonAsync("/api/timesheets", new
        {
            ResourceId = resourceId,
            StudyId = studyId,
            WorkDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Hours = 6.5m
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task SubmitTimesheet_AsReadOnly_Returns403()
    {
        var client = _factory.CreateClientWithRole("ReadOnly");
        var (resourceId, studyId) = await GetReferenceIdsAsync(client);

        var response = await client.PostAsJsonAsync("/api/timesheets", new
        {
            ResourceId = resourceId,
            StudyId = studyId,
            WorkDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Hours = 6.5m
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ApproveThenLockTimesheet_FullWorkflow_Succeeds()
    {
        var scientistClient = _factory.CreateClientWithRole("Scientist");
        var (resourceId, studyId) = await GetReferenceIdsAsync(scientistClient);

        var submitResponse = await scientistClient.PostAsJsonAsync("/api/timesheets", new
        {
            ResourceId = resourceId,
            StudyId = studyId,
            WorkDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Hours = 4m
        });
        var timeEntryId = await submitResponse.Content.ReadFromJsonAsync<Guid>();

        var managerClient = _factory.CreateClientWithRole("Manager");
        var approveResponse = await managerClient.PatchAsJsonAsync($"/api/timesheets/{timeEntryId}/approve", new { ApproverId = resourceId });
        approveResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var adminClient = _factory.CreateClientWithRole("Admin");
        var lockResponse = await adminClient.PatchAsync($"/api/timesheets/{timeEntryId}/lock", null);
        lockResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task RejectTimesheet_WithoutComment_Returns422()
    {
        var scientistClient = _factory.CreateClientWithRole("Scientist");
        var (resourceId, studyId) = await GetReferenceIdsAsync(scientistClient);

        var submitResponse = await scientistClient.PostAsJsonAsync("/api/timesheets", new
        {
            ResourceId = resourceId,
            StudyId = studyId,
            WorkDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Hours = 3m
        });
        var timeEntryId = await submitResponse.Content.ReadFromJsonAsync<Guid>();

        var managerClient = _factory.CreateClientWithRole("Manager");
        var rejectResponse = await managerClient.PatchAsJsonAsync($"/api/timesheets/{timeEntryId}/reject", new { ApproverId = resourceId, Comment = "" });

        rejectResponse.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task LockTimesheet_AsManager_Returns403BecauseAdminOnly()
    {
        var scientistClient = _factory.CreateClientWithRole("Scientist");
        var (resourceId, studyId) = await GetReferenceIdsAsync(scientistClient);

        var submitResponse = await scientistClient.PostAsJsonAsync("/api/timesheets", new
        {
            ResourceId = resourceId,
            StudyId = studyId,
            WorkDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Hours = 2m
        });
        var timeEntryId = await submitResponse.Content.ReadFromJsonAsync<Guid>();

        var managerClient = _factory.CreateClientWithRole("Manager");
        var lockResponse = await managerClient.PatchAsync($"/api/timesheets/{timeEntryId}/lock", null);

        lockResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private sealed record IdNameDto(Guid Id, string Name);
}
