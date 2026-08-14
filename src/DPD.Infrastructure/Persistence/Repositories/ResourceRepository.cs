using DPD.Application.Common.Interfaces;
using DPD.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DPD.Infrastructure.Persistence.Repositories;

public sealed class ResourceRepository : IResourceRepository
{
    private readonly AppDbContext _db;

    public ResourceRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<Resource?> FindAsync(Guid id, CancellationToken cancellationToken)
    {
        return _db.Resources.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task AddAsync(Resource resource, CancellationToken cancellationToken)
    {
        await _db.Resources.AddAsync(resource, cancellationToken);
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken)
    {
        return _db.Resources.AnyAsync(x => x.Id == id, cancellationToken);
    }
}
