using System.Text.Json.Serialization;
using DPD.Application;
using DPD.Infrastructure;
using DPD.Infrastructure.Persistence;
using DPD.Web.Authorization;
using DPD.Web.Components;
using DPD.Web.Middleware;
using DPD.Web.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using MudBlazor.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration);
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped(sp => CreateMockAwareHttpClient(sp, builder.Environment));
builder.Services.AddMudServices();

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddTransient<GlobalExceptionMiddleware>();

var authMode = builder.Configuration["Authentication:Mode"] ?? "Mock";
var isEntraMode = string.Equals(authMode, "Entra", StringComparison.OrdinalIgnoreCase);

ConfigureAuthentication(builder.Services, builder.Configuration, isEntraMode);
builder.Services.AddAuthorization(ConfigureAuthorizationPolicies);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseSerilogRequestLogging();
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

if (!isEntraMode)
{
    app.MapGet("/mock/set-role/{role}", MapSetMockRole);
}

app.MapControllers();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await AppDbInitializer.InitializeAsync(db);
}

await app.RunAsync();

// Construit le HttpClient utilisé par les composants Blazor Server pour appeler l'API interne,
// en propageant les en-têtes d'identité mock (rôle/utilisateur) et le cookie de session courants,
// afin que les appels HTTP internes soient authentifiés avec le même contexte que la requête initiale.
static HttpClient CreateMockAwareHttpClient(IServiceProvider sp, IWebHostEnvironment environment)
{
    var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
    var request = httpContextAccessor.HttpContext?.Request;

    var handler = new HttpClientHandler();
    if (environment.IsDevelopment())
    {
        handler.ServerCertificateCustomValidationCallback = ValidateDevelopmentLoopbackCertificate;
    }

    var baseAddress = request is null
        ? "https://localhost:7218/"
        : $"{request.Scheme}://{request.Host}/";

    var client = new HttpClient(handler)
    {
        BaseAddress = new Uri(baseAddress)
    };

    CopyMockIdentityHeaders(httpContextAccessor.HttpContext, client);

    return client;
}

// Callback de validation de certificat volontairement permissif, mais restreint au strict nécessaire :
// il n'est câblé que lorsque l'environnement est Development (jamais en production, cf. appel ci-dessus)
// et ne sert qu'à accepter le certificat HTTPS auto-signé généré par le SDK .NET pour le développement
// local (dotnet dev-certs). Aucune validation d'hôte n'est nécessaire ici car ce HttpClient n'est utilisé
// que pour rappeler l'application elle-même sur son propre BaseAddress (localhost), jamais un tiers.
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Security",
    "S4830:Server certificates should be verified during SSL/TLS connections",
    Justification = "Bypass volontaire et limité à l'environnement Development pour accepter le certificat HTTPS auto-signé de dotnet dev-certs, utilisé uniquement pour les appels internes de l'app vers elle-même (localhost). Jamais actif en production (cf. IsDevelopment()).")]
static bool ValidateDevelopmentLoopbackCertificate(
    HttpRequestMessage requestMessage,
    System.Security.Cryptography.X509Certificates.X509Certificate2? certificate,
    System.Security.Cryptography.X509Certificates.X509Chain? chain,
    System.Net.Security.SslPolicyErrors sslPolicyErrors) => true;

static void CopyMockIdentityHeaders(HttpContext? httpContext, HttpClient client)
{
    AddHeaderIfPresent(httpContext, client, "X-Mock-Role");
    AddHeaderIfPresent(httpContext, client, "X-Mock-UserId");
    AddHeaderIfPresent(httpContext, client, "X-Mock-UserName");
    AddHeaderIfPresent(httpContext, client, "Cookie");
}

static void AddHeaderIfPresent(HttpContext? httpContext, HttpClient client, string headerName)
{
    var value = httpContext?.Request.Headers[headerName].FirstOrDefault();
    if (!string.IsNullOrWhiteSpace(value))
    {
        client.DefaultRequestHeaders.Add(headerName, value);
    }
}

