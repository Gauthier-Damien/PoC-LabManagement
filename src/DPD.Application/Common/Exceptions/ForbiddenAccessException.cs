namespace DPD.Application.Common.Exceptions;

/// <summary>
/// Levée lorsque l'utilisateur courant n'a pas l'un des rôles requis pour exécuter une Command/Query
/// marquée par <see cref="Interfaces.IRequireRoles"/>. Mappée en HTTP 403 par le middleware global.
/// </summary>
public sealed class ForbiddenAccessException : Exception
{
    public ForbiddenAccessException(string message) : base(message)
    {
    }
}
