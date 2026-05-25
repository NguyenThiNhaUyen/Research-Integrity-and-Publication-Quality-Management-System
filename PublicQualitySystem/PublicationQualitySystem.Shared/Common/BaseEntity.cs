namespace PublicationQualitySystem.Shared.Common;

public interface IAuditableEntity
{
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
    bool Deleted { get; set; }
    string? CreatedBy { get; set; }
    string? UpdatedBy { get; set; }
}

public abstract class BaseEntity<TKey> : IAuditableEntity
{
    public TKey Id { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool Deleted { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}

public abstract class BaseEntity : BaseEntity<long>;
