using DPD.Application.Common.Interfaces;

namespace DPD.Infrastructure.Security;

public sealed class MockUserService : IUserService
{
    public string UserName => "scientist.local@dpd.test";
    public IReadOnlyCollection<string> Roles => ["Scientist"];
    public Guid UserId => Guid.Parse("11111111-1111-1111-1111-111111111111");
}
