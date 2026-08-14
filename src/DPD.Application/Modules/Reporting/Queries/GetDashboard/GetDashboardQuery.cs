using DPD.Application.Common.Interfaces;
using MediatR;

namespace DPD.Application.Modules.Reporting.Queries.GetDashboard;

public sealed record GetDashboardQuery() : IRequest<DashboardDto>;

public sealed class GetDashboardQueryHandler : IRequestHandler<GetDashboardQuery, DashboardDto>
{
    private readonly IReportingReadService _reporting;

    public GetDashboardQueryHandler(IReportingReadService reporting)
    {
        _reporting = reporting;
    }

    public Task<DashboardDto> Handle(GetDashboardQuery request, CancellationToken cancellationToken)
    {
        return _reporting.GetDashboardAsync(cancellationToken);
    }
}
