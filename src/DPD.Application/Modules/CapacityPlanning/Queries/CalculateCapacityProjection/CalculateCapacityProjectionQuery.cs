using DPD.Application.Common.Interfaces;
using MediatR;

namespace DPD.Application.Modules.CapacityPlanning.Queries.CalculateCapacityProjection;

public sealed record CalculateCapacityProjectionQuery() : IRequest<IReadOnlyCollection<CapacityProjectionPointDto>>;

public sealed class CalculateCapacityProjectionQueryHandler : IRequestHandler<CalculateCapacityProjectionQuery, IReadOnlyCollection<CapacityProjectionPointDto>>
{
    private static readonly int[] Horizons = [3, 6, 12, 24];
    private readonly ICapacityPlanningService _capacityPlanning;

    public CalculateCapacityProjectionQueryHandler(ICapacityPlanningService capacityPlanning)
    {
        _capacityPlanning = capacityPlanning;
    }

    public Task<IReadOnlyCollection<CapacityProjectionPointDto>> Handle(CalculateCapacityProjectionQuery request, CancellationToken cancellationToken)
    {
        return _capacityPlanning.CalculateAsync(Horizons, cancellationToken);
    }
}
