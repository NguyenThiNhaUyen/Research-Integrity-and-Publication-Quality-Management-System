using Microsoft.AspNetCore.Http;

namespace PublicationQualitySystem.DTOs.File;

public class UploadFileRequest
{
    public IFormFile File { get; set; } = null!;
}
