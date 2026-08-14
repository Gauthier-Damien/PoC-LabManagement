using FluentValidation;

namespace DPD.Application.Modules.Projects.Commands.CreateProject;

public sealed class CreateProjectCommandValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.ManagerId).NotEmpty();
        RuleFor(x => x.EstimatedMd).GreaterThan(0);
    }
}
