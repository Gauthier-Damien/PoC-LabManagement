namespace DPD.Application.Common.Interfaces;

public interface IUserService
{
    string UserName { get; }
    Guid UserId { get; }
    IReadOnlyCollection<string> GetRoles();
}
