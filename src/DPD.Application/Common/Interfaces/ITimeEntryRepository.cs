using DPD.Domain.Entities;

namespace DPD.Application.Common.Interfaces;

public interface ITimeEntryRepository
{
    Task<TimeEntry?> FindAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(TimeEntry timeEntry, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<TimeEntry>> ListAsync(int page, int pageSize, CancellationToken cancellationToken);
}
