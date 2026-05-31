using PublicationQualitySystem.Application.DTOs.Paper;

namespace PublicationQualitySystem.Application.Services.Interfaces;

public interface IPaperService
{
    Task<PaperVersionResponse> UploadPaperAsync(
        Stream pdfStream,
        string fileName,
        string contentType,
        string? title,
        CancellationToken cancellationToken);
}
