using System.Threading.Channels;
using PublicationQualitySystem.Application.DTOs.Paper;
using PublicationQualitySystem.Application.Services.Interfaces;

namespace PublicationQualitySystem.Infrastructure.Services.Implementations;

public sealed class PaperOcrQueue : IPaperOcrQueue
{
    private readonly Channel<PaperOcrJob> _queue = Channel.CreateUnbounded<PaperOcrJob>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

    public ValueTask QueueAsync(PaperOcrJob job, CancellationToken cancellationToken) =>
        _queue.Writer.WriteAsync(job, cancellationToken);

    public ValueTask<PaperOcrJob> DequeueAsync(CancellationToken cancellationToken) =>
        _queue.Reader.ReadAsync(cancellationToken);
}
