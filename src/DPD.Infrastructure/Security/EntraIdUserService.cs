using System.Security.Claims;
using DPD.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace DPD.Infrastructure.Security;

public sealed class EntraIdUserService : IUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public EntraIdUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string UserName =>
        _httpContextAccessor.HttpContext?.User?.Identity?.Name ??
        _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email) ??
        "unknown";

    public IReadOnlyCollection<string> GetRoles() =>
        _httpContextAccessor.HttpContext?.User?.FindAll(ClaimTypes.Role).Select(x => x.Value).ToArray() ?? [];

    public Guid UserId
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var parsed) ? parsed : Guid.Empty;
        }
    }
}
