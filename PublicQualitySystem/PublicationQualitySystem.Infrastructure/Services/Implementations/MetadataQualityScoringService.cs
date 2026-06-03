using System.Text.Json;
using System.Text.RegularExpressions;
using PublicationQualitySystem.Application.DTOs.Grobid;
using PublicationQualitySystem.Application.DTOs.Metadata;
using PublicationQualitySystem.Application.Services.Interfaces;
using PublicationQualitySystem.Domain.Entities;
using PublicationQualitySystem.Domain.Enums;

namespace PublicationQualitySystem.Infrastructure.Services.Implementations;

public sealed partial class MetadataQualityScoringService : IMetadataQualityScoringService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public MetadataQualityScoreResponse Calculate(PaperMetadata metadata)
    {
        var authors = Deserialize<IReadOnlyList<AuthorDto>>(metadata.AuthorsJson) ?? Array.Empty<AuthorDto>();
        var keywords = Deserialize<IReadOnlyList<string>>(metadata.KeywordsJson) ?? Array.Empty<string>();
        var references = Deserialize<IReadOnlyList<ReferenceDto>>(metadata.ReferencesJson) ?? Array.Empty<ReferenceDto>();
        var fundingOrganizations = GetFundingOrganizations(metadata);

        var response = new MetadataQualityScoreResponse();

        AddFieldScore(response, "title", 10, HasValue(metadata.Title));
        AddFieldScore(response, "authors", 10, authors.Any(x => HasValue(x.FullName)));
        AddFieldScore(response, "doi", 15, IsValidDoi(metadata.Doi));
        AddFieldScore(response, "abstract", 10, HasValue(metadata.Abstract));
        AddSourceNameScore(response, metadata);
        AddFieldScore(response, "publicationDateOrYear", 5, metadata.PublicationYear is not null);
        AddFieldScore(response, "references", 10, references.Count > 0);

        AddFieldScore(response, "affiliations", 5, authors.Any(x => HasValue(x.Affiliation)));
        AddFieldScore(response, "keywords", 5, keywords.Any(HasValue));
        AddFieldScore(response, "publisher", 5, HasValue(metadata.Publisher));
        var hasFunding = fundingOrganizations.Count > 0;
        AddFieldScore(response, "funding", 5, hasFunding);
        if (!hasFunding)
        {
            response.Warnings.Add("Funding metadata is missing. Funding is important but does not block the pipeline by itself.");
        }

        AddFieldScore(response, "orcid", 4, HasOrcid(metadata.AuthorsJson));
        AddFieldScore(response, "authorEmail", 3, authors.Any(x => HasValue(x.Email)));
        AddFieldScore(response, "correspondingAuthor", 3, HasValue(metadata.CorrespondingAuthor));

        response.CoreScore = Sum(response, "title", "authors", "doi", "abstract", "sourceName", "publicationDateOrYear", "references");
        response.ExtendedScore = Sum(response, "affiliations", "keywords", "publisher", "funding");
        response.EnrichmentScore = Sum(response, "orcid", "authorEmail", "correspondingAuthor");
        response.TotalScore = response.CoreScore + response.ExtendedScore + response.EnrichmentScore;

        var grade = GetGrade(response.TotalScore);
        response.Grade = grade.ToString();
        response.CanProceed = grade is MetadataQualityGrade.EXCELLENT
            or MetadataQualityGrade.GOOD
            or MetadataQualityGrade.ACCEPTABLE;

        if (grade == MetadataQualityGrade.ACCEPTABLE)
        {
            response.Warnings.Add("Metadata quality is acceptable but should be reviewed when possible.");
        }
        else if (grade == MetadataQualityGrade.POOR)
        {
            response.Warnings.Add("Metadata quality is poor. Human review is required before Integrity Screening.");
        }
        else if (grade == MetadataQualityGrade.FAIL)
        {
            response.Warnings.Add("Metadata quality failed. Human review is required before Integrity Screening.");
        }

        return response;
    }

    public static IReadOnlyList<string> GetFundingOrganizations(PaperMetadata metadata)
    {
        var organizations = Deserialize<IReadOnlyList<string>>(metadata.FundingOrganizationsJson) ?? Array.Empty<string>();
        return organizations
            .Select(x => x?.Trim())
            .Where(IsMeaningfulFundingOrganization)
            .Select(x => x!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static void AddSourceNameScore(MetadataQualityScoreResponse response, PaperMetadata metadata)
    {
        var isConference = (HasValue(metadata.ConferenceName) || HasValue(metadata.Venue))
            && !HasValue(metadata.Journal);
        var hasRequiredSourceName = isConference
            ? HasValue(metadata.ConferenceName)
            : HasValue(metadata.Journal);

        AddFieldScore(response, "sourceName", 10, hasRequiredSourceName);
        if (!hasRequiredSourceName)
        {
            response.Warnings.Add(isConference
                ? "Conference source detected, but conferenceName is missing."
                : "Journal source detected, but journalName is missing.");
        }
    }

    private static void AddFieldScore(MetadataQualityScoreResponse response, string fieldName, int maxScore, bool isValid)
    {
        response.FieldScores[fieldName] = isValid ? maxScore : 0;
        if (!isValid)
        {
            response.MissingFields.Add(fieldName);
        }
    }

    private static int Sum(MetadataQualityScoreResponse response, params string[] fields) =>
        fields.Sum(field => response.FieldScores.GetValueOrDefault(field));

    private static MetadataQualityGrade GetGrade(int score) => score switch
    {
        >= 90 => MetadataQualityGrade.EXCELLENT,
        >= 80 => MetadataQualityGrade.GOOD,
        >= 70 => MetadataQualityGrade.ACCEPTABLE,
        >= 50 => MetadataQualityGrade.POOR,
        _ => MetadataQualityGrade.FAIL
    };

    private static bool IsValidDoi(string? doi) =>
        HasValue(doi) && DoiRegex().IsMatch(doi!.Trim());

    private static bool HasOrcid(string? authorsJson)
    {
        if (!HasValue(authorsJson))
        {
            return false;
        }

        var json = authorsJson!.Trim();
        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.ValueKind == JsonValueKind.Array
                && document.RootElement.EnumerateArray().Any(AuthorHasOrcid);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool AuthorHasOrcid(JsonElement author)
    {
        if (author.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        foreach (var property in author.EnumerateObject())
        {
            if (string.Equals(property.Name, "orcid", StringComparison.OrdinalIgnoreCase)
                && property.Value.ValueKind == JsonValueKind.String
                && HasValue(property.Value.GetString()))
            {
                return true;
            }
        }

        return false;
    }

    private static T? Deserialize<T>(string? json)
    {
        if (!HasValue(json))
        {
            return default;
        }

        var content = json!.Trim();
        try
        {
            return JsonSerializer.Deserialize<T>(content, JsonOptions);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    private static bool HasValue(string? value) => !string.IsNullOrWhiteSpace(value);

    private static bool IsMeaningfulFundingOrganization(string? value) =>
        HasValue(value) && !value!.Trim().Equals("unknown", StringComparison.OrdinalIgnoreCase);

    [GeneratedRegex(@"^10\.\d{4,9}/[-._;()/:A-Z0-9]+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DoiRegex();
}
