using DPD.Domain.Entities;

namespace DPD.Application.Common.Interfaces;

public interface IProjectRepository
{
    Task<Project?> FindAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> ExistsByCodeAsync(string projectCode, CancellationToken cancellationToken);
    Task AddAsync(Project project, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Project>> ListAsync(int page, int pageSize, CancellationToken cancellationToken);
}
