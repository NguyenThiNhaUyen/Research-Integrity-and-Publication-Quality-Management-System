using PublicationQualitySystem.Application.DTOs.File;
using PublicationQualitySystem.Application.DTOs.Paper;
using PublicationQualitySystem.Domain.Entities;

namespace PublicationQualitySystem.Application.Services.Interfaces;

public interface IPaperUploadProcessor
{
    Task<PaperUploadProcessingResult> CreatePaperFromUploadedFileAsync(
        UploadedFile uploadedFile,
        FileUploadRequest file,
        CancellationToken cancellationToken = default);
}
