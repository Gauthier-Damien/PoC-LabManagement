using DPD.Application.Common.Exceptions;
using DPD.Application.Common.Interfaces;
using MediatR;

namespace DPD.Application.Modules.TimeTracking.Commands.RejectTimesheet;

/// <summary>
/// Rejette une feuille de temps soumise. Le commentaire est obligatoire (PRD 8.1 : "Une feuille de
/// temps rejetée retourne à son auteur avec commentaire obligatoire du valideur").
/// </summary>
public sealed record RejectTimesheetCommand(Guid TimeEntryId, Guid ApproverId, string Comment) : IRequest, IRequireRoles
{
    public IReadOnlyCollection<string> AllowedRoles => ["Manager", "Admin", "DepartmentResponsible"];
}

public sealed class RejectTimesheetCommandHandler : IRequestHandler<RejectTimesheetCommand>
{
    private readonly ITimeEntryRepository _timeEntries;
    private readonly IResourceRepository _resources;
    private readonly IUnitOfWork _unitOfWork;

    public RejectTimesheetCommandHandler(ITimeEntryRepository timeEntries, IResourceRepository resources, IUnitOfWork unitOfWork)
    {
        _timeEntries = timeEntries;
        _resources = resources;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(RejectTimesheetCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Comment))
        {
            throw new BusinessException("Un commentaire est obligatoire pour rejeter une feuille de temps.");
        }

        if (!await _resources.ExistsAsync(request.ApproverId, cancellationToken))
        {
            throw new BusinessException("Approver does not exist.");
        }

        var entry = await _timeEntries.FindAsync(request.TimeEntryId, cancellationToken);
        if (entry is null)
        {
            throw new NotFoundException("Time entry not found.");
        }

        entry.Reject(request.Comment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
