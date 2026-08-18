using DPD.Application.Common.Exceptions;
using DPD.Application.Common.Interfaces;
using DPD.Domain.Entities;
using DPD.Domain.Enums;
using MediatR;

namespace DPD.Application.Modules.Equipment.Commands.ReserveEquipment;

public sealed record ReserveEquipmentCommand(Guid EquipmentId, Guid ResourceId, DateTimeOffset StartTime, DateTimeOffset EndTime, string Purpose) : IRequest<Guid>, IRequireRoles
{
    public IReadOnlyCollection<string> AllowedRoles => ["Scientist", "Manager", "Admin", "PrincipalInvestigator", "StandardUser"];
}

public sealed class ReserveEquipmentCommandHandler : IRequestHandler<ReserveEquipmentCommand, Guid>
{
    private readonly IEquipmentRepository _equipments;
    private readonly IResourceRepository _resources;
    private readonly IUnitOfWork _unitOfWork;

    public ReserveEquipmentCommandHandler(IEquipmentRepository equipments, IResourceRepository resources, IUnitOfWork unitOfWork)
    {
        _equipments = equipments;
        _resources = resources;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(ReserveEquipmentCommand request, CancellationToken cancellationToken)
    {
        var equipment = await _equipments.FindAsync(request.EquipmentId, cancellationToken);
        if (equipment is null)
        {
            throw new NotFoundException("Equipment not found.");
        }

        if (!await _resources.ExistsAsync(request.ResourceId, cancellationToken))
        {
            throw new BusinessException("Resource not found.");
        }

        if (equipment.Status == EquipmentStatus.Maintenance)
        {
            throw new BusinessException("Equipment in maintenance cannot be reserved.");
        }

        var hasConflict = await _equipments.HasReservationConflictAsync(request.EquipmentId, request.StartTime, request.EndTime, cancellationToken);
        if (hasConflict)
        {
            throw new BusinessException("Time slot conflict for equipment reservation.");
        }

        var reservation = new EquipmentReservation
        {
            EquipmentId = request.EquipmentId,
            ResourceId = request.ResourceId,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Purpose = request.Purpose
        };

        await _equipments.AddReservationAsync(reservation, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return reservation.Id;
    }
}
