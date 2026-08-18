using DPD.Domain.Common;
using DPD.Domain.Enums;

namespace DPD.Domain.Entities;

public sealed class Study : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public string Code { get; set; } = string.Empty;
    public Guid StudyDirectorId { get; set; }
    public Resource StudyDirector { get; set; } = null!;
    public Department Department { get; set; }
    public StudyStatus Status { get; private set; } = StudyStatus.Planned;
    public DateOnly TargetDate { get; set; }
    public DateOnly? ActualDate { get; set; }
    public decimal EstimatedMd { get; set; }
    public decimal ActualMd { get; set; }
    public bool ReDo { get; set; }
    public string? Comments { get; set; }
    public ICollection<TimeEntry> TimeEntries { get; set; } = new List<TimeEntry>();

    public void ChangeStatus(StudyStatus target)
    {
        if (Status is StudyStatus.Completed or StudyStatus.Cancelled)
        {
            throw new DomainException("Archived studies cannot change status.");
        }

        Status = target;
        if (target == StudyStatus.Completed)
        {
            ActualDate ??= DateOnly.FromDateTime(DateTime.UtcNow);
        }
    }
}

