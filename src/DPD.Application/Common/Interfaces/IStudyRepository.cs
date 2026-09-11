using DPD.Domain.Entities;

namespace DPD.Application.Common.Interfaces;

public interface IStudyRepository
{
    Task<Study?> FindAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(Study study, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Study>> ListAsync(int page, int pageSize, CancellationToken cancellationToken);
    Task<int> CountAsync(CancellationToken cancellationToken);
}
