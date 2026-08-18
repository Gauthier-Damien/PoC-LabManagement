using DPD.Domain.Enums;
using DPD.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DPD.Infrastructure.Services;

/// <summary>
/// Traitement d'arrière-plan (ADR-007) générant des alertes automatisées :
/// - Contrats de maintenance arrivant à échéance sous J-30/J-60 (PRD "Should Have").
/// - Projets Signed/OnGoing en surcharge de capacité (MD Réalisés > MD Estimés).
/// Exécuté périodiquement sans bloquer l'IHM Blazor.
/// </summary>
public sealed class MaintenanceAlertWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MaintenanceAlertWorker> _logger;

    public MaintenanceAlertWorker(IServiceScopeFactory scopeFactory, ILogger<MaintenanceAlertWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunChecksAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Échec de l'exécution des vérifications d'alertes automatisées.");
            }

            await Task.Delay(TimeSpan.FromHours(12), stoppingToken);
        }
    }

    private async Task RunChecksAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var expiringContracts = await db.MaintenanceContracts
            .Include(c => c.Equipment)
            .Include(c => c.Supplier)
            .Where(c => c.EndDate >= today && c.EndDate <= today.AddDays(60))
            .ToListAsync(cancellationToken);

        foreach (var contract in expiringContracts)
        {
            var daysRemaining = contract.EndDate.DayNumber - today.DayNumber;
            var threshold = daysRemaining <= 30 ? "J-30" : "J-60";
            _logger.LogWarning(
                "ALERTE MAINTENANCE [{Threshold}] : le contrat de l'équipement '{Equipment}' (fournisseur {Supplier}) expire le {EndDate} ({DaysRemaining} jours restants).",
                threshold, contract.Equipment.Name, contract.Supplier.Name, contract.EndDate, daysRemaining);
        }

        var overloadedProjects = await db.Projects
            .Where(p => (p.Status == ProjectStatus.Signed || p.Status == ProjectStatus.OnGoing) && p.ActualMd > p.EstimatedMd)
            .ToListAsync(cancellationToken);

        foreach (var project in overloadedProjects)
        {
            _logger.LogWarning(
                "ALERTE CAPACITÉ : le projet '{ProjectCode}' est en surcharge (MD Réalisés {ActualMd} > MD Estimés {EstimatedMd}, écart {Variance}).",
                project.ProjectCode, project.ActualMd, project.EstimatedMd, project.ActualMd - project.EstimatedMd);
        }

        _logger.LogInformation(
            "Vérification des alertes automatisées terminée à {Timestamp} : {ContractsCount} contrat(s) à échéance, {ProjectsCount} projet(s) en surcharge.",
            DateTimeOffset.UtcNow, expiringContracts.Count, overloadedProjects.Count);
    }
}


