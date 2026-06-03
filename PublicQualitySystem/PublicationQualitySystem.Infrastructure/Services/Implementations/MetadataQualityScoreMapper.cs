using System.Text.Json;
using PublicationQualitySystem.Application.DTOs.Metadata;
using PublicationQualitySystem.Domain.Entities;

namespace PublicationQualitySystem.Infrastructure.Services.Implementations;

public static class MetadataQualityScoreMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static void Apply(PaperMetadata metadata, MetadataQualityScoreResponse score, DateTime scoredAt)
    {
        metadata.MetadataQualityTotalScore = score.TotalScore;
        metadata.MetadataQualityCoreScore = score.CoreScore;
        metadata.MetadataQualityExtendedScore = score.ExtendedScore;
        metadata.MetadataQualityEnrichmentScore = score.EnrichmentScore;
        metadata.MetadataQualityGrade = score.Grade;
        metadata.MetadataQualityCanProceed = score.CanProceed;
        metadata.MetadataQualityMissingFieldsJson = JsonSerializer.Serialize(score.MissingFields, JsonOptions);
        metadata.MetadataQualityWarningsJson = JsonSerializer.Serialize(score.Warnings, JsonOptions);
        metadata.MetadataQualityFieldScoresJson = JsonSerializer.Serialize(score.FieldScores, JsonOptions);
        metadata.MetadataQualityScoredAt = scoredAt;
    }

    public static MetadataQualityScoreResponse? ToResponse(PaperMetadata metadata)
    {
        if (metadata.MetadataQualityTotalScore is null
            && string.IsNullOrWhiteSpace(metadata.MetadataQualityGrade)
            && metadata.MetadataQualityCanProceed is null)
        {
            return null;
        }

        return new MetadataQualityScoreResponse
        {
            TotalScore = metadata.MetadataQualityTotalScore ?? 0,
            CoreScore = metadata.MetadataQualityCoreScore ?? 0,
            ExtendedScore = metadata.MetadataQualityExtendedScore ?? 0,
            EnrichmentScore = metadata.MetadataQualityEnrichmentScore ?? 0,
            Grade = metadata.MetadataQualityGrade ?? string.Empty,
            CanProceed = metadata.MetadataQualityCanProceed ?? false,
            MissingFields = Deserialize<List<string>>(metadata.MetadataQualityMissingFieldsJson) ?? [],
            Warnings = Deserialize<List<string>>(metadata.MetadataQualityWarningsJson) ?? [],
            FieldScores = Deserialize<Dictionary<string, int>>(metadata.MetadataQualityFieldScoresJson) ?? []
        };
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
