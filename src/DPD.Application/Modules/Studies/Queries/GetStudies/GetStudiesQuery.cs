using DPD.Application.Common.Interfaces;
using DPD.Application.Common.Models;
using DPD.Domain.Enums;
using MediatR;

namespace DPD.Application.Modules.Studies.Queries.GetStudies;

public sealed record GetStudiesQuery(int Page = 1, int PageSize = 25) : IRequest<PaginatedResult<StudyListItemDto>>;

/// <summary>
/// DTO de restitution d'une étude incluant les indicateurs de charge attendus par le PRD 6.7 :
/// MD Restants = MD Estimés - MD Réalisés ; Ecart = MD Réalisés - MD Estimés ;
/// Projection Finale (EAC) = MD Réalisés + MD Restants.
/// </summary>
public sealed record StudyListItemDto(
    Guid Id,
    Guid ProjectId,
    string Code,
    Guid StudyDirectorId,
    Department Department,
    StudyStatus Status,
    DateOnly TargetDate,
    DateOnly? ActualDate,
    decimal EstimatedMd,
    decimal ActualMd,
    decimal RemainingMd,
    decimal VarianceMd,
    decimal EstimateAtCompletion,
    bool ReDo,
    string? Comments);

public sealed class GetStudiesQueryHandler : IRequestHandler<GetStudiesQuery, PaginatedResult<StudyListItemDto>>
{
    private readonly IStudyRepository _studies;

    public GetStudiesQueryHandler(IStudyRepository studies)
    {
        _studies = studies;
    }

    public async Task<PaginatedResult<StudyListItemDto>> Handle(GetStudiesQuery request, CancellationToken cancellationToken)
    {
        var studies = await _studies.ListAsync(request.Page, request.PageSize, cancellationToken);
        var total = await _studies.CountAsync(cancellationToken);

        var items = studies.Select(s => new StudyListItemDto(
            s.Id,
            s.ProjectId,
            s.Code,
            s.StudyDirectorId,
            s.Department,
            s.Status,
            s.TargetDate,
            s.ActualDate,
            s.EstimatedMd,
            s.ActualMd,
            s.EstimatedMd - s.ActualMd,
            s.ActualMd - s.EstimatedMd,
            s.ActualMd + (s.EstimatedMd - s.ActualMd),
            s.ReDo,
            s.Comments)).ToList();

        return new PaginatedResult<StudyListItemDto>(items, request.Page, request.PageSize, total);
    }
}
