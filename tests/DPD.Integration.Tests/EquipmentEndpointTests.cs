using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace DPD.Integration.Tests;

/// <summary>
/// Couvre la réservation d'équipement (prévention des conflits, RBAC) de bout en bout.
/// </summary>
public class EquipmentEndpointTests : IClassFixture<DpdWebApplicationFactory>
{
    private readonly DpdWebApplicationFactory _factory;

    public EquipmentEndpointTests(DpdWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<(Guid EquipmentId, Guid ResourceId)> GetReferenceIdsAsync(HttpClient client)
    {
        var equipment = await client.GetFromJsonAsync<List<IdNameDto>>("/api/reference/equipment");
        var resources = await client.GetFromJsonAsync<List<IdNameDto>>("/api/reference/resources");
        return (equipment!.First().Id, resources!.First().Id);
    }

    [Fact]
    public async Task ReserveEquipment_AsEquipmentOwner_Returns403BecauseNotInAllowedRoles()
    {
        var client = _factory.CreateClientWithRole("EquipmentOwner");
        var (equipmentId, resourceId) = await GetReferenceIdsAsync(client);

        var response = await client.PostAsJsonAsync("/api/equipment/reservations", new
        {
            EquipmentId = equipmentId,
            ResourceId = resourceId,
            StartTime = DateTimeOffset.UtcNow.AddDays(1),
            EndTime = DateTimeOffset.UtcNow.AddDays(1).AddHours(1),
            Purpose = "Maintenance check"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ReserveEquipment_ConflictingSlot_Returns422()
    {
        var client = _factory.CreateClientWithRole("Scientist");
        var (equipmentId, resourceId) = await GetReferenceIdsAsync(client);
        var start = DateTimeOffset.UtcNow.AddDays(2);
        var end = start.AddHours(2);

        var first = await client.PostAsJsonAsync("/api/equipment/reservations", new
        {
            EquipmentId = equipmentId,
            ResourceId = resourceId,
            StartTime = start,
            EndTime = end,
            Purpose = "First booking"
        });
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var conflicting = await client.PostAsJsonAsync("/api/equipment/reservations", new
        {
            EquipmentId = equipmentId,
            ResourceId = resourceId,
            StartTime = start.AddMinutes(30),
            EndTime = end.AddMinutes(30),
            Purpose = "Overlapping booking"
        });

        conflicting.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    private sealed record IdNameDto(Guid Id, string Name);
}
