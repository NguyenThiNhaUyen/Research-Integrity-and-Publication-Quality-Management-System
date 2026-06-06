using PublicationQualitySystem.Application.DTOs.Paper;
using PublicationQualitySystem.Application.Mappings;
using PublicationQualitySystem.Application.Repositories.Interfaces;
using PublicationQualitySystem.Application.Services.Interfaces;
using PublicationQualitySystem.Domain.Entities;
using PublicationQualitySystem.Domain.Enums;
using PublicationQualitySystem.Infrastructure.Security;
using PublicationQualitySystem.Shared.Exceptions;
using PublicationQualitySystem.Shared.Extensions;

namespace PublicationQualitySystem.Infrastructure.Services.Implementations;

public class PaperVersionService(
    IPaperRepository papers,
    IPaperVersionRepository versions,
    ICurrentUserProvider currentUser) : IPaperVersionService
{
    public async Task<IReadOnlyList<PaperVersionResponse>> GetVersionsByPaperIdAsync(long paperId, CancellationToken cancellationToken)
    {
        var paper = await papers.FindByIdAsync(paperId, cancellationToken);
        if (paper is null)
        {
            throw new AppException(PaperErrorCode.NotFound);
        }

        EnsureCanViewPaper(paper);
        var result = await versions.FindByPaperIdAsync(paperId, cancellationToken);
        return result.Select(PaperVersionMapper.ToResponse).ToArray();
    }

    public async Task<PaperVersionResponse> GetVersionAsync(long paperVersionId, CancellationToken cancellationToken)
    {
        var version = await versions.FindByIdAsync(paperVersionId, cancellationToken);
        if (version is null)
        {
            throw new AppException(AuditLogErrorCode.NotFound);
        }

        EnsureCanViewPaper(version.Paper);
        return PaperVersionMapper.ToResponse(version);
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
