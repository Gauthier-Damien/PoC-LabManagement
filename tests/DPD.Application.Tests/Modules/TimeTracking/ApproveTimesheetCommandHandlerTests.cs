using DPD.Application.Common.Interfaces;
using DPD.Application.Modules.TimeTracking.Commands.ApproveTimesheet;
using DPD.Domain.Entities;
using DPD.Domain.Enums;
using FluentAssertions;
using Moq;

namespace DPD.Application.Tests.Modules.TimeTracking;

public class ApproveTimesheetCommandHandlerTests
{
    private readonly Mock<ITimeEntryRepository> _timeEntries = new();
    private readonly Mock<IResourceRepository> _resources = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly ApproveTimesheetCommandHandler _handler;

    public ApproveTimesheetCommandHandlerTests()
    {
        _handler = new ApproveTimesheetCommandHandler(_timeEntries.Object, _resources.Object, _unitOfWork.Object);
    }

    [Fact]
    public async Task Handle_ValidApproval_SetsApprovedAndSaves()
    {
        var approverId = Guid.NewGuid();
        var entry = new TimeEntry { ResourceId = Guid.NewGuid(), StudyId = Guid.NewGuid(), WorkDate = DateOnly.FromDateTime(DateTime.UtcNow), Hours = 8 };
        entry.Submit();

        _resources.Setup(r => r.ExistsAsync(approverId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _timeEntries.Setup(t => t.FindAsync(entry.Id, It.IsAny<CancellationToken>())).ReturnsAsync(entry);

        await _handler.Handle(new ApproveTimesheetCommand(entry.Id, approverId), CancellationToken.None);

        entry.Status.Should().Be(TimeEntryStatus.Approved);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ApproverDoesNotExist_ThrowsBusinessException()
    {
        _resources.Setup(r => r.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var act = () => _handler.Handle(new ApproveTimesheetCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<DPD.Application.Common.Exceptions.BusinessException>();
    }

    [Fact]
    public void AllowedRoles_IncludesManagerAdminDepartmentResponsible()
    {
        var command = new ApproveTimesheetCommand(Guid.NewGuid(), Guid.NewGuid());

        command.AllowedRoles.Should().BeEquivalentTo(["Manager", "Admin", "DepartmentResponsible"]);
    }
}
