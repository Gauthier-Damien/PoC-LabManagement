using DPD.Application.Common.Interfaces;
using DPD.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DPD.Infrastructure.Persistence.Repositories;

public sealed class EquipmentRepository : IEquipmentRepository
{
    private readonly AppDbContext _db;

    public EquipmentRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<Equipment?> FindAsync(Guid id, CancellationToken cancellationToken)
    {
        return _db.Equipments.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<bool> HasReservationConflictAsync(Guid equipmentId, DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken)
    {
        var existing = await _db.EquipmentReservations
            .Where(x => x.EquipmentId == equipmentId)
            .ToListAsync(cancellationToken);

        return existing.Any(x => x.StartTime < end && x.EndTime > start);
    }

    public async Task AddReservationAsync(EquipmentReservation reservation, CancellationToken cancellationToken)
    {
        await _db.EquipmentReservations.AddAsync(reservation, cancellationToken);
    }

    public async Task<IReadOnlyCollection<EquipmentReservation>> ListReservationsAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var all = await _db.EquipmentReservations.ToListAsync(cancellationToken);
        return all
            .OrderBy(x => x.StartTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
    }
}
