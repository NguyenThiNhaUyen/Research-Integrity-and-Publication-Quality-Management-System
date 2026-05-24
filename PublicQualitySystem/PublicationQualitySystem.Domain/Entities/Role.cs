using PublicationQualitySystem.Shared.Common;

namespace PublicationQualitySystem.Domain.Entities;

public class Role : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ICollection<Permission> Permissions { get; set; } = new HashSet<Permission>();
    public ICollection<User> Users { get; set; } = new HashSet<User>();
}
