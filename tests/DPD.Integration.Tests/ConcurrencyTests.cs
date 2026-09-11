using DPD.Domain.Entities;
using DPD.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DPD.Integration.Tests;

/// <summary>
/// Valide le mécanisme de gestion de la non-concurrence (contrôle de concurrence optimiste) mis en
/// place pour le PoC SQLite : un jeton applicatif <see cref="DPD.Domain.Common.BaseEntity.ConcurrencyStamp"/>
/// est régénéré à chaque modification par <see cref="DPD.Infrastructure.Persistence.Interceptors.AuditInterceptor"/>
/// et vérifié par EF Core dans la clause WHERE de l'UPDATE généré. Si deux utilisateurs chargent la même
/// entité puis la modifient l'un après l'autre, la seconde sauvegarde doit échouer avec une
/// <see cref="DbUpdateConcurrencyException"/> (traduite en HTTP 409 par GlobalExceptionMiddleware).
/// Sur la cible de production (SQL Server), le même scénario est couvert nativement par la colonne
/// rowversion (<c>.IsRowVersion()</c>), incrémentée automatiquement par le moteur.
/// </summary>
public class ConcurrencyTests : IClassFixture<DpdWebApplicationFactory>
{
    private readonly DpdWebApplicationFactory _factory;

    public ConcurrencyTests(DpdWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SecondConcurrentUpdate_OnSameProject_ThrowsDbUpdateConcurrencyException()
    {
        // Prépare une entité de référence en base via un contexte "setup".
        Guid projectId;
        using (var setupScope = _factory.Services.CreateScope())
        {
            var setupDb = setupScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var project = new Project
            {
                Name = "Etude concurrence",
                ProjectCode = $"CONC-{Guid.NewGuid():N}".Substring(0, 12),
                ManagerId = Guid.NewGuid(),
                EstimatedMd = 10m
            };
            setupDb.Projects.Add(project);
            await setupDb.SaveChangesAsync();
            projectId = project.Id;
        }

        // Deux "utilisateurs" chargent indépendamment la même entité dans deux contextes distincts,
        // simulant deux requêtes HTTP concurrentes qui liraient l'entité avant que l'une des deux ne sauvegarde.
        using var scopeUserA = _factory.Services.CreateScope();
        using var scopeUserB = _factory.Services.CreateScope();
        var dbUserA = scopeUserA.ServiceProvider.GetRequiredService<AppDbContext>();
        var dbUserB = scopeUserB.ServiceProvider.GetRequiredService<AppDbContext>();

        var projectSeenByUserA = await dbUserA.Projects.SingleAsync(p => p.Id == projectId);
        var projectSeenByUserB = await dbUserB.Projects.SingleAsync(p => p.Id == projectId);

        // L'utilisateur A modifie et sauvegarde en premier : succès, le ConcurrencyStamp est régénéré.
        projectSeenByUserA.Name = "Etude concurrence - modifiée par Utilisateur A";
        await dbUserA.SaveChangesAsync();

        // L'utilisateur B modifie la version qu'il avait chargée (jeton désormais périmé) et sauvegarde :
        // EF Core génère un WHERE incluant l'ancien ConcurrencyStamp, 0 ligne est affectée en base.
        projectSeenByUserB.Name = "Etude concurrence - modifiée par Utilisateur B";
        var actUserBSave = async () => await dbUserB.SaveChangesAsync();

        await actUserBSave.Should().ThrowAsync<DbUpdateConcurrencyException>(
            "le jeton de concurrence chargé par l'utilisateur B est devenu obsolète après la sauvegarde de l'utilisateur A");

        // La valeur persistée en base reste celle de l'utilisateur A (premier arrivé, premier servi).
        using var scopeVerify = _factory.Services.CreateScope();
        var dbVerify = scopeVerify.ServiceProvider.GetRequiredService<AppDbContext>();
        var persisted = await dbVerify.Projects.SingleAsync(p => p.Id == projectId);
        persisted.Name.Should().Be("Etude concurrence - modifiée par Utilisateur A");
    }

    [Fact]
    public async Task SequentialUpdates_OnSameProject_DoNotThrow()
    {
        // Contrôle négatif : lorsque les modifications sont bien séquentielles (l'utilisateur B recharge
        // l'entité après la sauvegarde de l'utilisateur A), aucune exception de concurrence ne doit survenir.
        Guid projectId;
        using (var setupScope = _factory.Services.CreateScope())
        {
            var setupDb = setupScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var project = new Project
            {
                Name = "Etude sequentielle",
                ProjectCode = $"SEQ-{Guid.NewGuid():N}".Substring(0, 12),
                ManagerId = Guid.NewGuid(),
                EstimatedMd = 5m
            };
            setupDb.Projects.Add(project);
            await setupDb.SaveChangesAsync();
            projectId = project.Id;
        }

        using (var scopeUserA = _factory.Services.CreateScope())
        {
            var dbUserA = scopeUserA.ServiceProvider.GetRequiredService<AppDbContext>();
            var project = await dbUserA.Projects.SingleAsync(p => p.Id == projectId);
            project.Name = "Modifié par A";
            await dbUserA.SaveChangesAsync();
        }

        using (var scopeUserB = _factory.Services.CreateScope())
        {
            var dbUserB = scopeUserB.ServiceProvider.GetRequiredService<AppDbContext>();
            var project = await dbUserB.Projects.SingleAsync(p => p.Id == projectId);
            project.Name = "Modifié par B après A";
            var act = async () => await dbUserB.SaveChangesAsync();
            await act.Should().NotThrowAsync();
        }
    }
}
