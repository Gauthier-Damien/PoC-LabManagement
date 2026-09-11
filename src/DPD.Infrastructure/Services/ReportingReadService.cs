using DPD.Application.Common.Interfaces;
using DPD.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DPD.Infrastructure.Services;

public sealed class ReportingReadService : IReportingReadService
{
    private readonly Persistence.AppDbContext _db;
    private readonly ICapacityPlanningService _capacity;

    public ReportingReadService(Persistence.AppDbContext db, ICapacityPlanningService capacity)
    {
        _db = db;
        _capacity = capacity;
    }

    public async Task<DashboardDto> GetDashboardAsync(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var now = DateTimeOffset.UtcNow;

        var resources = await _db.Resources.CountAsync(cancellationToken);
        var projects = await _db.Projects.CountAsync(cancellationToken);
        var activeStudies = await _db.Studies.CountAsync(x => x.TargetDate >= today, cancellationToken);
        var reservationEndTimes = await _db.EquipmentReservations
            .Select(x => x.EndTime)
            .ToListAsync(cancellationToken);
        var reservations = reservationEndTimes.Count(x => x > now);
        var maintenanceBudgets = await _db.MaintenanceContracts
            .Select(x => x.YearlyBudget)
            .ToListAsync(cancellationToken);
        var maintenanceBudget = maintenanceBudgets.Sum();
        var projection = await _capacity.CalculateAsync([3, 6, 12, 24], cancellationToken);

        // Répartition départementale (PRD 6.8) : charge = somme des MD réalisés des études par département.
        // Note : Sum(decimal) n'est pas traduisible en SQL par le provider SQLite ; on récupère les
        // paires (Department, ActualMd) et on agrège côté client (LINQ to Objects).
        var studyCharges = await _db.Studies
            .Select(s => new { s.Department, s.ActualMd })
            .ToListAsync(cancellationToken);

        var chargeAd = studyCharges.Where(x => x.Department == Department.AD).Sum(x => x.ActualMd);
        var chargeFpd = studyCharges.Where(x => x.Department == Department.FPD).Sum(x => x.ActualMd);
        var chargeMsi = studyCharges.Where(x => x.Department == Department.MSI).Sum(x => x.ActualMd);
        var chargeTotal = chargeAd + chargeFpd + chargeMsi;

        var departmentBreakdown = new DepartmentBreakdownDto(
            chargeAd,
            chargeFpd,
            chargeMsi,
            chargeTotal,
            chargeFpd == 0 ? 0 : Math.Round(chargeAd / chargeFpd, 2),
            chargeMsi == 0 ? 0 : Math.Round(chargeAd / chargeMsi, 2),
            chargeMsi == 0 ? 0 : Math.Round(chargeFpd / chargeMsi, 2));

        // EAC (Estimate At Completion) au niveau portefeuille = somme des MD réalisés + MD restants prévisionnels de tous les projets actifs.
        var projectLoads = await _db.Projects
            .Select(p => new { p.EstimatedMd, p.ActualMd })
            .ToListAsync(cancellationToken);
        var portfolioEac = projectLoads.Sum(p => p.ActualMd + Math.Max(0, p.EstimatedMd - p.ActualMd));

        // Contrats à renouveler sous 90 jours (KPI Maintenance).
        var renewalThreshold = today.AddDays(90);
        var contractsRenewingSoon = await _db.MaintenanceContracts
            .CountAsync(c => c.EndDate <= renewalThreshold && c.EndDate >= today, cancellationToken);

        return new DashboardDto(
            resources,
            projects,
            activeStudies,
            reservations,
            maintenanceBudget,
            projection,
            departmentBreakdown,
            portfolioEac,
            contractsRenewingSoon);
    }
}
