using DPD.Domain.Common;

namespace DPD.Domain.Entities;

public sealed class EquipmentReservation : BaseEntity
{
    public Guid EquipmentId { get; set; }
    public Equipment Equipment { get; set; } = null!;
    public Guid ResourceId { get; set; }
    public Resource Resource { get; set; } = null!;
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public string Purpose { get; set; } = string.Empty;
}
