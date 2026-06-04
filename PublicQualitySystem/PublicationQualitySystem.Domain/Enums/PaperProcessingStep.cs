namespace PublicationQualitySystem.Domain.Enums;

public enum PaperProcessingStep
{
    Upload = 0,
    MarkdownConversion = 1,
    MetadataExtraction = 2,
    CrossrefEnrichment = 3,
    MetadataQualityScoring = 4,
    OpenAlexSimilarityCheck = 5,
    AiPublicationQualityReview = 6,
    IntegrityScreening = 7
}
