namespace PublicationQualitySystem.Infrastructure.Options;

public class KafkaOptions
{
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string PaperUploadedTopic { get; set; } = "ripqms.paper-uploaded.v1";
    public int OutboxPublisherIntervalSeconds { get; set; } = 5;
    public int ConsumerPollTimeoutMs { get; set; } = 1000;
    public string OcrConsumerGroupId { get; set; } = "ripqms-paper-ocr";
    public string MetadataConsumerGroupId { get; set; } = "ripqms-paper-metadata";
}
