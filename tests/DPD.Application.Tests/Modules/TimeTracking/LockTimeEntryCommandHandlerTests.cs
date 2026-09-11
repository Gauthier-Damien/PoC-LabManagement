using DPD.Application.Common.Interfaces;
using DPD.Application.Modules.TimeTracking.Commands.LockTimeEntry;
using DPD.Domain.Entities;
using DPD.Domain.Enums;
using FluentAssertions;
using Moq;

namespace DPD.Application.Tests.Modules.TimeTracking;

public class LockTimeEntryCommandHandlerTests
{
    private readonly Mock<ITimeEntryRepository> _timeEntries = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly LockTimeEntryCommandHandler _handler;

    public LockTimeEntryCommandHandlerTests()
    {
        _handler = new LockTimeEntryCommandHandler(_timeEntries.Object, _unitOfWork.Object);
    }

    [Fact]
    public async Task Handle_ExistingEntry_LocksIt()
    {
        var entry = new TimeEntry { ResourceId = Guid.NewGuid(), StudyId = Guid.NewGuid(), WorkDate = DateOnly.FromDateTime(DateTime.UtcNow), Hours = 8 };
        _timeEntries.Setup(t => t.FindAsync(entry.Id, It.IsAny<CancellationToken>())).ReturnsAsync(entry);

        await _handler.Handle(new LockTimeEntryCommand(entry.Id), CancellationToken.None);

        entry.Status.Should().Be(TimeEntryStatus.Locked);
    }

    [Fact]
    public void AllowedRoles_IsAdminOnly()
    {
        var command = new LockTimeEntryCommand(Guid.NewGuid());

        command.AllowedRoles.Should().BeEquivalentTo("Admin");
    }
}
