using DPD.Application.Modules.Projects.Commands.ChangeProjectStatus;
using DPD.Application.Modules.Projects.Commands.CreateProject;
using DPD.Application.Modules.Projects.Queries.GetProjectPortfolio;
using DPD.Domain.Enums;
using DPD.Web.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DPD.Web.Controllers;

[ApiController]
[Route("api/projects")]
[Authorize]
public sealed class ProjectsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProjectsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public Task<IReadOnlyCollection<ProjectPortfolioItemDto>> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 25)
    {
        return _mediator.Send(new GetProjectPortfolioQuery(page, pageSize));
    }

    [HttpPost]
    [Authorize(Policy = PolicyNames.ModifyProjectStatus)]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateProjectRequest request)
    {
        var id = await _mediator.Send(new CreateProjectCommand(request.Name, request.Code, request.ManagerId, request.EstimatedMd));
        return CreatedAtAction(nameof(Get), new { id }, id);
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = PolicyNames.ModifyProjectStatus)]
    public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] ChangeProjectStatusRequest request)
    {
        await _mediator.Send(new ChangeProjectStatusCommand(id, request.Status));
        return NoContent();
    }

    public sealed record CreateProjectRequest(string Name, string Code, Guid ManagerId, decimal EstimatedMd);
    public sealed record ChangeProjectStatusRequest(ProjectStatus Status);
}
