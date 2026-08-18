using DPD.Application.Common.Exceptions;
using DPD.Application.Common.Interfaces;
using DPD.Domain.Enums;
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

    public GetProjectByIdQueryHandler(IProjectRepository projects)
    {
        _projects = projects;
    }

    public async Task<ProjectDetailDto> Handle(GetProjectByIdQuery request, CancellationToken cancellationToken)
    {
        var project = await _projects.FindWithStudiesAsync(request.ProjectId, cancellationToken);
        if (project is null)
        {
            throw new NotFoundException("Project not found.");
        }

        var remaining = project.EstimatedMd - project.ActualMd;
        var variance = project.ActualMd - project.EstimatedMd;
        var eac = project.ActualMd + Math.Max(0, remaining);

        var studies = project.Studies
            .Select(s => new ProjectStudySummaryDto(
                s.Id,
                s.Code,
                s.Department,
                s.Status,
                s.TargetDate,
                s.EstimatedMd,
                s.ActualMd,
                s.ActualMd - s.EstimatedMd))
            .OrderBy(s => s.Code)
            .ToList();

        var departmentCharges = project.Studies
            .GroupBy(s => s.Department)
            .Select(g => new ProjectDepartmentChargeDto(g.Key, g.Sum(s => s.ActualMd)))
            .OrderBy(d => d.Department)
            .ToList();

        return new ProjectDetailDto(
            project.Id,
            project.Name,
            project.ProjectCode,
            project.Status,
            project.GetAllowedNextStatuses(),
            project.ConsumesCapacity,
            project.IsArchived,
            project.ManagerId,
            project.EstimatedMd,
            project.ActualMd,
            remaining,
            variance,
            eac,
            studies,
            departmentCharges);
    }
}
