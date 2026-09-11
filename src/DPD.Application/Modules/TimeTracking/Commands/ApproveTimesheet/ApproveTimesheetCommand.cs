using DPD.Application.Common.Exceptions;
using DPD.Application.Common.Interfaces;
using MediatR;

namespace DPD.Application.Modules.TimeTracking.Commands.ApproveTimesheet;

public sealed record ApproveTimesheetCommand(Guid TimeEntryId, Guid ApproverId) : IRequest, IRequireRoles
{
    public IReadOnlyCollection<string> AllowedRoles => ["Manager", "Admin", "DepartmentResponsible"];
}

public sealed class ApproveTimesheetCommandHandler : IRequestHandler<ApproveTimesheetCommand>
{
    private readonly ITimeEntryRepository _timeEntries;
    private readonly IResourceRepository _resources;
    private readonly IUnitOfWork _unitOfWork;

    public ApproveTimesheetCommandHandler(ITimeEntryRepository timeEntries, IResourceRepository resources, IUnitOfWork unitOfWork)
    {
        _timeEntries = timeEntries;
        _resources = resources;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(ApproveTimesheetCommand request, CancellationToken cancellationToken)
    {
        if (!await _resources.ExistsAsync(request.ApproverId, cancellationToken))
        {
            throw new BusinessException("Approver does not exist.");
        }

        var entry = await _timeEntries.FindAsync(request.TimeEntryId, cancellationToken);
        if (entry is null)
        {
            throw new NotFoundException("Time entry not found.");
        }

        entry.Approve();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
