using DPD.Application.Common.Interfaces;
using DPD.Domain.Enums;
using MediatR;

namespace DPD.Application.Modules.TimeTracking.Queries.GetTimeEntries;

public sealed record GetTimeEntriesQuery(int Page = 1, int PageSize = 25) : IRequest<IReadOnlyCollection<TimeEntryListItemDto>>;

public sealed record TimeEntryListItemDto(Guid Id, Guid ResourceId, Guid StudyId, DateOnly WorkDate, decimal Hours, TimeEntryStatus Status);

public sealed class GetTimeEntriesQueryHandler : IRequestHandler<GetTimeEntriesQuery, IReadOnlyCollection<TimeEntryListItemDto>>
{
    private readonly ITimeEntryRepository _timeEntries;

    public GetTimeEntriesQueryHandler(ITimeEntryRepository timeEntries)
    {
        _timeEntries = timeEntries;
    }

    public async Task<IReadOnlyCollection<TimeEntryListItemDto>> Handle(GetTimeEntriesQuery request, CancellationToken cancellationToken)
    {
        var entries = await _timeEntries.ListAsync(request.Page, request.PageSize, cancellationToken);
        return entries.Select(t => new TimeEntryListItemDto(t.Id, t.ResourceId, t.StudyId, t.WorkDate, t.Hours, t.Status)).ToList();
    }
}
