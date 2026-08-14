using FluentValidation;

namespace DPD.Application.Modules.Equipment.Commands.ReserveEquipment;

public sealed class ReserveEquipmentCommandValidator : AbstractValidator<ReserveEquipmentCommand>
{
    public ReserveEquipmentCommandValidator()
    {
        RuleFor(x => x.EquipmentId).NotEmpty();
        RuleFor(x => x.ResourceId).NotEmpty();
        RuleFor(x => x.Purpose).NotEmpty().MaximumLength(250);
        RuleFor(x => x.EndTime).GreaterThan(x => x.StartTime);
    }
}
