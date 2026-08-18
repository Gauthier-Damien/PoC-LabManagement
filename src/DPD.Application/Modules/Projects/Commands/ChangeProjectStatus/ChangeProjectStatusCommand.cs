using DPD.Application.Common.Exceptions;
using DPD.Application.Common.Interfaces;
using DPD.Domain.Enums;
using MediatR;

namespace DPD.Application.Modules.Projects.Commands.ChangeProjectStatus;

public sealed record ChangeProjectStatusCommand(Guid ProjectId, ProjectStatus Status) : IRequest, IRequireRoles
{
    public IReadOnlyCollection<string> AllowedRoles => ["SD", "Manager", "Admin"];
}

public sealed class ChangeProjectStatusCommandHandler : IRequestHandler<ChangeProjectStatusCommand>
{
    private readonly IProjectRepository _projects;
    private readonly IUnitOfWork _unitOfWork;

    public ChangeProjectStatusCommandHandler(IProjectRepository projects, IUnitOfWork unitOfWork)
    {
        _projects = projects;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(ChangeProjectStatusCommand request, CancellationToken cancellationToken)
    {
        var project = await _projects.FindAsync(request.ProjectId, cancellationToken);
        if (project is null)
        {
            throw new NotFoundException("Project not found.");
        }

        project.ChangeStatus(request.Status);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
