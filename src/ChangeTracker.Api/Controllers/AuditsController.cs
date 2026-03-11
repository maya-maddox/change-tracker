using ChangeTracker.Application.DTOs;
using ChangeTracker.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ChangeTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuditsController(IAuditRepository auditRepository) : ControllerBase
{
    private readonly IAuditRepository _auditRepository = auditRepository;

    [HttpGet]
    public async Task<ActionResult<PagedResult<AuditRecordResponse>>> Query(
        [FromQuery] AuditQueryParameters parameters, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _auditRepository.QueryAsync(parameters, cancellationToken);

        var responses = items.Select(r =>
            new AuditRecordResponse(r.Id, r.EntityType, r.EntityId, r.Action, r.Changes, r.Timestamp))
            .ToList();

        return Ok(new PagedResult<AuditRecordResponse>(responses, totalCount, parameters.Page, parameters.PageSize));
    }

    [HttpGet("{entityId:guid}/lineage")]
    public async Task<ActionResult<IReadOnlyList<AuditRecordResponse>>> GetLineage(
        Guid entityId, CancellationToken cancellationToken)
    {
        var records = await _auditRepository.GetLineageAsync(entityId, cancellationToken);

        var responses = records.Select(r =>
            new AuditRecordResponse(r.Id, r.EntityType, r.EntityId, r.Action, r.Changes, r.Timestamp))
            .ToList();

        return Ok(responses);
    }
}
