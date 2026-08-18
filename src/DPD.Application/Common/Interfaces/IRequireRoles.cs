namespace DPD.Application.Common.Interfaces;

/// <summary>
/// Marqueur optionnel pour les Commands/Queries MediatR qui doivent être restreintes
/// à un sous-ensemble de rôles RBAC, appliqué par <see cref="Behaviours.AuthorizationBehaviour{TRequest,TResponse}"/>.
/// Ceci garantit une défense en profondeur : le contrôle n'est pas uniquement fait
/// au niveau Blazor/Controller (Policies ASP.NET Core), mais aussi dans la couche Application (CQRS),
/// conformément à l'ADR-005 (RBAC & Autorisations par Politiques).
/// </summary>
public interface IRequireRoles
{
    IReadOnlyCollection<string> AllowedRoles { get; }
}
