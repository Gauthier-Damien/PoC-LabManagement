using DPD.Domain.Common;
using DPD.Domain.Entities;
using DPD.Domain.Enums;
using FluentAssertions;

namespace DPD.Domain.Tests;

/// <summary>
/// Vérifie le workflow complet Draft -> Submitted -> Approved/Rejected -> Locked (PRD 6.3).
/// </summary>
public class TimeEntryTests
{
    private static TimeEntry CreateEntry() => new()
    {
        ResourceId = Guid.NewGuid(),
        StudyId = Guid.NewGuid(),
        WorkDate = DateOnly.FromDateTime(DateTime.UtcNow),
        Hours = 7.5m
    };

    [Fact]
    public void Submit_FromDraft_SetsSubmitted()
    {
        var entry = CreateEntry();

        entry.Submit();

        entry.Status.Should().Be(TimeEntryStatus.Submitted);
    }

    [Fact]
    public void Submit_WhenLocked_Throws()
    {
        var entry = CreateEntry();
        entry.Submit();
        entry.Approve();
        entry.Lock();

        var act = entry.Submit;

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Approve_FromSubmitted_SetsApproved()
    {
        var entry = CreateEntry();
        entry.Submit();

        entry.Approve();

        entry.Status.Should().Be(TimeEntryStatus.Approved);
    }

    [Fact]
    public void Approve_FromDraft_Throws()
    {
        var entry = CreateEntry();

        var act = entry.Approve;

        act.Should().Throw<DomainException>().WithMessage("*submitted*");
    }

    [Fact]
    public void Reject_FromSubmitted_SetsRejectedWithComment()
    {
        var entry = CreateEntry();
        entry.Submit();

        entry.Reject("Heures incohérentes avec le planning.");

        entry.Status.Should().Be(TimeEntryStatus.Rejected);
        entry.RejectionComment.Should().Be("Heures incohérentes avec le planning.");
    }

    [Fact]
    public void Reject_FromDraft_Throws()
    {
        var entry = CreateEntry();

        var act = () => entry.Reject("motif");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Lock_FromApproved_SetsLocked()
    {
        var entry = CreateEntry();
        entry.Submit();
        entry.Approve();

        entry.Lock();

        entry.Status.Should().Be(TimeEntryStatus.Locked);
    }

    [Fact]
    public void Lock_IsIdempotentAndCanBeCalledFromAnyState()
    {
        var entry = CreateEntry();

        entry.Lock();

        entry.Status.Should().Be(TimeEntryStatus.Locked);
    }
}
