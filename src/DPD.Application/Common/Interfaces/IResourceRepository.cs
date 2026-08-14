using DPD.Domain.Entities;

namespace DPD.Application.Common.Interfaces;

public interface IResourceRepository
{
    Task<Resource?> FindAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(Resource resource, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);
}
