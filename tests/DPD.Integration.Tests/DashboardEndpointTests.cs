using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace DPD.Integration.Tests;

/// <summary>
/// Test de non-régression explicite pour le bug SQLite "Sum on decimal" qui faisait planter
/// GET /api/dashboard (et donc la page d'accueil) avec une 500 Internal Server Error.
/// </summary>
public class DashboardEndpointTests : IClassFixture<DpdWebApplicationFactory>
{
    private readonly DpdWebApplicationFactory _factory;

    public DashboardEndpointTests(DpdWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetDashboard_ReturnsOk_WithDepartmentBreakdown()
    {
        var client = _factory.CreateClientWithRole("Admin");

        var response = await client.GetAsync("/api/dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("departmentBreakdown");
        body.Should().Contain("portfolioEstimateAtCompletion");
    }

    [Fact]
    public async Task GetHealth_ReturnsOk()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
