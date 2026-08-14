using DPD.Application.Common.Interfaces;
using DPD.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DPD.Infrastructure.Persistence.Repositories;

public sealed class ProjectRepository : IProjectRepository
{
    private readonly AppDbContext _db;

    public ProjectRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<Project?> FindAsync(Guid id, CancellationToken cancellationToken)
    {
        return _db.Projects.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<bool> ExistsByCodeAsync(string projectCode, CancellationToken cancellationToken)
    {
        return _db.Projects.AnyAsync(x => x.ProjectCode == projectCode, cancellationToken);
    }

    public async Task AddAsync(Project project, CancellationToken cancellationToken)
    {
        await _db.Projects.AddAsync(project, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Project>> ListAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        return await _db.Projects
            .OrderBy(x => x.ProjectCode)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }
}
