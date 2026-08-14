using DPD.Application.Common.Interfaces;
using DPD.Application.Modules.CapacityPlanning.Queries.CalculateCapacityProjection;
using DPD.Application.Modules.Reporting.Queries.GetDashboard;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DPD.Web.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public sealed class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;

    public DashboardController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public Task<DashboardDto> Get()
    {
        return _mediator.Send(new GetDashboardQuery());
    }

    [HttpGet("capacity")]
    public Task<IReadOnlyCollection<CapacityProjectionPointDto>> GetCapacityProjection()
    {
        return _mediator.Send(new CalculateCapacityProjectionQuery());
    }
}
