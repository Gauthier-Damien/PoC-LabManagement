namespace DPD.Application.Common.Interfaces;

public interface ICapacityPlanningService
{
    Task<IReadOnlyCollection<CapacityProjectionPointDto>> CalculateAsync(int[] horizons, CancellationToken cancellationToken);
}
