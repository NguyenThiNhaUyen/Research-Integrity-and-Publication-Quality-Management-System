using PublicationQualitySystem.Application.DTOs.Processing;
using PublicationQualitySystem.Application.Repositories.Interfaces;
using PublicationQualitySystem.Application.Services.Interfaces;
using PublicationQualitySystem.Domain.Entities;
using PublicationQualitySystem.Domain.Enums;
using PublicationQualitySystem.Infrastructure.Security;
using PublicationQualitySystem.Shared.Exceptions;
using PublicationQualitySystem.Shared.Extensions;

namespace PublicationQualitySystem.Infrastructure.Services.Implementations;

public class ProcessingTrackerService(
    IProcessingTrackerRepository trackers,
    IPaperVersionRepository versions,
    IPaperProcessingTrackerService trackerService,
    ICurrentUserProvider currentUser) : IProcessingTrackerService
{
    public async Task<PaperProcessingTrackerResponse> GetByPaperVersionIdAsync(long paperVersionId, CancellationToken cancellationToken)
    {
        var version = await versions.FindByIdAsync(paperVersionId, cancellationToken);
        if (version is null)
        {
            throw new AppException(AuditLogErrorCode.NotFound);
        }

        EnsureCanViewPaper(version.Paper);

        if (await trackers.FindByPaperVersionIdAsync(paperVersionId, cancellationToken) is null)
        {
            throw new AppException(AuditLogErrorCode.NotFound);
        }

        return await trackerService.GetByPaperVersionIdAsync(paperVersionId, cancellationToken);
    }

    private bool CanReadAllPapers() =>
        currentUser.User.HasPermission(nameof(PermissionName.PAPER_READ_ALL))
        || currentUser.User.HasPermission(nameof(PermissionName.AUDIT_LOG_READ));

    private void EnsureCanViewPaper(Paper paper)
    {
        if (CanReadAllPapers())
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(currentUser.Subject)
            && !string.IsNullOrWhiteSpace(paper.CreatedBy)
            && string.Equals(paper.CreatedBy, currentUser.Subject, StringComparison.Ordinal))
        {
            return;
        }

        throw new AppException(AuditLogErrorCode.Forbidden);
    }
}
