using PublicationQualitySystem.Application.DTOs.Paper;

namespace PublicationQualitySystem.Application.Services.Interfaces;

public interface IPaperService
{
    Task<IReadOnlyList<PaperResponse>> GetPapersAsync(
        int page,
        int size,
        string? search,
        bool sortDescending,
        CancellationToken cancellationToken);

    Task<PaperResponse> GetPaperAsync(
        long paperId,
        CancellationToken cancellationToken);

    Task<PaperVersionResponse> UploadPaperAsync(
        Stream pdfStream,
        string fileName,
        string contentType,
        string? title,
        CancellationToken cancellationToken);

    Task<PaperMetadataResponse> GetMetadataAsync(
        long paperId,
        CancellationToken cancellationToken);
}
