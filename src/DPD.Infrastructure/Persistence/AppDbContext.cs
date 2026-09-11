using System.Linq.Expressions;
using DPD.Domain.Common;
using DPD.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DPD.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Resource> Resources => Set<Resource>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Study> Studies => Set<Study>();
    public DbSet<TimeEntry> TimeEntries => Set<TimeEntry>();
    public DbSet<ProjectAllocation> ProjectAllocations => Set<ProjectAllocation>();
    public DbSet<Equipment> Equipments => Set<Equipment>();
    public DbSet<EquipmentReservation> EquipmentReservations => Set<EquipmentReservation>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<MaintenanceContract> MaintenanceContracts => Set<MaintenanceContract>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    /// <summary>
    /// Configure le jeton de concurrence adapté au fournisseur de base de données actif :
    /// - SQL Server (cible de production) : colonne "rowversion" native générée/incrémentée par le
    ///   moteur (<see cref="BaseEntity.RowVersion"/> + <c>.IsRowVersion()</c>).
    /// - Tout autre fournisseur (SQLite pour le PoC) : jeton applicatif <see cref="BaseEntity.ConcurrencyStamp"/>
    ///   régénéré explicitement par <see cref="Interceptors.AuditInterceptor"/> à chaque modification,
    ///   inclus par EF Core dans la clause WHERE (<c>.IsConcurrencyToken()</c>).
    /// </summary>
    private void ConfigureConcurrency<TEntity>(EntityTypeBuilder<TEntity> builder) where TEntity : BaseEntity
    {
        if (Database.IsSqlServer())
        {
            builder.Property(x => x.RowVersion).IsRowVersion();
        }
        else
        {
            builder.Property(x => x.ConcurrencyStamp).IsConcurrencyToken();
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Resource>(b =>
        {
            b.HasIndex(x => x.Email).IsUnique();
            b.Property(x => x.Fte).HasColumnType("decimal(5,2)");
            ConfigureConcurrency(b);
            b.HasOne(x => x.Manager).WithMany(x => x.DirectReports).HasForeignKey(x => x.ManagerId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Project>(b =>
        {
            b.HasIndex(x => x.ProjectCode).IsUnique();
            ConfigureConcurrency(b);
        });

        modelBuilder.Entity<Study>(b =>
        {
            ConfigureConcurrency(b);
            b.HasIndex(x => new { x.ProjectId, x.Code }).IsUnique();
            b.HasOne(x => x.StudyDirector).WithMany().HasForeignKey(x => x.StudyDirectorId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TimeEntry>(b =>
        {
            ConfigureConcurrency(b);
            b.HasIndex(x => new { x.ResourceId, x.WorkDate });
        });

        modelBuilder.Entity<Equipment>(b =>
        {
            ConfigureConcurrency(b);
            b.HasIndex(x => x.SerialNumber).IsUnique();
        });

        modelBuilder.Entity<EquipmentReservation>(b =>
        {
            ConfigureConcurrency(b);
            b.HasIndex(x => new { x.EquipmentId, x.StartTime, x.EndTime });
        });

        modelBuilder.Entity<AuditLog>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.ChangesJson).HasColumnType("TEXT");
        });

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                AddSoftDeleteQueryFilter(modelBuilder, entityType.ClrType);
            }

            foreach (var fk in entityType.GetForeignKeys())
            {
                fk.DeleteBehavior = DeleteBehavior.Restrict;
            }
        }

        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Ajoute le filtre global "!IsDeleted" pour un type d'entité donné en construisant directement
    /// l'expression lambda (Expression Trees), plutôt qu'en invoquant une méthode générique privée par
    /// réflexion (ce qui nécessiterait un accès non-public non souhaitable).
    /// </summary>
    private static void AddSoftDeleteQueryFilter(ModelBuilder modelBuilder, Type clrType)
    {
        var parameter = Expression.Parameter(clrType, "x");
        var isDeletedProperty = Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
        var notDeleted = Expression.Not(isDeletedProperty);
        var lambda = Expression.Lambda(notDeleted, parameter);

        modelBuilder.Entity(clrType).HasQueryFilter(lambda);
    }
}
