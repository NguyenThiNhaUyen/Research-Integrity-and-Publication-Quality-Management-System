using Microsoft.EntityFrameworkCore;
using PublicationQualitySystem.Application.Repositories.Interfaces;
using PublicationQualitySystem.Domain.Entities;
using PublicationQualitySystem.Infrastructure.Configurations;

namespace PublicationQualitySystem.Infrastructure.Repositories.Implementations;

public class AuditLogRepository(ApplicationDbContext db) : IAuditLogRepository
{
    public async Task<IReadOnlyList<AuditLog>> FindByPaperIdAsync(long paperId, CancellationToken cancellationToken = default) =>
        await db.AuditLogs
            .AsNoTracking()
            .Where(x => x.PaperId == paperId && !x.Deleted)
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.Step)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<AuditLog>> FindByPaperVersionIdAsync(long paperVersionId, CancellationToken cancellationToken = default) =>
        await db.AuditLogs
            .AsNoTracking()
            .Where(x => x.PaperVersionId == paperVersionId && !x.Deleted)
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.Step)
            .ToListAsync(cancellationToken);
}
