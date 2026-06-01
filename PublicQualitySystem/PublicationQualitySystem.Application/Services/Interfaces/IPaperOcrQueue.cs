using PublicationQualitySystem.Application.DTOs.Paper;

namespace PublicationQualitySystem.Application.Services.Interfaces;

public interface IPaperOcrQueue
{
    ValueTask QueueAsync(PaperOcrJob job, CancellationToken cancellationToken);

    ValueTask<PaperOcrJob> DequeueAsync(CancellationToken cancellationToken);
}
