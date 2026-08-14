using DPD.Domain.Common;
using DPD.Domain.Entities;
using Microsoft.EntityFrameworkCore;

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Resource>(b =>
        {
            b.HasIndex(x => x.Email).IsUnique();
            b.Property(x => x.Fte).HasColumnType("decimal(5,2)");
            b.Property(x => x.RowVersion).IsConcurrencyToken();
            b.HasOne(x => x.Manager).WithMany(x => x.DirectReports).HasForeignKey(x => x.ManagerId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Project>(b =>
        {
            b.HasIndex(x => x.ProjectCode).IsUnique();
            b.Property(x => x.RowVersion).IsConcurrencyToken();
        });

        modelBuilder.Entity<Study>(b =>
        {
            b.Property(x => x.RowVersion).IsConcurrencyToken();
            b.HasIndex(x => new { x.ProjectId, x.Code }).IsUnique();
            b.HasOne(x => x.StudyDirector).WithMany().HasForeignKey(x => x.StudyDirectorId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TimeEntry>(b =>
        {
            b.Property(x => x.RowVersion).IsConcurrencyToken();
            b.HasIndex(x => new { x.ResourceId, x.WorkDate });
        });

        modelBuilder.Entity<Equipment>(b =>
        {
            b.Property(x => x.RowVersion).IsConcurrencyToken();
            b.HasIndex(x => x.SerialNumber).IsUnique();
        });

        modelBuilder.Entity<EquipmentReservation>(b =>
        {
            b.Property(x => x.RowVersion).IsConcurrencyToken();
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
                var method = typeof(AppDbContext).GetMethod(nameof(AddSoftDeleteQueryFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                    .MakeGenericMethod(entityType.ClrType);
                method.Invoke(null, [modelBuilder]);
            }

            foreach (var fk in entityType.GetForeignKeys())
            {
                fk.DeleteBehavior = DeleteBehavior.Restrict;
            }
        }

        base.OnModelCreating(modelBuilder);
    }

    private static void AddSoftDeleteQueryFilter<TEntity>(ModelBuilder modelBuilder) where TEntity : class, ISoftDeletable
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(x => !x.IsDeleted);
    }
}
