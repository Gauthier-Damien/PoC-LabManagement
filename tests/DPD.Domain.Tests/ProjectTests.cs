using DPD.Domain.Common;
using DPD.Domain.Entities;
using DPD.Domain.Enums;
using FluentAssertions;

namespace DPD.Domain.Tests;

/// <summary>
/// Vérifie la machine à états du cycle de vie des projets (PRD 6.4) : un projet "vivant" peut
/// naviguer entre ses statuts actifs, mais les états terminaux (Completed/Cancelled/Lost) sont
/// définitivement archivés.
/// </summary>
public class ProjectTests
{
    private static Project CreateProject() => new()
    {
        Name = "Test Project",
        ProjectCode = "LM999",
        ManagerId = Guid.NewGuid(),
        EstimatedMd = 100,
        ActualMd = 20
    };

    [Theory]
    [InlineData(ProjectStatus.Hypothesis, ProjectStatus.Quoted)]
    [InlineData(ProjectStatus.Hypothesis, ProjectStatus.Cancelled)]
    [InlineData(ProjectStatus.Quoted, ProjectStatus.Signed)]
    [InlineData(ProjectStatus.Quoted, ProjectStatus.Hypothesis)]
    [InlineData(ProjectStatus.Quoted, ProjectStatus.Lost)]
    [InlineData(ProjectStatus.Signed, ProjectStatus.OnGoing)]
    [InlineData(ProjectStatus.Signed, ProjectStatus.OnHold)]
    [InlineData(ProjectStatus.Signed, ProjectStatus.Cancelled)]
    [InlineData(ProjectStatus.OnGoing, ProjectStatus.OnHold)]
    [InlineData(ProjectStatus.OnGoing, ProjectStatus.Completed)]
    [InlineData(ProjectStatus.OnGoing, ProjectStatus.Cancelled)]
    [InlineData(ProjectStatus.OnHold, ProjectStatus.OnGoing)]
    [InlineData(ProjectStatus.OnHold, ProjectStatus.Cancelled)]
    public void ChangeStatus_ValidTransition_Succeeds(ProjectStatus from, ProjectStatus to)
    {
        var project = CreateProject();
        SetStatus(project, from);

        project.ChangeStatus(to);

        project.Status.Should().Be(to);
    }

    [Theory]
    [InlineData(ProjectStatus.Hypothesis, ProjectStatus.Signed)]
    [InlineData(ProjectStatus.Hypothesis, ProjectStatus.OnGoing)]
    [InlineData(ProjectStatus.Quoted, ProjectStatus.OnGoing)]
    [InlineData(ProjectStatus.Signed, ProjectStatus.Hypothesis)]
    [InlineData(ProjectStatus.OnGoing, ProjectStatus.Hypothesis)]
    [InlineData(ProjectStatus.OnHold, ProjectStatus.Signed)]
    public void ChangeStatus_InvalidTransition_Throws(ProjectStatus from, ProjectStatus to)
    {
        var project = CreateProject();
        SetStatus(project, from);

        var act = () => project.ChangeStatus(to);

        act.Should().Throw<DomainException>().WithMessage("*Transition de statut invalide*");
    }

    [Theory]
    [InlineData(ProjectStatus.Completed)]
    [InlineData(ProjectStatus.Cancelled)]
    [InlineData(ProjectStatus.Lost)]
    public void ChangeStatus_FromTerminalState_AlwaysThrows(ProjectStatus terminal)
    {
        var project = CreateProject();
        SetStatus(project, terminal);

        var act = () => project.ChangeStatus(ProjectStatus.OnGoing);

        act.Should().Throw<DomainException>();
        project.IsArchived.Should().BeTrue();
        project.GetAllowedNextStatuses().Should().BeEmpty();
    }

    [Fact]
    public void ChangeStatus_ToSameStatus_IsNoOpAndDoesNotThrow()
    {
        var project = CreateProject();
        SetStatus(project, ProjectStatus.Signed);

        var act = () => project.ChangeStatus(ProjectStatus.Signed);

        act.Should().NotThrow();
        project.Status.Should().Be(ProjectStatus.Signed);
    }

    [Theory]
    [InlineData(ProjectStatus.Signed, true)]
    [InlineData(ProjectStatus.OnGoing, true)]
    [InlineData(ProjectStatus.Hypothesis, false)]
    [InlineData(ProjectStatus.Quoted, false)]
    [InlineData(ProjectStatus.OnHold, false)]
    public void ConsumesCapacity_OnlyForSignedOrOnGoing(ProjectStatus status, bool expected)
    {
        var project = CreateProject();
        SetStatus(project, status);

        project.ConsumesCapacity.Should().Be(expected);
    }

    [Fact]
    public void GetAllowedNextStatuses_FromHypothesis_ReturnsQuotedAndCancelled()
    {
        var project = CreateProject();

        project.GetAllowedNextStatuses().Should().BeEquivalentTo([ProjectStatus.Quoted, ProjectStatus.Cancelled]);
    }

    /// <summary>Utilise la réflexion pour positionner directement le statut interne (setter privé) dans les tests.</summary>
    private static void SetStatus(Project project, ProjectStatus status)
    {
        typeof(Project).GetProperty(nameof(Project.Status))!.SetValue(project, status);
    }
}
