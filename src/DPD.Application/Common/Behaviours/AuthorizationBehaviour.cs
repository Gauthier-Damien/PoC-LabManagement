using DPD.Application.Common.Exceptions;
using DPD.Application.Common.Interfaces;
using MediatR;

namespace DPD.Application.Common.Behaviours;

/// <summary>
/// Applique le contrôle RBAC (ADR-005) au niveau de la couche Application, en plus des
/// Authorization Policies déjà vérifiées côté Blazor/Controllers. Toute Command/Query
/// implémentant <see cref="IRequireRoles"/> est vérifiée contre les rôles de l'utilisateur courant.
/// </summary>
public sealed class AuthorizationBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IUserService _userService;

    public AuthorizationBehaviour(IUserService userService)
    {
        _userService = userService;
    }

    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is IRequireRoles requireRoles && requireRoles.AllowedRoles.Count > 0)
        {
            var roles = _userService.GetRoles();
            var hasRole = roles.Any(role => requireRoles.AllowedRoles.Contains(role, StringComparer.OrdinalIgnoreCase));
            if (!hasRole)
            {
                throw new ForbiddenAccessException(
                    $"L'utilisateur '{_userService.UserName}' (rôles: {string.Join(", ", roles)}) " +
                    $"n'a pas l'un des rôles requis pour '{typeof(TRequest).Name}': {string.Join(", ", requireRoles.AllowedRoles)}.");
            }
        }

        return next();
    }
}
