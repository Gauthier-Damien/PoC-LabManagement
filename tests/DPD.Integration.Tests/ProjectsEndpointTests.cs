using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace DPD.Integration.Tests;

/// <summary>
/// Valide le RBAC de bout en bout (Web Authorization Policies + Application AuthorizationBehaviour)
/// et le cycle de vie complet d'un projet (création + transitions de statut).
/// </summary>
public class ProjectsEndpointTests : IClassFixture<DpdWebApplicationFactory>
{
    private readonly DpdWebApplicationFactory _factory;

    public ProjectsEndpointTests(DpdWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static async Task<Guid> GetFirstResourceIdAsync(HttpClient client)
    {
        var resources = await client.GetFromJsonAsync<List<IdNameDto>>("/api/reference/resources");
        return resources![0].Id;
    }

    [Fact]
    public async Task CreateProject_AsScientist_Returns403Forbidden()
    {
        var client = _factory.CreateClientWithRole("Scientist");
        var managerId = await GetFirstResourceIdAsync(client);

        var response = await client.PostAsJsonAsync("/api/projects", new
        {
            Name = "Unauthorized Project",
            Code = $"NP-{Guid.NewGuid():N}"[..10],
            ManagerId = managerId,
            EstimatedMd = 50
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateProject_AsSd_Returns201AndProjectIsRetrievable()
    {
        var client = _factory.CreateClientWithRole("SD");
        var managerId = await GetFirstResourceIdAsync(client);
        var code = $"SD{Guid.NewGuid():N}"[..8].ToUpperInvariant();

        var createResponse = await client.PostAsJsonAsync("/api/projects", new
        {
            Name = "SD Managed Project",
            Code = code,
            ManagerId = managerId,
            EstimatedMd = 80
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var projectId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var getResponse = await client.GetAsync($"/api/projects/{projectId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await getResponse.Content.ReadFromJsonAsync<ProjectDetailDto>();
        detail!.Code.Should().Be(code);
        detail.Status.Should().Be("Hypothesis");
        detail.AllowedNextStatuses.Should().BeEquivalentTo("Quoted", "Cancelled");
    }

    [Fact]
    public async Task ChangeProjectStatus_ValidTransition_Returns204()
    {
        var client = _factory.CreateClientWithRole("Manager");
        var managerId = await GetFirstResourceIdAsync(client);
        var code = $"MG{Guid.NewGuid():N}"[..8].ToUpperInvariant();

        var createResponse = await client.PostAsJsonAsync("/api/projects", new
        {
            Name = "Manager Project",
            Code = code,
            ManagerId = managerId,
            EstimatedMd = 60
        });
        var projectId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var statusResponse = await client.PatchAsJsonAsync($"/api/projects/{projectId}/status", new { Status = "Quoted" });

        statusResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ChangeProjectStatus_InvalidTransition_Returns422()
    {
        var client = _factory.CreateClientWithRole("Admin");
        var managerId = await GetFirstResourceIdAsync(client);
        var code = $"AD{Guid.NewGuid():N}"[..8].ToUpperInvariant();

        var createResponse = await client.PostAsJsonAsync("/api/projects", new
        {
            Name = "Admin Project",
            Code = code,
            ManagerId = managerId,
            EstimatedMd = 60
        });
        var projectId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        // Hypothesis -> OnGoing est une transition invalide (doit passer par Quoted puis Signed).
        var statusResponse = await client.PatchAsJsonAsync($"/api/projects/{projectId}/status", new { Status = "OnGoing" });

        statusResponse.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task ChangeProjectStatus_AsScientist_Returns403Forbidden()
    {
        var adminClient = _factory.CreateClientWithRole("Admin");
        var managerId = await GetFirstResourceIdAsync(adminClient);
        var code = $"SC{Guid.NewGuid():N}"[..8].ToUpperInvariant();

        var createResponse = await adminClient.PostAsJsonAsync("/api/projects", new
        {
            Name = "Scientist Blocked Project",
            Code = code,
            ManagerId = managerId,
            EstimatedMd = 60
        });
        var projectId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var scientistClient = _factory.CreateClientWithRole("Scientist");
        var statusResponse = await scientistClient.PatchAsJsonAsync($"/api/projects/{projectId}/status", new { Status = "Quoted" });

        statusResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetProjectById_UnknownId_Returns404()
    {
        var client = _factory.CreateClientWithRole("Admin");

        var response = await client.GetAsync($"/api/projects/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private sealed record IdNameDto(Guid Id, string Name);
    private sealed record ProjectDetailDto(Guid Id, string Name, string Code, string Status, List<string> AllowedNextStatuses);
}
