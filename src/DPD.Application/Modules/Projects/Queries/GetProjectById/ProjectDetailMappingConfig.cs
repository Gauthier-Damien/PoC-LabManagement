using DPD.Domain.Entities;
using Mapster;

namespace DPD.Application.Modules.Projects.Queries.GetProjectById;

/// <summary>
/// Configuration Mapster compilée au démarrage pour la vue détaillée d'un projet (PRD 6.4/6.6/6.7/6.8).
/// Les propriétés calculées (Remaining/Variance/EAC) et les statuts autorisés sont dérivés
/// directement dans la configuration pour éviter toute réflexion à l'exécution.
/// </summary>
public sealed class ProjectDetailMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Project, ProjectDetailDto>()
            .Map(dest => dest.Code, src => src.ProjectCode)
            .Map(dest => dest.AllowedNextStatuses, src => src.GetAllowedNextStatuses())
            .Map(dest => dest.RemainingMd, src => src.EstimatedMd - src.ActualMd)
            .Map(dest => dest.VarianceMd, src => src.ActualMd - src.EstimatedMd)
            .Map(dest => dest.EstimateAtCompletion, src => src.ActualMd + Math.Max(0, src.EstimatedMd - src.ActualMd))
            .Map(dest => dest.Studies, src => src.Studies.OrderBy(s => s.Code).ToList().Adapt<List<ProjectStudySummaryDto>>())
            .Map(dest => dest.DepartmentCharges, src => src.Studies
                .GroupBy(s => s.Department)
                .Select(g => new ProjectDepartmentChargeDto(g.Key, g.Sum(s => s.ActualMd)))
                .OrderBy(d => d.Department)
                .ToList());

        config.NewConfig<Study, ProjectStudySummaryDto>()
            .Map(dest => dest.VarianceMd, src => src.ActualMd - src.EstimatedMd);
    }
}
