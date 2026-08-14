using DPD.Domain.Common;
using DPD.Domain.Enums;

namespace DPD.Domain.Entities;

public sealed class Equipment : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public EquipmentStatus Status { get; set; } = EquipmentStatus.Available;
    public DateOnly InstallationDate { get; set; }
    public DateOnly CommissioningDate { get; set; }
    public DateOnly WarrantyEndDate { get; set; }
    public decimal AcquisitionCost { get; set; }
    public decimal MaintenanceCostCumulative { get; set; }
    public Guid OwnerId { get; set; }
    public Guid BackupOwnerId { get; set; }
    public ICollection<EquipmentReservation> Reservations { get; set; } = new List<EquipmentReservation>();
    public ICollection<MaintenanceContract> MaintenanceContracts { get; set; } = new List<MaintenanceContract>();
}
