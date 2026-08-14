using DPD.Domain.Entities;

namespace DPD.Application.Common.Interfaces;

public interface IEquipmentRepository
{
    Task<Equipment?> FindAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> HasReservationConflictAsync(Guid equipmentId, DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken);
    Task AddReservationAsync(EquipmentReservation reservation, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<EquipmentReservation>> ListReservationsAsync(int page, int pageSize, CancellationToken cancellationToken);
}
