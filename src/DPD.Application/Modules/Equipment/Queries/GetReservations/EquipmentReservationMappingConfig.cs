using DPD.Domain.Entities;
using Mapster;

namespace DPD.Application.Modules.Equipment.Queries.GetReservations;

/// <summary>
/// Mapping 1:1 EquipmentReservation -&gt; EquipmentReservationDto compilé au démarrage.
/// </summary>
public sealed class EquipmentReservationMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<EquipmentReservation, EquipmentReservationDto>();
    }
}
