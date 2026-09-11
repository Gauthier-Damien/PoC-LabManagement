using DPD.Application.Common.Exceptions;
using DPD.Application.Common.Interfaces;
using DPD.Domain.Entities;
using DPD.Domain.Enums;
using MediatR;

namespace DPD.Application.Modules.Studies.Commands.CreateStudy;

public sealed record CreateStudyCommand(
    Guid ProjectId,
    string Code,
    Guid StudyDirectorId,
    Department Department,
    DateOnly TargetDate,
    decimal EstimatedMd) : IRequest<Guid>, IRequireRoles
{
    public IReadOnlyCollection<string> AllowedRoles => ["SD", "PrincipalInvestigator", "Admin"];
}

public sealed class CreateStudyCommandHandler : IRequestHandler<CreateStudyCommand, Guid>
{
    private readonly IProjectRepository _projects;
    private readonly IResourceRepository _resources;
    private readonly IStudyRepository _studies;
    private readonly IUnitOfWork _unitOfWork;

    public CreateStudyCommandHandler(IProjectRepository projects, IResourceRepository resources, IStudyRepository studies, IUnitOfWork unitOfWork)
    {
        _projects = projects;
        _resources = resources;
        _studies = studies;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateStudyCommand request, CancellationToken cancellationToken)
    {
        if (await _projects.FindAsync(request.ProjectId, cancellationToken) is null)
        {
            throw new NotFoundException("Project not found.");
        }

        if (!await _resources.ExistsAsync(request.StudyDirectorId, cancellationToken))
        {
            throw new BusinessException("Study Director does not exist.");
        }

        var study = new Study
        {
            ProjectId = request.ProjectId,
            Code = request.Code,
            StudyDirectorId = request.StudyDirectorId,
            Department = request.Department,
            TargetDate = request.TargetDate,
            EstimatedMd = request.EstimatedMd
        };

        await _studies.AddAsync(study, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return study.Id;
    }
}
