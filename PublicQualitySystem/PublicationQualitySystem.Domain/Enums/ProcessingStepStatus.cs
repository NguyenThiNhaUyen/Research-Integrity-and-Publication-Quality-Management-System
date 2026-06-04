namespace PublicationQualitySystem.Domain.Enums;

public enum ProcessingStepStatus
{
    NotStarted = 0,
    EventPublished = 1,
    Processing = 2,
    Completed = 3,
    Failed = 4,
    Skipped = 5,
    RetryScheduled = 6
}
