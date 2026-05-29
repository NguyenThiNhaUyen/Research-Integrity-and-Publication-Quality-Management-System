namespace PublicationQualitySystem.Application.DTOs.Role.Responses;

public class PermissionResponseDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
