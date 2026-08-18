using DPD.Application.Common.Behaviours;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Moq;

namespace DPD.Application.Tests.Common.Behaviours;

public class ValidationBehaviourTests
{
    private sealed record SampleRequest(string Name) : IRequest<string>;

    [Fact]
    public async Task Handle_NoValidators_CallsNext()
    {
        var behaviour = new ValidationBehaviour<SampleRequest, string>([]);

        var result = await behaviour.Handle(new SampleRequest("x"), () => Task.FromResult("ok"), CancellationToken.None);

        result.Should().Be("ok");
    }

    [Fact]
    public async Task Handle_ValidatorPasses_CallsNext()
    {
        var validator = new Mock<IValidator<SampleRequest>>();
        validator.Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<SampleRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var behaviour = new ValidationBehaviour<SampleRequest, string>([validator.Object]);

        var result = await behaviour.Handle(new SampleRequest("x"), () => Task.FromResult("ok"), CancellationToken.None);

        result.Should().Be("ok");
    }

    [Fact]
    public async Task Handle_ValidatorFails_ThrowsValidationException()
    {
        var failures = new List<ValidationFailure> { new("Name", "Name is required") };
        var validator = new Mock<IValidator<SampleRequest>>();
        validator.Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<SampleRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(failures));

        var behaviour = new ValidationBehaviour<SampleRequest, string>([validator.Object]);

        var act = () => behaviour.Handle(new SampleRequest(""), () => Task.FromResult("ok"), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }
}
