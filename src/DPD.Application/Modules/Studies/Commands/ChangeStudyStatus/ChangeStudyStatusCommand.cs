using DPD.Application.Common.Exceptions;
using DPD.Application.Common.Interfaces;
using DPD.Domain.Enums;
using MediatR;

namespace DPD.Application.Modules.Studies.Commands.ChangeStudyStatus;

public sealed record ChangeStudyStatusCommand(Guid StudyId, StudyStatus Status) : IRequest, IRequireRoles
{
    public IReadOnlyCollection<string> AllowedRoles => ["SD", "PrincipalInvestigator", "Admin"];
}

public sealed class ChangeStudyStatusCommandHandler : IRequestHandler<ChangeStudyStatusCommand>
{
    private readonly IStudyRepository _studies;
    private readonly IUnitOfWork _unitOfWork;

    public ChangeStudyStatusCommandHandler(IStudyRepository studies, IUnitOfWork unitOfWork)
    {
        _studies = studies;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(ChangeStudyStatusCommand request, CancellationToken cancellationToken)
    {
        var study = await _studies.FindAsync(request.StudyId, cancellationToken);
        if (study is null)
        {
            throw new NotFoundException("Study not found.");
        }

        study.ChangeStatus(request.Status);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
