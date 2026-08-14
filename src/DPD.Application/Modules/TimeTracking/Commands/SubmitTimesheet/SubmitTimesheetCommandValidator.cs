using FluentValidation;

namespace DPD.Application.Modules.TimeTracking.Commands.SubmitTimesheet;

public sealed class SubmitTimesheetCommandValidator : AbstractValidator<SubmitTimesheetCommand>
{
    public SubmitTimesheetCommandValidator()
    {
        RuleFor(x => x.ResourceId).NotEmpty();
        RuleFor(x => x.StudyId).NotEmpty();
        RuleFor(x => x.Hours).GreaterThan(0).LessThanOrEqualTo(24);
    }
}
