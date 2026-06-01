using PublicationQualitySystem.Shared.Common;

namespace PublicationQualitySystem.Domain.Entities;

public class User : BaseEntity<string>
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    public ICollection<Role> Roles { get; set; } = new HashSet<Role>();
    public ResearchProfile? Profile { get; set; }
    public ICollection<ResearchGroupMember> GroupMemberships { get; set; } = new HashSet<ResearchGroupMember>();
}
