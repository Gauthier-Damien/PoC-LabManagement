using DPD.Application.Modules.TimeTracking.Commands.ApproveTimesheet;
using DPD.Application.Modules.TimeTracking.Commands.SubmitTimesheet;
using DPD.Application.Modules.TimeTracking.Queries.GetTimeEntries;
using DPD.Web.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DPD.Web.Controllers;

[ApiController]
[Route("api/timesheets")]
[Authorize]
public sealed class TimesheetsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TimesheetsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public Task<IReadOnlyCollection<TimeEntryListItemDto>> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 25)
    {
        return _mediator.Send(new GetTimeEntriesQuery(page, pageSize));
    }

    [HttpPost]
    [Authorize(Policy = PolicyNames.SubmitTimeEntry)]
    public async Task<ActionResult<Guid>> Submit([FromBody] SubmitTimesheetRequest request)
    {
        var id = await _mediator.Send(new SubmitTimesheetCommand(request.ResourceId, request.StudyId, request.WorkDate, request.Hours));
        return CreatedAtAction(nameof(Get), new { id }, id);
    }

    [HttpPatch("{id:guid}/approve")]
    [Authorize(Policy = PolicyNames.ValidateTimesheet)]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ApproveTimesheetRequest request)
    {
        await _mediator.Send(new ApproveTimesheetCommand(id, request.ApproverId));
        return NoContent();
    }

    public sealed record SubmitTimesheetRequest(Guid ResourceId, Guid StudyId, DateOnly WorkDate, decimal Hours);
    public sealed record ApproveTimesheetRequest(Guid ApproverId);
}
