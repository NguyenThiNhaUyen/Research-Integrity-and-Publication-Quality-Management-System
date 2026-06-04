namespace PublicationQualitySystem.Domain.Enums;

public enum ProcessingStage
{
    UPLOADED = 0,
    OCR_REQUESTED = 1,
    OCR_COMPLETED = 2,
    METADATA_REQUESTED = 3,
    METADATA_COMPLETED = 4,
    OPENALEX_REQUESTED = 5,
    OPENALEX_COMPLETED = 6,
    QUALITY_SCORING_REQUESTED = 7,
    QUALITY_SCORING_COMPLETED = 8,
    COMPLETED = 9,
    FAILED = 10
}
