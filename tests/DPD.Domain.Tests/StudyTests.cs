using DPD.Domain.Common;
using DPD.Domain.Entities;
using DPD.Domain.Enums;
using FluentAssertions;

namespace DPD.Domain.Tests;

public class StudyTests
{
    private static Study CreateStudy() => new()
    {
        ProjectId = Guid.NewGuid(),
        Code = "ST01",
        StudyDirectorId = Guid.NewGuid(),
        Department = Department.FPD,
        TargetDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(2)),
        EstimatedMd = 40,
        ActualMd = 10
    };

    [Fact]
    public void ChangeStatus_ToInProgress_UpdatesStatus()
    {
        var study = CreateStudy();

        study.ChangeStatus(StudyStatus.InProgress);

        study.Status.Should().Be(StudyStatus.InProgress);
    }

    [Fact]
    public void ChangeStatus_ToCompleted_SetsActualDateIfNotAlreadySet()
    {
        var study = CreateStudy();
        study.ActualDate.Should().BeNull();

        study.ChangeStatus(StudyStatus.Completed);

        study.Status.Should().Be(StudyStatus.Completed);
        study.ActualDate.Should().Be(DateOnly.FromDateTime(DateTime.UtcNow));
    }

    [Fact]
    public void ChangeStatus_ToCompleted_DoesNotOverwriteExistingActualDate()
    {
        var study = CreateStudy();
        var existingDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5));
        study.ActualDate = existingDate;

        study.ChangeStatus(StudyStatus.Completed);

        study.ActualDate.Should().Be(existingDate);
    }

    [Theory]
    [InlineData(StudyStatus.Completed)]
    [InlineData(StudyStatus.Cancelled)]
    public void ChangeStatus_FromArchivedState_Throws(StudyStatus archivedStatus)
    {
        var study = CreateStudy();
        typeof(Study).GetProperty(nameof(Study.Status))!.SetValue(study, archivedStatus);

        var act = () => study.ChangeStatus(StudyStatus.InProgress);

        act.Should().Throw<DomainException>();
    }
}
