using DPD.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DPD.Web.Controllers;

[ApiController]
[Route("api/reference")]
[Authorize]
public sealed class ReferenceDataController : ControllerBase
{
    private readonly AppDbContext _db;

    public ReferenceDataController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("resources")]
    public async Task<IReadOnlyCollection<IdNameDto>> GetResources(CancellationToken cancellationToken)
    {
        return await _db.Resources.OrderBy(x => x.LastName).Select(x => new IdNameDto(x.Id, x.FirstName + " " + x.LastName)).ToListAsync(cancellationToken);
    }

    [HttpGet("studies")]
    public async Task<IReadOnlyCollection<IdNameDto>> GetStudies(CancellationToken cancellationToken)
    {
        return await _db.Studies.OrderBy(x => x.Code).Select(x => new IdNameDto(x.Id, x.Code)).ToListAsync(cancellationToken);
    }

    [HttpGet("projects")]
    public async Task<IReadOnlyCollection<IdNameDto>> GetProjects(CancellationToken cancellationToken)
    {
        return await _db.Projects.OrderBy(x => x.ProjectCode).Select(x => new IdNameDto(x.Id, x.ProjectCode + " - " + x.Name)).ToListAsync(cancellationToken);
    }

    [HttpGet("equipment")]
    public async Task<IReadOnlyCollection<IdNameDto>> GetEquipment(CancellationToken cancellationToken)
    {
        return await _db.Equipments.OrderBy(x => x.Name).Select(x => new IdNameDto(x.Id, x.Name)).ToListAsync(cancellationToken);
    }

    public sealed record IdNameDto(Guid Id, string Name);
}
