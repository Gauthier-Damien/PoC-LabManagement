using DPD.Application.Common.Interfaces;
using MapsterMapper;
using MediatR;

namespace DPD.Application.Modules.Equipment.Queries.GetReservations;

public sealed record GetReservationsQuery(int Page = 1, int PageSize = 25) : IRequest<IReadOnlyCollection<EquipmentReservationDto>>;

public sealed record EquipmentReservationDto(Guid Id, Guid EquipmentId, Guid ResourceId, DateTimeOffset StartTime, DateTimeOffset EndTime, string Purpose);

public sealed class GetReservationsQueryHandler : IRequestHandler<GetReservationsQuery, IReadOnlyCollection<EquipmentReservationDto>>
{
    private readonly IEquipmentRepository _equipments;
    private readonly IMapper _mapper;

    public GetReservationsQueryHandler(IEquipmentRepository equipments, IMapper mapper)
    {
        _equipments = equipments;
        _mapper = mapper;
    }

    public async Task<IReadOnlyCollection<EquipmentReservationDto>> Handle(GetReservationsQuery request, CancellationToken cancellationToken)
    {
        var reservations = await _equipments.ListReservationsAsync(request.Page, request.PageSize, cancellationToken);
        return _mapper.Map<List<EquipmentReservationDto>>(reservations);
    }
}
