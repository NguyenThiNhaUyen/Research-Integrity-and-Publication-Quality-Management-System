using Microsoft.AspNetCore.Http;

namespace PublicationQualitySystem.DTOs;

public class UploadFileRequest
{
    public IFormFile File { get; set; } = null!;
}
