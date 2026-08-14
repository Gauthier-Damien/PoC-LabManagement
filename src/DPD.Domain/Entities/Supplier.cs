using DPD.Domain.Common;

namespace DPD.Domain.Entities;

public sealed class Supplier : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string SlaLevel { get; set; } = string.Empty;
    public ICollection<MaintenanceContract> MaintenanceContracts { get; set; } = new List<MaintenanceContract>();
}
