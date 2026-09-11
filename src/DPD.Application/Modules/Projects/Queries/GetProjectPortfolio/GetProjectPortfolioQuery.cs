using DPD.Application.Common.Interfaces;
using DPD.Application.Common.Models;
using DPD.Domain.Enums;
using Mapster;
using MapsterMapper;
using MediatR;

namespace DPD.Application.Modules.Projects.Queries.GetProjectPortfolio;

public sealed record GetProjectPortfolioQuery(int Page = 1, int PageSize = 25) : IRequest<PaginatedResult<ProjectPortfolioItemDto>>;

public sealed record ProjectPortfolioItemDto(Guid Id, string Name, string Code, ProjectStatus Status, decimal EstimatedMd, decimal ActualMd, decimal RemainingMd, decimal VarianceMd);

public sealed class GetProjectPortfolioQueryHandler : IRequestHandler<GetProjectPortfolioQuery, PaginatedResult<ProjectPortfolioItemDto>>
{
    private readonly IProjectRepository _projects;
    private readonly IMapper _mapper;

    public GetProjectPortfolioQueryHandler(IProjectRepository projects, IMapper mapper)
    {
        _projects = projects;
        _mapper = mapper;
    }

    public async Task<PaginatedResult<ProjectPortfolioItemDto>> Handle(GetProjectPortfolioQuery request, CancellationToken cancellationToken)
    {
        var projects = await _projects.ListAsync(request.Page, request.PageSize, cancellationToken);
        var total = await _projects.CountAsync(cancellationToken);

        var items = _mapper.Map<List<ProjectPortfolioItemDto>>(projects);

        return new PaginatedResult<ProjectPortfolioItemDto>(items, request.Page, request.PageSize, total);
    }
}
