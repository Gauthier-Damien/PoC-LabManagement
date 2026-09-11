using DPD.Application.Common.Interfaces;
using DPD.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DPD.Infrastructure.Persistence.Repositories;

public sealed class TimeEntryRepository : ITimeEntryRepository
{
    private readonly AppDbContext _db;

    public TimeEntryRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<TimeEntry?> FindAsync(Guid id, CancellationToken cancellationToken)
    {
        return _db.TimeEntries.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task AddAsync(TimeEntry timeEntry, CancellationToken cancellationToken)
    {
        await _db.TimeEntries.AddAsync(timeEntry, cancellationToken);
    }

    public async Task<IReadOnlyCollection<TimeEntry>> ListAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        return await _db.TimeEntries
            .OrderByDescending(x => x.WorkDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(CancellationToken cancellationToken)
    {
        return _db.TimeEntries.CountAsync(cancellationToken);
    }
}
