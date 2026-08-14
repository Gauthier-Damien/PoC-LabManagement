using System.Security.Claims;
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
builder.Services.AddScoped(sp =>
{
    var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
    var request = httpContextAccessor.HttpContext?.Request;

    var handler = new HttpClientHandler();
    if (builder.Environment.IsDevelopment())
    {
        handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
    }

    var baseAddress = request is null
        ? "https://localhost:7218/"
        : $"{request.Scheme}://{request.Host}/";

    var client = new HttpClient(handler)
    {
        BaseAddress = new Uri(baseAddress)
    };

    var role = httpContextAccessor.HttpContext?.Request.Headers["X-Mock-Role"].FirstOrDefault();
    var userId = httpContextAccessor.HttpContext?.Request.Headers["X-Mock-UserId"].FirstOrDefault();
    var userName = httpContextAccessor.HttpContext?.Request.Headers["X-Mock-UserName"].FirstOrDefault();

    if (!string.IsNullOrWhiteSpace(role))
    {
        client.DefaultRequestHeaders.Add("X-Mock-Role", role);
    }

    if (!string.IsNullOrWhiteSpace(userId))
    {
        client.DefaultRequestHeaders.Add("X-Mock-UserId", userId);
    }

    if (!string.IsNullOrWhiteSpace(userName))
    {
        client.DefaultRequestHeaders.Add("X-Mock-UserName", userName);
    }

    var cookieHeader = httpContextAccessor.HttpContext?.Request.Headers["Cookie"].FirstOrDefault();
    if (!string.IsNullOrWhiteSpace(cookieHeader))
    {
        client.DefaultRequestHeaders.Add("Cookie", cookieHeader);
    }

    return client;
});
builder.Services.AddMudServices();

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddTransient<GlobalExceptionMiddleware>();

var authMode = builder.Configuration["Authentication:Mode"] ?? "Mock";
if (string.Equals(authMode, "Entra", StringComparison.OrdinalIgnoreCase))
{
    builder.Services
        .AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        })
        .AddCookie()
        .AddOpenIdConnect(options =>
        {
            builder.Configuration.GetSection("Authentication:Entra").Bind(options);
            options.Scope.Add("openid");
            options.Scope.Add("profile");
            options.SaveTokens = true;
        });
}
else
{
    builder.Services
        .AddAuthentication("Mock")
        .AddScheme<AuthenticationSchemeOptions, MockAuthenticationHandler>("Mock", _ => { });
}

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(PolicyNames.SubmitTimeEntry, policy =>
        policy.RequireRole("Scientist", "Manager", "Admin", "StandardUser"));

    options.AddPolicy(PolicyNames.ValidateTimesheet, policy =>
        policy.RequireRole("Manager", "Admin"));

    options.AddPolicy(PolicyNames.ModifyProjectStatus, policy =>
        policy.RequireRole("SD", "Manager", "Admin"));

    options.AddPolicy(PolicyNames.ReserveEquipment, policy =>
        policy.RequireRole("Scientist", "Manager", "Admin"));

    options.AddPolicy(PolicyNames.ManageMaintenanceContracts, policy =>
        policy.RequireRole("EquipmentOwner", "Admin"));

    options.AddPolicy(PolicyNames.ManageUsersRoles, policy =>
        policy.RequireRole("Admin"));
});

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

if (string.Equals(authMode, "Entra", StringComparison.OrdinalIgnoreCase) is false)
{
    app.MapGet("/mock/set-role/{role}", (string role, HttpContext http) =>
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
    });
}

app.MapControllers();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await AppDbInitializer.InitializeAsync(db);
}

app.Run();
