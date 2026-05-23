using PublicationQualitySystem.Common;
using PublicationQualitySystem.Enums;

namespace PublicationQualitySystem.Entities;

public class ResearchProfile : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public User User { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public string Institution { get; set; } = string.Empty;
    public string? Affiliation { get; set; }
    public string? Department { get; set; }
    public string? Specialization { get; set; }
    public string? Orcid { get; set; }
    public string? GoogleScholarUrl { get; set; }
    public string? ResearchGateUrl { get; set; }
    public string? ScopusId { get; set; }
    public string? ResearchInterests { get; set; }
    public string? Biography { get; set; }
    public AcademicRank AcademicRank { get; set; } = AcademicRank.RESEARCHER;
    public MemberStatus Status { get; set; } = MemberStatus.ACTIVE;
    public int TotalPublications { get; set; }
    public int TotalReviews { get; set; }
    public int AcceptedPapers { get; set; }
    public int CitationCount { get; set; }
    public int HIndex { get; set; }
    public double ContributionScore { get; set; }
}
