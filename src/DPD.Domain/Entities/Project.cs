using DPD.Domain.Common;
using DPD.Domain.Enums;

namespace DPD.Domain.Entities;

public sealed class Project : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string ProjectCode { get; set; } = string.Empty;
    public ProjectStatus Status { get; private set; } = ProjectStatus.Hypothesis;
    public Guid ManagerId { get; set; }
    public decimal EstimatedMd { get; set; }
    public decimal ActualMd { get; set; }
    public ICollection<Study> Studies { get; set; } = new List<Study>();

    public void ChangeStatus(ProjectStatus target)
    {
        if (Status is ProjectStatus.Completed or ProjectStatus.Cancelled or ProjectStatus.Lost)
        {
            throw new DomainException("Archived projects cannot change status.");
        }

        Status = target;
    }
}
