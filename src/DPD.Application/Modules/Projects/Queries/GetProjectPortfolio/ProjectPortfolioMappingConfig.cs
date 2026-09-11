using DPD.Domain.Entities;
using Mapster;

namespace DPD.Application.Modules.Projects.Queries.GetProjectPortfolio;

/// <summary>
/// Configuration Mapster compilée au démarrage (scan d'assembly), évitant la réflexion à
/// l'exécution pour la projection Project -&gt; ProjectPortfolioItemDto (PRD 6.7).
/// </summary>
public sealed class ProjectPortfolioMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Project, ProjectPortfolioItemDto>()
            .Map(dest => dest.Code, src => src.ProjectCode)
            .Map(dest => dest.RemainingMd, src => src.EstimatedMd - src.ActualMd)
            .Map(dest => dest.VarianceMd, src => src.ActualMd - src.EstimatedMd);
    }
}
