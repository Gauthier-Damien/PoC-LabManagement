using DPD.Application.Common.Exceptions;
using DPD.Application.Common.Interfaces;
using MediatR;

namespace DPD.Application.Modules.TimeTracking.Commands.LockTimeEntry;

/// <summary>
/// Verrouille une feuille de temps de manière définitive (fin de mois clôturée) ou débloque
/// exceptionnellement une entrée déjà verrouillée. Réservé à l'Administrateur (PRD 6.3 : "Une fois
/// le statut 'Locked' atteint, aucune modification n'est permise sans l'intervention exceptionnelle
/// d'un Administrateur, qui générera une entrée critique dans l'Audit Trail").
/// </summary>
public sealed record LockTimeEntryCommand(Guid TimeEntryId) : IRequest, IRequireRoles
{
    public IReadOnlyCollection<string> AllowedRoles => ["Admin"];
}

public sealed class LockTimeEntryCommandHandler : IRequestHandler<LockTimeEntryCommand>
{
    private readonly ITimeEntryRepository _timeEntries;
    private readonly IUnitOfWork _unitOfWork;

    public LockTimeEntryCommandHandler(ITimeEntryRepository timeEntries, IUnitOfWork unitOfWork)
    {
        _timeEntries = timeEntries;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(LockTimeEntryCommand request, CancellationToken cancellationToken)
    {
        var entry = await _timeEntries.FindAsync(request.TimeEntryId, cancellationToken);
        if (entry is null)
        {
            throw new NotFoundException("Time entry not found.");
        }

        entry.Lock();
        // L'AuditInterceptor capture automatiquement ce changement de statut (Locked) avec
        // l'utilisateur Admin courant : c'est l'entrée "critique" exigée par le PRD.
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
