using DPD.Domain.Common;
using DPD.Domain.Enums;

namespace DPD.Domain.Entities;

public sealed class Project : BaseEntity
{
    /// <summary>
    /// Matrice des transitions de statut autorisées (PRD 6.4 - Cycle de vie des Projets).
    /// Un projet est "vivant" : les statuts actifs (Signed/OnGoing/OnHold) peuvent naviguer entre eux
    /// pour refléter la réalité opérationnelle, tandis que Completed/Cancelled/Lost sont des états
    /// terminaux archivables (aucune transition sortante).
    /// </summary>
    private static readonly IReadOnlyDictionary<ProjectStatus, ProjectStatus[]> AllowedTransitions =
        new Dictionary<ProjectStatus, ProjectStatus[]>
        {
            [ProjectStatus.Hypothesis] = [ProjectStatus.Quoted, ProjectStatus.Cancelled],
            [ProjectStatus.Quoted] = [ProjectStatus.Hypothesis, ProjectStatus.Signed, ProjectStatus.Lost],
            [ProjectStatus.Signed] = [ProjectStatus.OnGoing, ProjectStatus.OnHold, ProjectStatus.Cancelled],
            [ProjectStatus.OnGoing] = [ProjectStatus.OnHold, ProjectStatus.Completed, ProjectStatus.Cancelled],
            [ProjectStatus.OnHold] = [ProjectStatus.OnGoing, ProjectStatus.Cancelled],
            [ProjectStatus.Completed] = [],
            [ProjectStatus.Cancelled] = [],
            [ProjectStatus.Lost] = []
        };

    public string Name { get; set; } = string.Empty;
    public string ProjectCode { get; set; } = string.Empty;
    public ProjectStatus Status { get; private set; } = ProjectStatus.Hypothesis;
    public Guid ManagerId { get; set; }
    public decimal EstimatedMd { get; set; }
    public decimal ActualMd { get; set; }
    public ICollection<Study> Studies { get; set; } = new List<Study>();

    /// <summary>
    /// Statuts vers lesquels ce projet peut légalement transiter depuis son état courant.
    /// Utilisé par l'UI pour n'afficher que des actions valides.
    /// </summary>
    public IReadOnlyCollection<ProjectStatus> GetAllowedNextStatuses() => AllowedTransitions[Status];

    public bool IsArchived => Status is ProjectStatus.Completed or ProjectStatus.Cancelled or ProjectStatus.Lost;

    /// <summary>
    /// Le projet consomme officiellement de la capacité (PRD 6.4) uniquement dans ces statuts.
    /// </summary>
    public bool ConsumesCapacity => Status is ProjectStatus.Signed or ProjectStatus.OnGoing;

    public void ChangeStatus(ProjectStatus target)
    {
        if (target == Status)
        {
            return;
        }

        if (!AllowedTransitions.TryGetValue(Status, out var allowed) || !allowed.Contains(target))
        {
            var allowedList = AllowedTransitions.TryGetValue(Status, out var next) && next.Length > 0
                ? string.Join(", ", next)
                : "aucune (statut archivé/terminal)";

            throw new DomainException(
                $"Transition de statut invalide : '{Status}' -> '{target}'. Transitions autorisées depuis '{Status}' : {allowedList}.");
        }

        Status = target;
    }
}


