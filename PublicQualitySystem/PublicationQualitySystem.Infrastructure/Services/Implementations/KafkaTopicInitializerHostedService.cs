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
        var topic = options.Value.PaperUploadedTopic;

        using var admin = new AdminClientBuilder(new AdminClientConfig
        {
            BootstrapServers = options.Value.BootstrapServers
        }).Build();

        try
        {
            await admin.CreateTopicsAsync(
            [
                new TopicSpecification
                {
                    Name = topic,
                    NumPartitions = 1,
                    ReplicationFactor = 1
                }
            ]);

            logger.LogInformation("Kafka topic created. Topic={Topic}", topic);
        }
        catch (CreateTopicsException ex) when (ex.Results.Any(x => x.Error.Code == ErrorCode.TopicAlreadyExists))
        {
            logger.LogInformation("Kafka topic already exists. Topic={Topic}", topic);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Kafka topic initialization failed. Topic={Topic}. Outbox publisher and consumers will retry when Kafka is ready.",
                topic);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
