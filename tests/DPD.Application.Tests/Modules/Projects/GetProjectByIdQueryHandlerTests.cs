using DPD.Application.Common.Interfaces;
using DPD.Application.Modules.Projects.Queries.GetProjectById;
using DPD.Domain.Entities;
using DPD.Domain.Enums;
using FluentAssertions;
using Mapster;
using MapsterMapper;
using Moq;

namespace DPD.Application.Tests.Modules.Projects;

public class GetProjectByIdQueryHandlerTests
{
    private readonly Mock<IProjectRepository> _projects = new();
    private readonly GetProjectByIdQueryHandler _handler;

    public GetProjectByIdQueryHandlerTests()
    {
        var config = new TypeAdapterConfig();
        config.Scan(typeof(DependencyInjection).Assembly);
        IMapper mapper = new ServiceMapper(Mock.Of<IServiceProvider>(), config);
        _handler = new GetProjectByIdQueryHandler(_projects.Object, mapper);
    }

    [Fact]
    public async Task Handle_ExistingProject_ReturnsDetailWithComputedIndicators()
    {
        var project = new Project
        {
            Name = "Liver Model",
            ProjectCode = "LM028",
            ManagerId = Guid.NewGuid(),
            EstimatedMd = 100,
            ActualMd = 40
        };
        project.Studies.Add(new Study
        {
            ProjectId = project.Id,
            Code = "ST01",
            StudyDirectorId = Guid.NewGuid(),
            Department = Department.AD,
            TargetDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EstimatedMd = 50,
            ActualMd = 20
        });

        _projects.Setup(p => p.FindWithStudiesAsync(project.Id, It.IsAny<CancellationToken>())).ReturnsAsync(project);

        var result = await _handler.Handle(new GetProjectByIdQuery(project.Id), CancellationToken.None);

        result.Code.Should().Be("LM028");
        result.RemainingMd.Should().Be(60);
        result.VarianceMd.Should().Be(-60);
        result.EstimateAtCompletion.Should().Be(100);
        result.Studies.Should().ContainSingle(s => s.Code == "ST01");
        result.DepartmentCharges.Should().ContainSingle(d => d.Department == Department.AD && d.ActualMd == 20);
        result.AllowedNextStatuses.Should().BeEquivalentTo([ProjectStatus.Quoted, ProjectStatus.Cancelled]);
    }

    [Fact]
    public async Task Handle_ProjectNotFound_ThrowsNotFoundException()
    {
        _projects.Setup(p => p.FindWithStudiesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Project?)null);

        var act = () => _handler.Handle(new GetProjectByIdQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<DPD.Application.Common.Exceptions.NotFoundException>();
    }
}
