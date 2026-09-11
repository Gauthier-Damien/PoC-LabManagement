using DPD.Application.Common.Interfaces;
using DPD.Domain.Entities;
using MediatR;

namespace DPD.Application.Modules.TimeTracking.Commands.SubmitTimesheet;

public sealed record SubmitTimesheetCommand(Guid ResourceId, Guid StudyId, DateOnly WorkDate, decimal Hours) : IRequest<Guid>, IRequireRoles
{
    public IReadOnlyCollection<string> AllowedRoles => ["Scientist", "Manager", "Admin", "StandardUser", "PrincipalInvestigator", "SD"];
}

public sealed class SubmitTimesheetCommandHandler : IRequestHandler<SubmitTimesheetCommand, Guid>
{
    private readonly IResourceRepository _resources;
    private readonly IStudyRepository _studies;
    private readonly ITimeEntryRepository _timeEntries;
    private readonly IUnitOfWork _unitOfWork;

    public SubmitTimesheetCommandHandler(IResourceRepository resources, IStudyRepository studies, ITimeEntryRepository timeEntries, IUnitOfWork unitOfWork)
    {
        _resources = resources;
        _studies = studies;
        _timeEntries = timeEntries;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(SubmitTimesheetCommand request, CancellationToken cancellationToken)
    {
        if (!await _resources.ExistsAsync(request.ResourceId, cancellationToken))
        {
            throw new FluentValidation.ValidationException("Resource not found.");
        }

        if (!await _studies.ExistsAsync(request.StudyId, cancellationToken))
        {
            throw new FluentValidation.ValidationException("Study not found.");
        }

        var entry = new TimeEntry
        {
            ResourceId = request.ResourceId,
            StudyId = request.StudyId,
            WorkDate = request.WorkDate,
            Hours = request.Hours
        };

        entry.Submit();
        await _timeEntries.AddAsync(entry, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return entry.Id;
    }
}
