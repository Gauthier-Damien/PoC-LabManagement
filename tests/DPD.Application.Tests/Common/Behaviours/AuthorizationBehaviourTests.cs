using DPD.Application.Common.Behaviours;
using DPD.Application.Common.Exceptions;
using DPD.Application.Common.Interfaces;
using FluentAssertions;
using MediatR;
using Moq;

namespace DPD.Application.Tests.Common.Behaviours;

public class AuthorizationBehaviourTests
{
    private sealed record RestrictedRequest : IRequest<string>, IRequireRoles
    {
        public IReadOnlyCollection<string> AllowedRoles => ["Manager", "Admin"];
    }

    private sealed record OpenRequest : IRequest<string>;

    private readonly Mock<IUserService> _userService = new();

    [Fact]
    public async Task Handle_UserHasAllowedRole_CallsNext()
    {
        _userService.Setup(u => u.GetRoles()).Returns(["Manager"]);
        _userService.Setup(u => u.UserName).Returns("test.user@dpd.test");
        var behaviour = new AuthorizationBehaviour<RestrictedRequest, string>(_userService.Object);

        var result = await behaviour.Handle(new RestrictedRequest(), () => Task.FromResult("ok"), CancellationToken.None);

        result.Should().Be("ok");
    }

    [Fact]
    public async Task Handle_UserLacksRole_ThrowsForbiddenAccessException()
    {
        _userService.Setup(u => u.GetRoles()).Returns(["Scientist"]);
        _userService.Setup(u => u.UserName).Returns("test.user@dpd.test");
        var behaviour = new AuthorizationBehaviour<RestrictedRequest, string>(_userService.Object);

        var act = () => behaviour.Handle(new RestrictedRequest(), () => Task.FromResult("ok"), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Handle_RequestWithoutRoleRestriction_AlwaysCallsNext()
    {
        _userService.Setup(u => u.GetRoles()).Returns(["ReadOnly"]);
        var behaviour = new AuthorizationBehaviour<OpenRequest, string>(_userService.Object);

        var result = await behaviour.Handle(new OpenRequest(), () => Task.FromResult("ok"), CancellationToken.None);

        result.Should().Be("ok");
    }
}
