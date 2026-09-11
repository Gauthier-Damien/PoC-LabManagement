using DPD.Application.Modules.TimeTracking.Commands.ApproveTimesheet;
using DPD.Application.Modules.TimeTracking.Commands.LockTimeEntry;
using DPD.Application.Modules.TimeTracking.Commands.RejectTimesheet;
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
    public async Task<ActionResult<IReadOnlyCollection<TimeEntryListItemDto>>> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 25)
    {
        var result = await _mediator.Send(new GetTimeEntriesQuery(page, pageSize));
        Response.Headers.Append("X-Total-Count", result.TotalCount.ToString());
        Response.Headers.Append("X-Total-Pages", result.TotalPages.ToString());
        return Ok(result.Items);
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

    [HttpPatch("{id:guid}/reject")]
    [Authorize(Policy = PolicyNames.ValidateTimesheet)]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectTimesheetRequest request)
    {
        await _mediator.Send(new RejectTimesheetCommand(id, request.ApproverId, request.Comment));
        return NoContent();
    }

    [HttpPatch("{id:guid}/lock")]
    [Authorize(Policy = PolicyNames.ManageUsersRoles)]
    public async Task<IActionResult> Lock(Guid id)
    {
        await _mediator.Send(new LockTimeEntryCommand(id));
        return NoContent();
    }

    public sealed record SubmitTimesheetRequest(Guid ResourceId, Guid StudyId, DateOnly WorkDate, decimal Hours);
    public sealed record ApproveTimesheetRequest(Guid ApproverId);
    public sealed record RejectTimesheetRequest(Guid ApproverId, string Comment);
}


