using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace DPD.Integration.Tests;

/// <summary>
/// Démarre l'application complète (Program.cs) en mémoire avec une base SQLite temporaire dédiée
/// (isolée du fichier de développement dpd-poc.db) et l'authentification Mock activée, permettant
/// de tester le pipeline HTTP complet : Middleware, Authentication, Authorization, Controllers,
/// MediatR (Validation + Authorization Behaviours), EF Core (Audit/Soft Delete/RowVersion).
/// </summary>
public sealed class DpdWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"dpd-tests-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Sqlite"] = $"Data Source={_dbPath}",
                ["Authentication:Mode"] = "Mock"
            });
        });
    }

    /// <summary>
    /// Crée un HttpClient qui envoie automatiquement le rôle mock souhaité via l'en-tête X-Mock-Role,
    /// lu par MockAuthenticationHandler pour construire les claims de l'utilisateur courant.
    /// </summary>
    public HttpClient CreateClientWithRole(string role)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Mock-Role", role);
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (File.Exists(_dbPath))
        {
            try
            {
                File.Delete(_dbPath);
            }
            catch (IOException)
            {
                // Le fichier peut rester verrouillé brièvement par le pool de connexions SQLite ; sans impact sur les tests.
            }
        }
    }
}
