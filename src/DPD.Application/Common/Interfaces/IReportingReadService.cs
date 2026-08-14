namespace DPD.Application.Common.Interfaces;

public interface IReportingReadService
{
    Task<DashboardDto> GetDashboardAsync(CancellationToken cancellationToken);
}

public sealed record DashboardDto(
    int Resources,
    int Projects,
    int ActiveStudies,
    int ActiveEquipmentReservations,
    decimal MaintenanceBudget,
    IReadOnlyCollection<CapacityProjectionPointDto> CapacityProjection
);

public sealed record CapacityProjectionPointDto(int HorizonMonths, decimal CapacityMd, decimal AllocatedMd, decimal GapMd);
