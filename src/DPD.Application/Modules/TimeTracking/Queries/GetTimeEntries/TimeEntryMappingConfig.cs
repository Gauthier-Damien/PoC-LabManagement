using DPD.Domain.Entities;
using Mapster;

namespace DPD.Application.Modules.TimeTracking.Queries.GetTimeEntries;

/// <summary>
/// Mapping 1:1 TimeEntry -&gt; TimeEntryListItemDto compilé au démarrage.
/// </summary>
public sealed class TimeEntryMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<TimeEntry, TimeEntryListItemDto>();
    }
}
