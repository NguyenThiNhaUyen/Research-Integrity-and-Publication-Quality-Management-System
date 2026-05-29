using Microsoft.Extensions.Logging;
using PublicationQualitySystem.Application.DTOs.PaperReviewProfile;
using PublicationQualitySystem.Application.Repositories.Interfaces;
using PublicationQualitySystem.Application.Services.Interfaces;
using PublicationQualitySystem.Domain.Entities;
using PublicationQualitySystem.Domain.Enums;
using PublicationQualitySystem.Infrastructure.Security;
using PublicationQualitySystem.Shared.Constants;
using PublicationQualitySystem.Shared.Exceptions;

namespace PublicationQualitySystem.Infrastructure.Services.Implementations;

public class PaperReviewProfileService(
    IPaperRepository papers,
    IPaperReviewProfileRepository profiles,
    ICurrentUserProvider currentUser,
    ILogger<PaperReviewProfileService> logger) : IPaperReviewProfileService
{
    public async Task<PaperReviewProfileResponse> CreateOrUpdateAsync(long paperId, CreatePaperReviewProfileRequest request)
    {
        if (request is null)
        {
            throw new AppException(PaperReviewProfileErrorCode.ValidationError);
        }

        var paper = await GetAuthorizedPaperAsync(paperId);
        var profile = await profiles.FindByPaperIdAsync(paperId);

        if (profile is null)
        {
            profile = new PaperReviewProfile
            {
                PaperId = paper.Id,
                Paper = paper
            };

            ApplyRequest(profile, request);
            await profiles.AddAsync(profile);
            logger.LogInformation("Created paper review profile for paper {PaperId}", paperId);
        }
        else
        {
            ApplyRequest(profile, request);
            logger.LogInformation("Updated paper review profile {ProfileId} for paper {PaperId}", profile.Id, paperId);
        }

        await profiles.SaveChangesAsync();
        return ToResponse(profile);
    }

    public async Task<PaperReviewProfileResponse> GetByPaperIdAsync(long paperId)
    {
        await GetAuthorizedPaperAsync(paperId);

        var profile = await profiles.FindByPaperIdAsync(paperId)
            ?? throw new AppException(PaperReviewProfileErrorCode.ProfileNotFound);

        logger.LogInformation("Retrieved paper review profile {ProfileId} for paper {PaperId}", profile.Id, paperId);
        return ToResponse(profile);
    }

    private async Task<Paper> GetAuthorizedPaperAsync(long paperId)
    {
        var paper = await papers.FindByIdWithAccessDataAsync(paperId)
            ?? throw new AppException(PaperReviewProfileErrorCode.PaperNotFound);

        EnsureCanAccess(paper);
        return paper;
    }

    private void EnsureCanAccess(Paper paper)
    {
        if (IsAdmin())
        {
            return;
        }

        var userId = currentUser.Subject;
        if (!string.IsNullOrWhiteSpace(userId) && paper.OwnerUserId == userId)
        {
            return;
        }

        logger.LogWarning(
            "Forbidden paper review profile access. paperId={PaperId}, ownerUserId={OwnerUserId}, currentUserId={CurrentUserId}",
            paper.Id,
            paper.OwnerUserId,
            userId);

        throw new AppException(PaperReviewProfileErrorCode.Forbidden);
    }

    private bool IsAdmin() =>
        currentUser.User.HasClaim(ClaimConstants.Role, $"{ClaimConstants.RolePrefix}{RoleName.ADMIN}") ||
        currentUser.User.HasClaim(ClaimConstants.Permission, PermissionName.ADMIN_SETTING_MANAGE.ToString());

    private static void ApplyRequest(PaperReviewProfile profile, CreatePaperReviewProfileRequest request)
    {
        profile.TargetType = request.TargetType;
        profile.ResearchField = request.ResearchField;
        profile.PaperType = request.PaperType;
        profile.ReviewGoal = request.ReviewGoal;
        profile.Note = request.Note;
    }

    private static PaperReviewProfileResponse ToResponse(PaperReviewProfile profile) => new()
    {
        Id = profile.Id,
        PaperId = profile.PaperId,
        TargetType = profile.TargetType,
        ResearchField = profile.ResearchField,
        PaperType = profile.PaperType,
        ReviewGoal = profile.ReviewGoal,
        Note = profile.Note,
        CreatedAt = profile.CreatedAt,
        UpdatedAt = profile.UpdatedAt
    };
}
