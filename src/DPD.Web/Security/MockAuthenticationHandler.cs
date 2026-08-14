using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace DPD.Web.Security;

public sealed class MockAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public MockAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var role = Request.Headers["X-Mock-Role"].FirstOrDefault()
            ?? Request.Cookies["mock_role"]
            ?? "Scientist";
        var userId = Request.Headers["X-Mock-UserId"].FirstOrDefault()
            ?? Request.Cookies["mock_userid"]
            ?? "11111111-1111-1111-1111-111111111111";
        var userName = Request.Headers["X-Mock-UserName"].FirstOrDefault()
            ?? Request.Cookies["mock_username"]
            ?? "scientist.local@dpd.test";

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, userName),
            new(ClaimTypes.Email, userName),
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Role, role)
        };

        if (role == "Admin")
        {
            claims.Add(new Claim(ClaimTypes.Role, "Manager"));
            claims.Add(new Claim(ClaimTypes.Role, "EquipmentOwner"));
        }

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
