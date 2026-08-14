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

        return new DashboardDto(resources, projects, activeStudies, reservations, maintenanceBudget, projection);
    }
}
