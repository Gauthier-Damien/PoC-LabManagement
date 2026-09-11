using DPD.Application.Common.Interfaces;
using DPD.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DPD.Infrastructure.Persistence.Repositories;

public sealed class StudyRepository : IStudyRepository
{
    private readonly AppDbContext _db;

    public StudyRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<Study?> FindAsync(Guid id, CancellationToken cancellationToken)
    {
        return _db.Studies.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken)
    {
        return _db.Studies.AnyAsync(x => x.Id == id, cancellationToken);
    }

    public async Task AddAsync(Study study, CancellationToken cancellationToken)
    {
        await _db.Studies.AddAsync(study, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Study>> ListAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        return await _db.Studies
            .OrderBy(x => x.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(CancellationToken cancellationToken)
    {
        return _db.Studies.CountAsync(cancellationToken);
    }
}
