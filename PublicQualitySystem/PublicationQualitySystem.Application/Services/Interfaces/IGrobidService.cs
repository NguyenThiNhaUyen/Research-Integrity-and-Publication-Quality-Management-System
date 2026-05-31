using PublicationQualitySystem.Application.DTOs.Grobid;

namespace PublicationQualitySystem.Application.Services.Interfaces;

public interface IGrobidService
{
    Task<GrobidMetadataResponse> ExtractMetadataAsync(
        Stream pdfStream,
        string fileName,
        CancellationToken cancellationToken);
}
