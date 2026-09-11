using DPD.Application.Common.Interfaces;
using DPD.Domain.Entities;
using MediatR;

namespace DPD.Application.Modules.Projects.Commands.CreateProject;

public sealed record CreateProjectCommand(string Name, string Code, Guid ManagerId, decimal EstimatedMd) : IRequest<Guid>, IRequireRoles
{
    public IReadOnlyCollection<string> AllowedRoles => ["SD", "Manager", "Admin"];
}

public sealed class CreateProjectCommandHandler : IRequestHandler<CreateProjectCommand, Guid>
{
    private readonly IProjectRepository _projects;
    private readonly IResourceRepository _resources;
    private readonly IUnitOfWork _unitOfWork;

    public CreateProjectCommandHandler(IProjectRepository projects, IResourceRepository resources, IUnitOfWork unitOfWork)
    {
        _projects = projects;
        _resources = resources;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        if (!await _resources.ExistsAsync(request.ManagerId, cancellationToken))
        {
            throw new FluentValidation.ValidationException("Manager does not exist.");
        }

        if (await _projects.ExistsByCodeAsync(request.Code, cancellationToken))
        {
            throw new FluentValidation.ValidationException("Project code must be unique.");
        }

        var project = new Project
        {
            Name = request.Name,
            ProjectCode = request.Code,
            ManagerId = request.ManagerId,
            EstimatedMd = request.EstimatedMd
        };

        await _projects.AddAsync(project, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return project.Id;
    }
}
