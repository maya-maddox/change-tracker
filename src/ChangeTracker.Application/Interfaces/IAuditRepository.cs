using ChangeTracker.Application.DTOs;
using ChangeTracker.Domain.Entities;

namespace ChangeTracker.Application.Interfaces;

public interface IAuditRepository
{
    Task<(IReadOnlyList<AuditRecord> Items, int TotalCount)> QueryAsync(
        AuditQueryParameters parameters, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuditRecord>> GetLineageAsync(
        Guid entityId, CancellationToken cancellationToken = default);
}
