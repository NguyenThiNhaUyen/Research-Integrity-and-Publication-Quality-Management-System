using PublicationQualitySystem.Application.DTOs.Auth;
using PublicationQualitySystem.Application.DTOs.File;
using PublicationQualitySystem.Application.DTOs.ResearchGroup;
using PublicationQualitySystem.Application.DTOs.ResearchProfile;
using PublicationQualitySystem.Application.DTOs.Role;
using PublicationQualitySystem.Application.DTOs.User;
using PublicationQualitySystem.Domain.Entities;

namespace PublicationQualitySystem.Application.Mappings;

public static class ResearchProfileMapper
{
    public static ResearchProfileResponse ToResponse(ResearchProfile profile) => new()
    {
        Id = profile.Id,
        UserId = profile.UserId,
        AvatarUrl = profile.AvatarUrl,
        Institution = profile.Institution,
        Specialization = profile.Specialization,
        Orcid = profile.Orcid,
        ResearchInterests = profile.ResearchInterests,
        AcademicRank = profile.AcademicRank,
        Status = profile.Status
    };

    public static void UpdateEntity(ResearchProfile profile, UpdateResearchProfileRequest dto)
    {
        profile.AvatarUrl = dto.AvatarUrl;
        profile.Institution = dto.Institution ?? profile.Institution;
        profile.Specialization = dto.Specialization;
        profile.Orcid = dto.Orcid;
        profile.ResearchInterests = dto.ResearchInterests;
        if (dto.AcademicRank.HasValue) profile.AcademicRank = dto.AcademicRank.Value;
        if (dto.Status.HasValue) profile.Status = dto.Status.Value;
    }
}
