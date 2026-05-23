using PublicationQualitySystem.DTOs.Auth;
using PublicationQualitySystem.DTOs.File;
using PublicationQualitySystem.DTOs.ResearchGroup;
using PublicationQualitySystem.DTOs.ResearchProfile;
using PublicationQualitySystem.DTOs.Role;
using PublicationQualitySystem.DTOs.User;
using PublicationQualitySystem.Entities;

namespace PublicationQualitySystem.Mappings;

public static class ResearchProfileMapper
{
    public static ResearchProfileDto ToDto(ResearchProfile profile) => new()
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

    public static void UpdateEntity(ResearchProfile profile, ResearchProfileDto dto)
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
