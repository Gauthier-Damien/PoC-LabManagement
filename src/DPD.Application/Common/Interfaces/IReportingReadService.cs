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
    IReadOnlyCollection<CapacityProjectionPointDto> CapacityProjection,
    DepartmentBreakdownDto DepartmentBreakdown,
    decimal PortfolioEstimateAtCompletion,
    int ContractsRenewingSoon
);

public sealed record CapacityProjectionPointDto(int HorizonMonths, decimal CapacityMd, decimal AllocatedMd, decimal GapMd);

/// <summary>
/// Répartition départementale de la charge (PRD 6.8) : charge par département (AD/FPD/MSI)
/// calculée à partir des MD réalisés des études, et ratios croisés utilisés en reporting de portefeuille.
/// </summary>
public sealed record DepartmentBreakdownDto(
    decimal ChargeAd,
    decimal ChargeFpd,
    decimal ChargeMsi,
    decimal ChargeTotal,
    decimal RatioAdFpd,
    decimal RatioAdMsi,
    decimal RatioFpdMsi);

