using DPD.Application.Common.Interfaces;
using DPD.Application.Modules.Projects.Commands.ChangeProjectStatus;
using DPD.Application.Modules.Projects.Commands.CreateProject;
using DPD.Domain.Entities;
using DPD.Domain.Enums;
using FluentAssertions;
using FluentValidation;
using Moq;

namespace DPD.Application.Tests.Modules.Projects;

public class CreateProjectCommandHandlerTests
{
    private readonly Mock<IProjectRepository> _projects = new();
    private readonly Mock<IResourceRepository> _resources = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly CreateProjectCommandHandler _handler;

    public CreateProjectCommandHandlerTests()
    {
        _handler = new CreateProjectCommandHandler(_projects.Object, _resources.Object, _unitOfWork.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesProjectAndSaves()
    {
        var managerId = Guid.NewGuid();
        _resources.Setup(r => r.ExistsAsync(managerId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _projects.Setup(p => p.ExistsByCodeAsync("LM100", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var command = new CreateProjectCommand("New Project", "LM100", managerId, 150);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeEmpty();
        _projects.Verify(p => p.AddAsync(It.Is<Project>(x => x.ProjectCode == "LM100" && x.ManagerId == managerId), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ManagerDoesNotExist_ThrowsValidationException()
    {
        _resources.Setup(r => r.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var command = new CreateProjectCommand("New Project", "LM100", Guid.NewGuid(), 150);
        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DuplicateProjectCode_ThrowsValidationException()
    {
        var managerId = Guid.NewGuid();
        _resources.Setup(r => r.ExistsAsync(managerId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _projects.Setup(p => p.ExistsByCodeAsync("LM100", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var command = new CreateProjectCommand("New Project", "LM100", managerId, 150);
        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public void AllowedRoles_RestrictedToSdManagerAdmin()
    {
        var command = new CreateProjectCommand("N", "C", Guid.NewGuid(), 1);

        command.AllowedRoles.Should().BeEquivalentTo("SD", "Manager", "Admin");
    }
}

public class ChangeProjectStatusCommandHandlerTests
{
    private readonly Mock<IProjectRepository> _projects = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly ChangeProjectStatusCommandHandler _handler;

    public ChangeProjectStatusCommandHandlerTests()
    {
        _handler = new ChangeProjectStatusCommandHandler(_projects.Object, _unitOfWork.Object);
    }

    [Fact]
    public async Task Handle_ValidTransition_ChangesStatusAndSaves()
    {
        var project = new Project { Name = "P", ProjectCode = "LM001", ManagerId = Guid.NewGuid(), EstimatedMd = 10 };
        _projects.Setup(p => p.FindAsync(project.Id, It.IsAny<CancellationToken>())).ReturnsAsync(project);

        await _handler.Handle(new ChangeProjectStatusCommand(project.Id, ProjectStatus.Quoted), CancellationToken.None);

        project.Status.Should().Be(ProjectStatus.Quoted);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ProjectNotFound_ThrowsNotFoundException()
    {
        _projects.Setup(p => p.FindAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Project?)null);

        var act = () => _handler.Handle(new ChangeProjectStatusCommand(Guid.NewGuid(), ProjectStatus.Quoted), CancellationToken.None);

        await act.Should().ThrowAsync<DPD.Application.Common.Exceptions.NotFoundException>();
    }

    [Fact]
    public async Task Handle_InvalidTransition_ThrowsDomainExceptionAndDoesNotSave()
    {
        var project = new Project { Name = "P", ProjectCode = "LM001", ManagerId = Guid.NewGuid(), EstimatedMd = 10 };
        _projects.Setup(p => p.FindAsync(project.Id, It.IsAny<CancellationToken>())).ReturnsAsync(project);

        var act = () => _handler.Handle(new ChangeProjectStatusCommand(project.Id, ProjectStatus.OnGoing), CancellationToken.None);

        await act.Should().ThrowAsync<DPD.Domain.Common.DomainException>();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
