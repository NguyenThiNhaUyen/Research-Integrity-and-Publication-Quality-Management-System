namespace PublicationQualitySystem.Infrastructure.Options;

public class KafkaOptions
{
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string PaperUploadedTopic { get; set; } = "ripqms.paper-uploaded.v1";
    public string MetadataQualityScoredTopic { get; set; } = "ripqms.metadata-quality-scored.v1";
    public string OpenAlexSimilarityRequestedTopic { get; set; } = "ripqms.openalex-similarity-requested.v1";
    public string OpenAlexSimilaritySkippedTopic { get; set; } = "ripqms.openalex-similarity-skipped.v1";
    public int OutboxPublisherIntervalSeconds { get; set; } = 5;
    public int ConsumerPollTimeoutMs { get; set; } = 1000;
    public string OcrConsumerGroupId { get; set; } = "ripqms-paper-ocr";
    public string MetadataConsumerGroupId { get; set; } = "ripqms-paper-metadata";
    public string OpenAlexGateConsumerGroupId { get; set; } = "ripqms-openalex-gate";
    public string OpenAlexSimilarityConsumerGroupId { get; set; } = "ripqms-openalex-similarity";
}
