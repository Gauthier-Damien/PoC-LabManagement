using DPD.Domain.Common;
using DPD.Domain.Enums;

namespace DPD.Domain.Entities;

public sealed class Resource : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public Department Department { get; set; }
    public decimal Fte { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public Guid? ManagerId { get; set; }
    public Resource? Manager { get; set; }
    public ICollection<Resource> DirectReports { get; set; } = new List<Resource>();
    public ICollection<TimeEntry> TimeEntries { get; set; } = new List<TimeEntry>();
    public ICollection<ProjectAllocation> ProjectAllocations { get; set; } = new List<ProjectAllocation>();
}
