using DPD.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DPD.Infrastructure.Services;

public sealed class CapacityPlanningService : ICapacityPlanningService
{
    private readonly Persistence.AppDbContext _db;

    public CapacityPlanningService(Persistence.AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyCollection<CapacityProjectionPointDto>> CalculateAsync(int[] horizons, CancellationToken cancellationToken)
    {
        var resources = await _db.Resources.Select(r => r.Fte).ToListAsync(cancellationToken);
        var allocations = await _db.ProjectAllocations.ToListAsync(cancellationToken);

        var results = horizons.Select(h =>
        {
            var openedDays = h * 21m;
            var capacity = resources.Sum() * openedDays;
            var allocated = allocations.Where(a => a.StartDate <= DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(h))).Sum(a => a.PlannedMd);
            return new CapacityProjectionPointDto(h, capacity, allocated, capacity - allocated);
        }).ToList();

        return results;
    }
}
