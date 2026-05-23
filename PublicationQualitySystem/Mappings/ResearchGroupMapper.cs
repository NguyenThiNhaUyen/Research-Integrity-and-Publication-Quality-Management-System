using PublicationQualitySystem.DTOs.Auth;
using PublicationQualitySystem.DTOs.File;
using PublicationQualitySystem.DTOs.ResearchGroup;
using PublicationQualitySystem.DTOs.ResearchProfile;
using PublicationQualitySystem.DTOs.Role;
using PublicationQualitySystem.DTOs.User;
using PublicationQualitySystem.Entities;
using PublicationQualitySystem.Enums;

namespace PublicationQualitySystem.Mappings;

public static class ResearchGroupMapper
{
    public static ResearchGroupDto ToDto(ResearchGroup group, List<ResearchGroupMemberDto>? members = null) => new()
    {
        Id = group.Id,
        Name = group.Name,
        Description = group.Description,
        ResearchTopics = group.ResearchTopics,
        ActiveProjects = group.ActiveProjects,
        Specialization = group.Specialization,
        Institution = group.Institution,
        TotalPublications = group.TotalPublications,
        AcceptedPublications = group.AcceptedPublications,
        AcceptanceRate = group.AcceptanceRate,
        MemberCount = members?.Count,
        Leader = members?.FirstOrDefault(m => m.Role == MemberRoleInGroup.LEADER),
        Members = members
    };

    public static ResearchGroupMemberDto ToMemberDto(ResearchGroupMember member) => new()
    {
        Id = member.Id,
        UserId = member.UserId,
        FullName = member.User.FullName,
        Email = member.User.Email,
        Role = member.Role,
        Status = member.Status,
        JoinedAt = member.JoinedAt,
        LeftAt = member.LeftAt,
        ContributionScore = member.ContributionScore,
        AssignedReviews = member.AssignedReviews,
        CompletedReviews = member.CompletedReviews,
        Responsibilities = member.Responsibilities
    };

    public static void UpdateEntity(ResearchGroup group, ResearchGroupDto dto)
    {
        group.Name = dto.Name;
        group.Description = dto.Description;
        group.ResearchTopics = dto.ResearchTopics;
        group.ActiveProjects = dto.ActiveProjects;
        group.Specialization = dto.Specialization;
        group.Institution = dto.Institution;
        group.TotalPublications = dto.TotalPublications ?? group.TotalPublications;
        group.AcceptedPublications = dto.AcceptedPublications ?? group.AcceptedPublications;
        group.AcceptanceRate = dto.AcceptanceRate ?? group.AcceptanceRate;
    }
}
