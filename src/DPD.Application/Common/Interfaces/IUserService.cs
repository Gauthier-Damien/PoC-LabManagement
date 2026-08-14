namespace DPD.Application.Common.Interfaces;

public interface IUserService
{
    string UserName { get; }
    IReadOnlyCollection<string> Roles { get; }
    Guid UserId { get; }
}
