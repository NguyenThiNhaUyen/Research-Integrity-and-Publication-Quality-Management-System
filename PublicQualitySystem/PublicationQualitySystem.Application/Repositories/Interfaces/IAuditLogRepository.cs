using PublicationQualitySystem.Domain.Entities;

namespace PublicationQualitySystem.Application.Repositories.Interfaces;

public interface IAuditLogRepository
{
    Task<IReadOnlyList<AuditLog>> FindByPaperIdAsync(long paperId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuditLog>> FindByPaperVersionIdAsync(long paperVersionId, CancellationToken cancellationToken = default);
}
