using System.Security.Claims;
using DPD.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace DPD.Infrastructure.Security;

/// <summary>
/// Implémentation PoC de <see cref="IUserService"/> qui reflète l'utilisateur mock courant à
/// partir des claims posées par MockAuthenticationHandler (elles-mêmes dérivées du cookie/en-tête
/// de rôle sélectionné dans le NavMenu). Ceci garantit que le RBAC applicatif (AuthorizationBehaviour)
/// est cohérent avec le rôle affiché et les Authorization Policies côté Web.
/// </summary>
public sealed class MockUserService : IUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public MockUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public string UserName => User?.FindFirst(ClaimTypes.Name)?.Value ?? "scientist.local@dpd.test";

    public IReadOnlyCollection<string> GetRoles() =>
        User?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray() ?? ["Scientist"];

    public Guid UserId =>
        Guid.TryParse(User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id)
            ? id
            : Guid.Parse("11111111-1111-1111-1111-111111111111");
}


