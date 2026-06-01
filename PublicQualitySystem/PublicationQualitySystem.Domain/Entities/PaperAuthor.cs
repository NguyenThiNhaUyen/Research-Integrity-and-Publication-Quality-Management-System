using PublicationQualitySystem.Shared.Common;
using PublicationQualitySystem.Domain.Enums;

namespace PublicationQualitySystem.Domain.Entities;

public class PaperAuthor : BaseEntity
{
    public long PaperId { get; set; }
    public Paper Paper { get; set; } = null!;
    public long AuthorId { get; set; }
    public Author Author { get; set; } = null!;
    public AuthorRole Role { get; set; }
}
