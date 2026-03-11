using ChangeTracker.Application.DTOs;
using ChangeTracker.Application.Interfaces;
using ChangeTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChangeTracker.Infrastructure.Data.Repositories;

public class AuditRepository(ChangeTrackerDbContext context) : IAuditRepository
{
    private readonly ChangeTrackerDbContext _context = context;

    public async Task<(IReadOnlyList<AuditRecord> Items, int TotalCount)> QueryAsync(
        AuditQueryParameters parameters, CancellationToken cancellationToken = default)
    {
        var query = _context.AuditRecords.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(parameters.EntityType))
            query = query.Where(a => a.EntityType == parameters.EntityType);

        if (parameters.EntityId.HasValue)
            query = query.Where(a => a.EntityId == parameters.EntityId.Value);

        if (!string.IsNullOrWhiteSpace(parameters.Action))
            query = query.Where(a => a.Action == parameters.Action);

        if (parameters.From.HasValue)
            query = query.Where(a => a.Timestamp >= parameters.From.Value);

        if (parameters.To.HasValue)
            query = query.Where(a => a.Timestamp <= parameters.To.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<AuditRecord>> GetLineageAsync(
        Guid entityId, CancellationToken cancellationToken = default)
    {
        return await _context.AuditRecords
            .AsNoTracking()
            .Where(a => a.EntityId == entityId)
            .OrderBy(a => a.Timestamp)
            .ToListAsync(cancellationToken);
    }
}
