using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace PublicationQualitySystem.Application.DTOs.Paper;

public class UploadPaperRequest
{
    [Required]
    public IFormFile File { get; set; } = default!;

    [MaxLength(500)]
    public string? Title { get; set; }
}