// Configure le schéma d'authentification actif : OpenID Connect + Cookie pour Entra ID (production),
// ou le schéma Mock (PoC) piloté par l'en-tête/cookie de rôle.
static void ConfigureAuthentication(IServiceCollection services, IConfiguration configuration, bool isEntraMode)
{
    if (isEntraMode)
    {
        services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie()
            .AddOpenIdConnect(options =>
            {
                configuration.GetSection("Authentication:Entra").Bind(options);
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.SaveTokens = true;
            });
    }
    else
    {
        services
            .AddAuthentication("Mock")
            .AddScheme<AuthenticationSchemeOptions, MockAuthenticationHandler>("Mock", _ => { });
    }
}

// 8 rôles RBAC prévus par le PRD/SAD : Admin, Manager, EquipmentOwner (Responsable Équipement),
// DepartmentResponsible (Responsable Département), SD (Study Director), PrincipalInvestigator (PI),
// StandardUser/Scientist (Utilisateur Standard), ReadOnly (Lecture seule - aucune policy d'écriture).
static void ConfigureAuthorizationPolicies(Microsoft.AspNetCore.Authorization.AuthorizationOptions options)
{
    const string admin = "Admin";
    const string manager = "Manager";

    options.AddPolicy(PolicyNames.SubmitTimeEntry, policy =>
        policy.RequireRole("Scientist", manager, admin, "StandardUser", "PrincipalInvestigator", "SD"));

    options.AddPolicy(PolicyNames.ValidateTimesheet, policy =>
        policy.RequireRole(manager, admin, "DepartmentResponsible"));

    options.AddPolicy(PolicyNames.ModifyProjectStatus, policy =>
        policy.RequireRole("SD", manager, admin));

    options.AddPolicy(PolicyNames.ManageStudies, policy =>
        policy.RequireRole("SD", "PrincipalInvestigator", admin));

    options.AddPolicy(PolicyNames.ReserveEquipment, policy =>
        policy.RequireRole("Scientist", manager, admin, "PrincipalInvestigator", "StandardUser"));

    options.AddPolicy(PolicyNames.ManageMaintenanceContracts, policy =>
        policy.RequireRole("EquipmentOwner", admin));

    options.AddPolicy(PolicyNames.ManageUsersRoles, policy =>
        policy.RequireRole(admin));
}

// Endpoint PoC permettant de changer de rôle mock : pose les cookies d'identité puis redirige vers
// la page d'origine (reconstruite sur l'hôte courant, jamais sur l'autorité brute du Referer) afin
// que la page se recharge avec les nouvelles claims/policies.
static IResult MapSetMockRole(string role, HttpContext http)
{
    var cookieOptions = new CookieOptions
    {
        Path = "/",
        HttpOnly = false,
        SameSite = SameSiteMode.Lax,
        Expires = DateTimeOffset.UtcNow.AddDays(7)
    };

    http.Response.Cookies.Append("mock_role", role, cookieOptions);

    var userId = role switch
    {
        "Manager" => "22222222-2222-2222-2222-222222222222",
        "Admin" => "33333333-3333-3333-3333-333333333333",
        _ => "11111111-1111-1111-1111-111111111111"
    };
    var userName = $"{role.ToLowerInvariant()}.local@dpd.test";

    http.Response.Cookies.Append("mock_userid", userId, cookieOptions);
    http.Response.Cookies.Append("mock_username", userName, cookieOptions);

    // Ne pas réutiliser l'autorité (schéma+hôte+port) du Referer : elle peut pointer vers
    // un ancien profil de lancement (ex: IIS Express sur un autre port) et mener nulle part.
    // On ne conserve que le chemin + la query, en reconstruisant l'URL sur l'hôte courant.
    var referer = http.Request.Headers["Referer"].FirstOrDefault();
    var redirectPath = "/";
    if (!string.IsNullOrWhiteSpace(referer) && Uri.TryCreate(referer, UriKind.Absolute, out var refererUri))
    {
        redirectPath = refererUri.PathAndQuery;
    }

    return Results.Redirect(redirectPath);
}

/// <summary>
/// Point d'entrée exposé en classe partielle pour permettre à WebApplicationFactory&lt;Program&gt;
/// (tests d'intégration) de démarrer l'application en mémoire. Constructeur protégé : cette classe
/// n'est instanciée que par l'infrastructure ASP.NET Core / le framework de tests, jamais directement.
/// </summary>
public partial class Program
{
    protected Program()
    {
    }
}
