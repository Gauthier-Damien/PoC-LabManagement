namespace DPD.Domain.Common;

public abstract class BaseEntity : IAuditable, ISoftDeletable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string CreatedBy { get; set; } = "system";
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    /// <summary>
    /// Jeton de concurrence natif SQL Server (colonne "timestamp"/rowversion). Configuré via
    /// <c>.IsRowVersion()</c> lorsque le fournisseur actif est SQL Server (cible de production) :
    /// la valeur est alors générée et incrémentée automatiquement par le moteur à chaque UPDATE.
    /// </summary>
    public byte[]? RowVersion { get; set; }

    /// <summary>
    /// Jeton de concurrence applicatif utilisé comme contournement pour le PoC SQLite, qui ne
    /// supporte pas nativement les colonnes rowversion. Un nouveau Guid est assigné explicitement
    /// par <see cref="DPD.Infrastructure.Persistence.Interceptors.AuditInterceptor"/> à chaque
    /// modification, et EF Core l'inclut dans la clause WHERE de l'UPDATE généré (IsConcurrencyToken).
    /// Si la ligne a déjà été modifiée par un autre utilisateur entre le chargement et la sauvegarde,
    /// 0 ligne est affectée et EF Core lève une <see cref="Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException"/>.
    /// </summary>
    public Guid ConcurrencyStamp { get; set; } = Guid.NewGuid();
}
