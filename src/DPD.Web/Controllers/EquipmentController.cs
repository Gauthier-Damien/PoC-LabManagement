using DPD.Application.Modules.Equipment.Commands.ReserveEquipment;
using DPD.Application.Modules.Equipment.Queries.GetReservations;
using DPD.Web.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DPD.Web.Controllers;

[ApiController]
[Route("api/equipment")]
[Authorize]
public sealed class EquipmentController : ControllerBase
{
    private readonly IMediator _mediator;

    public EquipmentController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("reservations")]
    public Task<IReadOnlyCollection<EquipmentReservationDto>> GetReservations([FromQuery] int page = 1, [FromQuery] int pageSize = 25)
    {
        return _mediator.Send(new GetReservationsQuery(page, pageSize));
    }

    [HttpPost("reservations")]
    [Authorize(Policy = PolicyNames.ReserveEquipment)]
    public async Task<ActionResult<Guid>> Reserve([FromBody] ReserveEquipmentRequest request)
    {
        var id = await _mediator.Send(new ReserveEquipmentCommand(
            request.EquipmentId,
            request.ResourceId,
            request.StartTime,
            request.EndTime,
            request.Purpose));

        return CreatedAtAction(nameof(GetReservations), new { id }, id);
    }

    public sealed record ReserveEquipmentRequest(Guid EquipmentId, Guid ResourceId, DateTimeOffset StartTime, DateTimeOffset EndTime, string Purpose);
}
