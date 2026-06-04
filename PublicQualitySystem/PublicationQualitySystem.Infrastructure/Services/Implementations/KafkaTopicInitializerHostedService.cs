using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using PublicationQualitySystem.Infrastructure.Options;

namespace PublicationQualitySystem.Infrastructure.Services.Implementations;

public sealed class KafkaTopicInitializerHostedService(
    IOptions<KafkaOptions> options,
    ILogger<KafkaTopicInitializerHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var topics = new[]
        {
            options.Value.PaperUploadedTopic,
            options.Value.MetadataQualityScoredTopic,
            options.Value.OpenAlexSimilarityRequestedTopic,
            options.Value.OpenAlexSimilaritySkippedTopic
        }.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        using var admin = new AdminClientBuilder(new AdminClientConfig
        {
            BootstrapServers = options.Value.BootstrapServers
        }).Build();

        try
        {
            await admin.CreateTopicsAsync(
                topics.Select(topic => new TopicSpecification
                {
                    Name = topic,
                    NumPartitions = 1,
                    ReplicationFactor = 1
                }));

            logger.LogInformation("Kafka topics created. Topics={Topics}", string.Join(", ", topics));
        }
        catch (CreateTopicsException ex) when (ex.Results.Any(x => x.Error.Code == ErrorCode.TopicAlreadyExists))
        {
            logger.LogInformation("One or more Kafka topics already exist. Topics={Topics}", string.Join(", ", topics));
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Kafka topic initialization failed. Topics={Topics}. Outbox publisher and consumers will retry when Kafka is ready.",
                string.Join(", ", topics));
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
