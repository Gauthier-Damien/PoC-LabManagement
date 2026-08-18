using DPD.Application.Common.Interfaces;
using DPD.Application.Common.Models;
using DPD.Domain.Enums;
using MediatR;

namespace DPD.Application.Modules.Projects.Queries.GetProjectPortfolio;

public sealed record GetProjectPortfolioQuery(int Page = 1, int PageSize = 25) : IRequest<PaginatedResult<ProjectPortfolioItemDto>>;

public sealed record ProjectPortfolioItemDto(Guid Id, string Name, string Code, ProjectStatus Status, decimal EstimatedMd, decimal ActualMd, decimal RemainingMd, decimal VarianceMd);

public sealed class GetProjectPortfolioQueryHandler : IRequestHandler<GetProjectPortfolioQuery, PaginatedResult<ProjectPortfolioItemDto>>
{
    private readonly IProjectRepository _projects;

    public GetProjectPortfolioQueryHandler(IProjectRepository projects)
    {
        _projects = projects;
    }

    public async Task<PaginatedResult<ProjectPortfolioItemDto>> Handle(GetProjectPortfolioQuery request, CancellationToken cancellationToken)
    {
        var projects = await _projects.ListAsync(request.Page, request.PageSize, cancellationToken);
        var total = await _projects.CountAsync(cancellationToken);

        var items = projects
            .Select(p => new ProjectPortfolioItemDto(
                p.Id,
                p.Name,
                p.ProjectCode,
                p.Status,
                p.EstimatedMd,
                p.ActualMd,
                p.EstimatedMd - p.ActualMd,
                p.ActualMd - p.EstimatedMd))
            .ToList();

        return new PaginatedResult<ProjectPortfolioItemDto>(items, request.Page, request.PageSize, total);
    }
}
