using DPD.Domain.Common;

namespace DPD.Domain.Entities;

public sealed class Study : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public string Code { get; set; } = string.Empty;
    public Guid StudyDirectorId { get; set; }
    public Resource StudyDirector { get; set; } = null!;
    public DateOnly TargetDate { get; set; }
    public DateOnly? ActualDate { get; set; }
    public decimal EstimatedMd { get; set; }
    public decimal ActualMd { get; set; }
    public ICollection<TimeEntry> TimeEntries { get; set; } = new List<TimeEntry>();
}
