using DPD.Domain.Entities;
using Mapster;

namespace DPD.Application.Modules.Studies.Queries.GetStudies;

/// <summary>
/// Configuration Mapster compilée au démarrage pour la projection Study -&gt; StudyListItemDto,
/// incluant les indicateurs de charge du PRD 6.7 (Remaining/Variance/EAC).
/// </summary>
public sealed class StudyMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Study, StudyListItemDto>()
            .Map(dest => dest.RemainingMd, src => src.EstimatedMd - src.ActualMd)
            .Map(dest => dest.VarianceMd, src => src.ActualMd - src.EstimatedMd)
            .Map(dest => dest.EstimateAtCompletion, src => src.ActualMd + (src.EstimatedMd - src.ActualMd));
    }
}
