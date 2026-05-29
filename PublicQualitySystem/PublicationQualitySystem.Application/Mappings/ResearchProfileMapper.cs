using PublicationQualitySystem.Application.DTOs.ResearchProfile.Requests;
using PublicationQualitySystem.Application.DTOs.ResearchProfile.Responses;
using PublicationQualitySystem.Domain.Entities;

namespace PublicationQualitySystem.Application.Mappings;

public static class ResearchProfileMapper
{
    public static ResearchProfileResponseDto ToDto(ResearchProfile profile) => new()
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

    public static void UpdateEntity(ResearchProfile profile, UpdateResearchProfileRequestDto dto)
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
