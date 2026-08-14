using DPD.Domain.Entities;
using DPD.Domain.Enums;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DPD.Infrastructure.Persistence;

public static class AppDbInitializer
{
    public static async Task InitializeAsync(AppDbContext db, bool forceReset = false, CancellationToken cancellationToken = default)
    {
        if (forceReset)
        {
            // Release any pooled native SQLite connections (including ones held by a previous
            // process instance or a prior DbContext) so the underlying file can be deleted safely.
            SqliteConnection.ClearAllPools();
            await DeleteWithRetryAsync(db, cancellationToken);
        }

        await db.Database.EnsureCreatedAsync(cancellationToken);

        if (await db.Resources.AnyAsync(cancellationToken))
        {
            return;
        }

        var manager = new Resource
        {
            FirstName = "Sophie",
            LastName = "Manager",
            Email = "sophie.manager@dpd.test",
            Department = Department.AD,
            Fte = 1m,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-2))
        };

        var scientist = new Resource
        {
            FirstName = "Lina",
            LastName = "Scientist",
            Email = "lina.scientist@dpd.test",
            Department = Department.FPD,
            Fte = 0.8m,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)),
            Manager = manager
        };

        var project = new Project
        {
            Name = "Liver Model Program",
            ProjectCode = "LM028",
            ManagerId = manager.Id,
            EstimatedMd = 240,
            ActualMd = 40
        };

        var study = new Study
        {
            Project = project,
            Code = "ST01",
            StudyDirectorId = manager.Id,
            TargetDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3)),
            EstimatedMd = 80,
            ActualMd = 12
        };

        var equipment = new Equipment
        {
            Name = "HPLC Unit",
            SerialNumber = "HPLC-001",
            Status = EquipmentStatus.Available,
            InstallationDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-3)),
            CommissioningDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-3).AddDays(7)),
            WarrantyEndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)),
            AcquisitionCost = 95000,
            MaintenanceCostCumulative = 8600,
            OwnerId = manager.Id,
            BackupOwnerId = scientist.Id
        };

        var supplier = new Supplier
        {
            Name = "LabTech Services",
            ContactName = "Ana Dupont",
            ContactEmail = "ana.dupont@labtech.test",
            SlaLevel = "Gold"
        };

        var contract = new MaintenanceContract
        {
            Equipment = equipment,
            Supplier = supplier,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-2)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(10)),
            YearlyBudget = 18000,
            ForecastN1 = 20000,
            ForecastN2 = 21000
        };

        var reservation = new EquipmentReservation
        {
            Equipment = equipment,
            Resource = scientist,
            StartTime = DateTimeOffset.UtcNow.AddHours(2),
            EndTime = DateTimeOffset.UtcNow.AddHours(5),
            Purpose = "Stability run"
        };

        var allocation = new ProjectAllocation
        {
            Resource = scientist,
            Project = project,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(6)),
            PlannedMd = 92
        };

        var timeEntry = new TimeEntry
        {
            Resource = scientist,
            Study = study,
            WorkDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            Hours = 7.5m
        };
        timeEntry.Submit();

        await db.Resources.AddRangeAsync(manager, scientist);
        await db.Projects.AddAsync(project, cancellationToken);
        await db.Studies.AddAsync(study, cancellationToken);
        await db.Equipments.AddAsync(equipment, cancellationToken);
        await db.Suppliers.AddAsync(supplier, cancellationToken);
        await db.MaintenanceContracts.AddAsync(contract, cancellationToken);
        await db.EquipmentReservations.AddAsync(reservation, cancellationToken);
        await db.ProjectAllocations.AddAsync(allocation, cancellationToken);
        await db.TimeEntries.AddAsync(timeEntry, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task DeleteWithRetryAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        const int maxAttempts = 5;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await db.Database.EnsureDeletedAsync(cancellationToken);
                return;
            }
            catch (IOException) when (attempt < maxAttempts)
            {
                SqliteConnection.ClearAllPools();
                await Task.Delay(200 * attempt, cancellationToken);
            }
        }
    }
}
