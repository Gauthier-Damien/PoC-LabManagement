using DPD.Domain.Common;

namespace DPD.Domain.Entities;

public sealed class MaintenanceContract : BaseEntity
{
    public Guid EquipmentId { get; set; }
    public Equipment Equipment { get; set; } = null!;
    public Guid SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal YearlyBudget { get; set; }
    public decimal ForecastN1 { get; set; }
    public decimal ForecastN2 { get; set; }
}
