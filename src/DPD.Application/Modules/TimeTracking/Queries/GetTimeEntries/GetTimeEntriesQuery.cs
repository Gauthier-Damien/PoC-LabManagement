using DPD.Application.Common.Interfaces;
using DPD.Application.Common.Models;
using DPD.Domain.Enums;
using MapsterMapper;
using MediatR;

namespace DPD.Application.Modules.TimeTracking.Queries.GetTimeEntries;

public sealed record GetTimeEntriesQuery(int Page = 1, int PageSize = 25) : IRequest<PaginatedResult<TimeEntryListItemDto>>;

public sealed record TimeEntryListItemDto(Guid Id, Guid ResourceId, Guid StudyId, DateOnly WorkDate, decimal Hours, TimeEntryStatus Status, string? RejectionComment);

public sealed class GetTimeEntriesQueryHandler : IRequestHandler<GetTimeEntriesQuery, PaginatedResult<TimeEntryListItemDto>>
{
    private readonly ITimeEntryRepository _timeEntries;
    private readonly IMapper _mapper;

    public GetTimeEntriesQueryHandler(ITimeEntryRepository timeEntries, IMapper mapper)
    {
        _timeEntries = timeEntries;
        _mapper = mapper;
    }

    public async Task<PaginatedResult<TimeEntryListItemDto>> Handle(GetTimeEntriesQuery request, CancellationToken cancellationToken)
    {
        var entries = await _timeEntries.ListAsync(request.Page, request.PageSize, cancellationToken);
        var total = await _timeEntries.CountAsync(cancellationToken);
        var items = _mapper.Map<List<TimeEntryListItemDto>>(entries);
        return new PaginatedResult<TimeEntryListItemDto>(items, request.Page, request.PageSize, total);
    }
}
