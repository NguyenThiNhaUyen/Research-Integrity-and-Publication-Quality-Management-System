using System.Text.Json;
using PublicationQualitySystem.Application.DTOs.Metadata;
using PublicationQualitySystem.Domain.Entities;

namespace PublicationQualitySystem.Application.Mappings;

public static class MetadataQualityMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static MetadataQualityResponse ToResponse(PaperMetadata metadata, long paperVersionId)
    {
        var missingFields = Deserialize<IReadOnlyList<string>>(metadata.MetadataQualityMissingFieldsJson) ?? Array.Empty<string>();
        var warnings = Deserialize<IReadOnlyList<string>>(metadata.MetadataQualityWarningsJson) ?? Array.Empty<string>();
        var fieldScores = Deserialize<Dictionary<string, int>>(metadata.MetadataQualityFieldScoresJson) ?? new Dictionary<string, int>();
        var passed = metadata.MetadataQualityCanProceed == true;

        return new MetadataQualityResponse
        {
            PaperId = metadata.PaperId,
            PaperVersionId = paperVersionId,
            PaperMetadataId = metadata.Id,
            MetadataScore = metadata.MetadataQualityTotalScore ?? 0,
            CompletenessScore = metadata.MetadataQualityCoreScore ?? 0,
            ConsistencyScore = metadata.MetadataQualityExtendedScore ?? 0,
            AuthorityScore = metadata.MetadataQualityEnrichmentScore ?? 0,
            Grade = metadata.MetadataQualityGrade ?? string.Empty,
            Passed = passed,
            Explanation = BuildExplanation(passed, missingFields, warnings),
            MissingFields = missingFields,
            Warnings = warnings,
            FieldScores = fieldScores,
            ScoredAt = metadata.MetadataQualityScoredAt
        };
    }

    private static string BuildExplanation(bool passed, IReadOnlyList<string> missingFields, IReadOnlyList<string> warnings)
    {
        if (passed && missingFields.Count == 0 && warnings.Count == 0)
        {
            return "Metadata quality gate passed with no missing fields or warnings.";
        }

        if (passed)
        {
            return "Metadata quality gate passed with warnings or optional missing fields.";
        }

        return "Metadata quality gate failed and requires human review before downstream screening.";
    }

    private static T? Deserialize<T>(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return default;
        }
    }
}
