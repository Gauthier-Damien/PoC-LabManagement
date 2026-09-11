using DPD.Application.Common.Interfaces;
using DPD.Application.Modules.TimeTracking.Commands.SubmitTimesheet;
using DPD.Domain.Entities;
using DPD.Domain.Enums;
using FluentAssertions;
using FluentValidation;
using Moq;

namespace DPD.Application.Tests.Modules.TimeTracking;

public class SubmitTimesheetCommandHandlerTests
{
    private readonly Mock<IResourceRepository> _resources = new();
    private readonly Mock<IStudyRepository> _studies = new();
    private readonly Mock<ITimeEntryRepository> _timeEntries = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly SubmitTimesheetCommandHandler _handler;

    public SubmitTimesheetCommandHandlerTests()
    {
        _handler = new SubmitTimesheetCommandHandler(_resources.Object, _studies.Object, _timeEntries.Object, _unitOfWork.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesSubmittedEntry()
    {
        var resourceId = Guid.NewGuid();
        var studyId = Guid.NewGuid();
        _resources.Setup(r => r.ExistsAsync(resourceId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _studies.Setup(s => s.ExistsAsync(studyId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var command = new SubmitTimesheetCommand(resourceId, studyId, DateOnly.FromDateTime(DateTime.UtcNow), 7.5m);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeEmpty();
        _timeEntries.Verify(t => t.AddAsync(It.Is<TimeEntry>(e => e.Status == TimeEntryStatus.Submitted && e.Hours == 7.5m), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ResourceDoesNotExist_ThrowsValidationException()
    {
        _resources.Setup(r => r.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var command = new SubmitTimesheetCommand(Guid.NewGuid(), Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow), 5m);
        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Handle_StudyDoesNotExist_ThrowsValidationException()
    {
        _resources.Setup(r => r.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _studies.Setup(s => s.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var command = new SubmitTimesheetCommand(Guid.NewGuid(), Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow), 5m);
        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }
}
