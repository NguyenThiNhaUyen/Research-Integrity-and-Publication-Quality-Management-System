namespace PublicationQualitySystem.Domain.Enums;

public enum DoiValidationStatus
{
    MISSING = 0,
    INVALID_FORMAT = 1,
    VALID = 2,
    NOT_FOUND = 3,
    METADATA_MISMATCH = 4,
    SERVICE_ERROR = 5
}
