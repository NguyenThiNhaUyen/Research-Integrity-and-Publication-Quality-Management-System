using PublicationQualitySystem.Application.DTOs.Paper;

namespace PublicationQualitySystem.Application.Services.Interfaces;

public interface IPaperMetadataQueue
{
    ValueTask QueueAsync(PaperMetadataExtractionJob job, CancellationToken cancellationToken);

    ValueTask<PaperMetadataExtractionJob> DequeueAsync(CancellationToken cancellationToken);
}
