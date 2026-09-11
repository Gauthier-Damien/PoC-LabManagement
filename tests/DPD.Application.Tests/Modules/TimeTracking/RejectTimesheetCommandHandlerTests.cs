using Moq;

namespace DPD.Application.Tests.Modules.TimeTracking;

public class RejectTimesheetCommandHandlerTests
{
    private readonly Moq.Mock<DPD.Application.Common.Interfaces.ITimeEntryRepository> _timeEntries = new();
    private readonly Moq.Mock<DPD.Application.Common.Interfaces.IResourceRepository> _resources = new();
    private readonly Moq.Mock<DPD.Application.Common.Interfaces.IUnitOfWork> _unitOfWork = new();
    private readonly DPD.Application.Modules.TimeTracking.Commands.RejectTimesheet.RejectTimesheetCommandHandler _handler;

    public RejectTimesheetCommandHandlerTests()
    {
        _handler = new DPD.Application.Modules.TimeTracking.Commands.RejectTimesheet.RejectTimesheetCommandHandler(_timeEntries.Object, _resources.Object, _unitOfWork.Object);
    }

    [Fact]
    public async Task Handle_EmptyComment_ThrowsBusinessException()
    {
        var act = () => _handler.Handle(new DPD.Application.Modules.TimeTracking.Commands.RejectTimesheet.RejectTimesheetCommand(Guid.NewGuid(), Guid.NewGuid(), ""), CancellationToken.None);

        await FluentAssertions.AssertionExtensions.Should(act).ThrowAsync<DPD.Application.Common.Exceptions.BusinessException>();
    }

    [Fact]
    public async Task Handle_ValidRejection_SetsRejectedWithComment()
    {
        var approverId = Guid.NewGuid();
        var entry = new DPD.Domain.Entities.TimeEntry { ResourceId = Guid.NewGuid(), StudyId = Guid.NewGuid(), WorkDate = DateOnly.FromDateTime(DateTime.UtcNow), Hours = 8 };
        entry.Submit();

        _resources.Setup(r => r.ExistsAsync(approverId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _timeEntries.Setup(t => t.FindAsync(entry.Id, It.IsAny<CancellationToken>())).ReturnsAsync(entry);

        await _handler.Handle(new DPD.Application.Modules.TimeTracking.Commands.RejectTimesheet.RejectTimesheetCommand(entry.Id, approverId, "Motif de rejet"), CancellationToken.None);

        FluentAssertions.AssertionExtensions.Should(entry.Status).Be(DPD.Domain.Enums.TimeEntryStatus.Rejected);
        FluentAssertions.AssertionExtensions.Should(entry.RejectionComment).Be("Motif de rejet");
    }
}
