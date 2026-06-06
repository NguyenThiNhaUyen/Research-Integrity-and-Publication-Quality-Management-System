using PublicationQualitySystem.Application.DTOs.Metadata;
using PublicationQualitySystem.Application.Mappings;
using PublicationQualitySystem.Application.Repositories.Interfaces;
using PublicationQualitySystem.Application.Services.Interfaces;
using PublicationQualitySystem.Domain.Entities;
using PublicationQualitySystem.Domain.Enums;
using PublicationQualitySystem.Infrastructure.Security;
using PublicationQualitySystem.Shared.Exceptions;
using PublicationQualitySystem.Shared.Extensions;

namespace PublicationQualitySystem.Infrastructure.Services.Implementations;

public class MetadataQualityService(
    IMetadataQualityRepository metadataQuality,
    IPaperVersionRepository versions,
    ICurrentUserProvider currentUser) : IMetadataQualityService
{
    public async Task<MetadataQualityResponse> GetByPaperVersionIdAsync(long paperVersionId, CancellationToken cancellationToken)
    {
        var version = await versions.FindByIdAsync(paperVersionId, cancellationToken);
        if (version is null)
        {
            throw new AppException(AuditLogErrorCode.NotFound);
        }

        EnsureCanViewPaper(version.Paper);

        var metadata = await metadataQuality.FindByPaperVersionIdAsync(paperVersionId, cancellationToken);
        if (metadata is null || metadata.MetadataQualityTotalScore is null)
        {
            throw new AppException(PaperErrorCode.MetadataNotFound);
        }

        return MetadataQualityMapper.ToResponse(metadata, paperVersionId);
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
