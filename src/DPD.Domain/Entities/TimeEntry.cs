using DPD.Domain.Common;
using DPD.Domain.Enums;

namespace DPD.Domain.Entities;

public sealed class TimeEntry : BaseEntity
{
    public Guid ResourceId { get; set; }
    public Resource Resource { get; set; } = null!;
    public Guid StudyId { get; set; }
    public Study Study { get; set; } = null!;
    public DateOnly WorkDate { get; set; }
    public decimal Hours { get; set; }
    public TimeEntryStatus Status { get; private set; } = TimeEntryStatus.Draft;
    public string? RejectionComment { get; set; }

    public void Submit()
    {
        if (Status == TimeEntryStatus.Locked)
        {
            throw new DomainException("Locked time entry cannot be submitted.");
        }

        Status = TimeEntryStatus.Submitted;
    }

    public void Approve()
    {
        if (Status != TimeEntryStatus.Submitted)
        {
            throw new DomainException("Only submitted time entries can be approved.");
        }

        Status = TimeEntryStatus.Approved;
    }

    public void Reject(string comment)
    {
        if (Status != TimeEntryStatus.Submitted)
        {
            throw new DomainException("Only submitted time entries can be rejected.");
        }

        RejectionComment = comment;
        Status = TimeEntryStatus.Rejected;
    }

    public void Lock()
    {
        Status = TimeEntryStatus.Locked;
    }
}
