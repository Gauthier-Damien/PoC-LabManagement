using DPD.Application.Modules.Studies.Commands.ChangeStudyStatus;
using DPD.Application.Modules.Studies.Commands.CreateStudy;
using DPD.Application.Modules.Studies.Queries.GetStudies;
using DPD.Domain.Enums;
using DPD.Web.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DPD.Web.Controllers;

[ApiController]
[Route("api/studies")]
[Authorize]
public sealed class StudiesController : ControllerBase
{
    private readonly IMediator _mediator;

    public StudiesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<StudyListItemDto>>> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 25)
    {
        var result = await _mediator.Send(new GetStudiesQuery(page, pageSize));
        Response.Headers.Append("X-Total-Count", result.TotalCount.ToString());
        Response.Headers.Append("X-Total-Pages", result.TotalPages.ToString());
        return Ok(result.Items);
    }

    [HttpPost]
    [Authorize(Policy = PolicyNames.ManageStudies)]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateStudyRequest request)
    {
        var id = await _mediator.Send(new CreateStudyCommand(
            request.ProjectId, request.Code, request.StudyDirectorId, request.Department, request.TargetDate, request.EstimatedMd));

        return CreatedAtAction(nameof(Get), new { id }, id);
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = PolicyNames.ManageStudies)]
    public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] ChangeStudyStatusRequest request)
    {
        await _mediator.Send(new ChangeStudyStatusCommand(id, request.Status));
        return NoContent();
    }

    public sealed record CreateStudyRequest(Guid ProjectId, string Code, Guid StudyDirectorId, Department Department, DateOnly TargetDate, decimal EstimatedMd);
    public sealed record ChangeStudyStatusRequest(StudyStatus Status);
}
