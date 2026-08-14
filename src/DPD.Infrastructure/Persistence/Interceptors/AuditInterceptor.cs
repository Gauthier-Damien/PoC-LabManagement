using System.Text.Json;
using DPD.Application.Common.Interfaces;
using DPD.Domain.Common;
using DPD.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DPD.Infrastructure.Persistence.Interceptors;

public sealed class AuditInterceptor : SaveChangesInterceptor
{
    private readonly IUserService _userService;

    public AuditInterceptor(IUserService userService)
    {
        _userService = userService;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null)
        {
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        var context = eventData.Context;
        var now = DateTimeOffset.UtcNow;

        var auditableEntries = context.ChangeTracker
            .Entries()
            .Where(x => x.Entity is BaseEntity && x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        var logs = new List<AuditLog>();

        foreach (var entry in auditableEntries)
        {
            var entity = (BaseEntity)entry.Entity;
            ApplyAuditStamp(entry, entity, now);

            logs.Add(new AuditLog
            {
                User = _userService.UserName,
                Action = entry.State.ToString(),
                EntityName = entry.Entity.GetType().Name,
                EntityId = entity.Id.ToString(),
                ChangesJson = SerializeChanges(entry)
            });
        }

        if (logs.Count > 0)
        {
            context.Set<AuditLog>().AddRange(logs);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyAuditStamp(EntityEntry entry, BaseEntity entity, DateTimeOffset now)
    {
        if (entry.State == EntityState.Added)
        {
            entity.CreatedAt = now;
            entity.CreatedBy = _userService.UserName;
            return;
        }

        if (entry.State == EntityState.Modified)
        {
            entity.UpdatedAt = now;
            entity.UpdatedBy = _userService.UserName;
            return;
        }

        if (entry.State == EntityState.Deleted)
        {
            entry.State = EntityState.Modified;
            entity.IsDeleted = true;
            entity.DeletedAt = now;
            entity.DeletedBy = _userService.UserName;
            entity.UpdatedAt = now;
            entity.UpdatedBy = _userService.UserName;
        }
    }

    private static string SerializeChanges(EntityEntry entry)
    {
        var changes = entry.Properties
            .Where(x => x.IsModified || entry.State is EntityState.Added or EntityState.Deleted)
            .Select(x => new
            {
                Field = x.Metadata.Name,
                OldValue = entry.State == EntityState.Added ? null : x.OriginalValue,
                NewValue = entry.State == EntityState.Deleted ? null : x.CurrentValue
            });

        return JsonSerializer.Serialize(changes);
    }
}
