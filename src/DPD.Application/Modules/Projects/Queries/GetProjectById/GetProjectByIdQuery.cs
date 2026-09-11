using DPD.Application.Common.Exceptions;
using DPD.Application.Common.Interfaces;
using DPD.Domain.Enums;
using MapsterMapper;
using MediatR;

namespace DPD.Application.Modules.Projects.Queries.GetProjectById;

public sealed record GetProjectByIdQuery(Guid ProjectId) : IRequest<ProjectDetailDto>;

/// <summary>
/// Vue détaillée d'un projet incluant ses études, la charge par département et les transitions
/// de statut valides (pilotage complet du cycle de vie - PRD 6.4/6.6/6.7/6.8).
/// </summary>
public sealed record ProjectDetailDto(
    Guid Id,
    string Name,
    string Code,
    ProjectStatus Status,
    IReadOnlyCollection<ProjectStatus> AllowedNextStatuses,
    bool ConsumesCapacity,
    bool IsArchived,
    Guid ManagerId,
    decimal EstimatedMd,
    decimal ActualMd,
    decimal RemainingMd,
    decimal VarianceMd,
    decimal EstimateAtCompletion,
    IReadOnlyCollection<ProjectStudySummaryDto> Studies,
    IReadOnlyCollection<ProjectDepartmentChargeDto> DepartmentCharges);

public sealed record ProjectStudySummaryDto(
    Guid Id,
    string Code,
    Department Department,
    StudyStatus Status,
    DateOnly TargetDate,
    decimal EstimatedMd,
    decimal ActualMd,
    decimal VarianceMd);

public sealed record ProjectDepartmentChargeDto(Department Department, decimal ActualMd);

public sealed class GetProjectByIdQueryHandler : IRequestHandler<GetProjectByIdQuery, ProjectDetailDto>
{
    private readonly IProjectRepository _projects;
    private readonly IMapper _mapper;

    public GetProjectByIdQueryHandler(IProjectRepository projects, IMapper mapper)
    {
        _projects = projects;
        _mapper = mapper;
    }

    public async Task<ProjectDetailDto> Handle(GetProjectByIdQuery request, CancellationToken cancellationToken)
    {
        var project = await _projects.FindWithStudiesAsync(request.ProjectId, cancellationToken);
        if (project is null)
        {
            throw new NotFoundException("Project not found.");
        }

        return _mapper.Map<ProjectDetailDto>(project);
    }
}
