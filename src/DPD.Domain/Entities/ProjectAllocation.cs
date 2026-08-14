using DPD.Domain.Common;

namespace DPD.Domain.Entities;

public sealed class ProjectAllocation : BaseEntity
{
    public Guid ResourceId { get; set; }
    public Resource Resource { get; set; } = null!;
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal PlannedMd { get; set; }
}
