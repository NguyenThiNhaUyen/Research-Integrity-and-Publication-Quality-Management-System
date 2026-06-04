using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using PublicationQualitySystem.Application.DTOs.Grobid;
using PublicationQualitySystem.Application.DTOs.OpenAlex;
using PublicationQualitySystem.Application.Services.Interfaces;
using PublicationQualitySystem.Domain.Entities;
using PublicationQualitySystem.Domain.Enums;
using PublicationQualitySystem.Infrastructure.Options;

namespace PublicationQualitySystem.Infrastructure.Services.Implementations;

public sealed partial class OpenAlexService(
    HttpClient httpClient,
    IOptions<OpenAlexOptions> options,
    ILogger<OpenAlexService> logger) : IOpenAlexService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<OpenAlexWorkDto?> GetWorkByDoiAsync(string doi, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(doi) || string.IsNullOrWhiteSpace(options.Value.ApiKey))
        {
            logger.LogWarning("OpenAlex DOI lookup skipped because DOI or API key is missing.");
            return null;
        }

        var normalizedDoi = NormalizeDoi(doi);
        var path = $"/works/doi:{Uri.EscapeDataString(normalizedDoi)}?api_key={Uri.EscapeDataString(options.Value.ApiKey!)}";
        using var response = await httpClient.GetAsync(path, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return ParseWork(json);
    }

    public async Task<IReadOnlyList<OpenAlexWorkDto>> SearchWorksByTitleAsync(
        string title,
        int maxCandidates,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(options.Value.ApiKey))
        {
            logger.LogWarning("OpenAlex title search skipped because title or API key is missing.");
            return Array.Empty<OpenAlexWorkDto>();
        }

        var perPage = Math.Clamp(maxCandidates <= 0 ? options.Value.MaxCandidates : maxCandidates, 1, 50);
        var path = $"/works?search={Uri.EscapeDataString(title.Trim())}&per-page={perPage}&api_key={Uri.EscapeDataString(options.Value.ApiKey!)}";
        using var response = await httpClient.GetAsync(path, cancellationToken);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return ParseSearchResults(json);
    }

    public async Task<OpenAlexSimilarityResult?> CheckSimilarityAsync(
        PaperMetadata metadata,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(options.Value.ApiKey))
        {
            throw new InvalidOperationException("OpenAlex API key is missing.");
        }

        OpenAlexWorkDto? bestWork = null;
        if (!string.IsNullOrWhiteSpace(metadata.Doi))
        {
            bestWork = await GetWorkByDoiAsync(metadata.Doi, cancellationToken);
        }

        if (bestWork is null && !string.IsNullOrWhiteSpace(metadata.Title))
        {
            var candidates = await SearchWorksByTitleAsync(metadata.Title, options.Value.MaxCandidates, cancellationToken);
            bestWork = candidates
                .Select(candidate => new { Candidate = candidate, Score = CalculateSimilarity(metadata, candidate).OverallScore })
                .OrderByDescending(x => x.Score)
                .FirstOrDefault()
                ?.Candidate;
        }

        return bestWork is null ? null : CalculateSimilarity(metadata, bestWork);
    }

    public static OpenAlexWorkDto? ParseWork(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        using var document = JsonDocument.Parse(json);
        return ParseWork(document.RootElement, json);
    }

    public static IReadOnlyList<OpenAlexWorkDto> ParseSearchResults(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<OpenAlexWorkDto>();
        }

        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("results", out var results) || results.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<OpenAlexWorkDto>();
        }

        return results.EnumerateArray()
            .Select(result => ParseWork(result, result.GetRawText()))
            .Where(work => work is not null)
            .Select(work => work!)
            .ToArray();
    }

    public static string? ReconstructAbstract(JsonElement work)
    {
        if (!work.TryGetProperty("abstract_inverted_index", out var index)
            || index.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var words = new List<(int Position, string Word)>();
        foreach (var property in index.EnumerateObject())
        {
            if (property.Value.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            words.AddRange(property.Value.EnumerateArray()
                .Where(position => position.ValueKind == JsonValueKind.Number && position.TryGetInt32(out _))
                .Select(position => (position.GetInt32(), property.Name)));
        }

        return words.Count == 0
            ? null
            : string.Join(" ", words.OrderBy(x => x.Position).Select(x => x.Word));
    }

    public static OpenAlexSimilarityResult CalculateSimilarity(PaperMetadata metadata, OpenAlexWorkDto work)
    {
        var sourceAuthors = Deserialize<IReadOnlyList<AuthorDto>>(metadata.AuthorsJson) ?? Array.Empty<AuthorDto>();
        var sourceReferences = Deserialize<IReadOnlyList<ReferenceDto>>(metadata.ReferencesJson) ?? Array.Empty<ReferenceDto>();

        var titleSimilarity = TextSimilarity(metadata.Title, work.Title);
        var authorSimilarity = AuthorSimilarity(sourceAuthors, work.Authors);
        var abstractSimilarity = TextSimilarity(metadata.Abstract, work.Abstract);
        var referenceSimilarity = ReferenceSimilarity(sourceReferences, work.ReferencedWorks);
        var overall = Math.Round(
            (titleSimilarity * 0.40)
            + (authorSimilarity * 0.25)
            + (abstractSimilarity * 0.25)
            + (referenceSimilarity * 0.10),
            2);

        return new OpenAlexSimilarityResult
        {
            MatchedOpenAlexId = work.Id,
            MatchedDoi = work.Doi,
            MatchedTitle = work.Title,
            MatchedJournal = work.Journal,
            PublicationYear = work.PublicationYear,
            CitedByCount = work.CitedByCount,
            TitleSimilarity = titleSimilarity,
            AuthorSimilarity = authorSimilarity,
            AbstractSimilarity = abstractSimilarity,
            ReferenceSimilarity = referenceSimilarity,
            OverallScore = overall,
            RiskLevel = overall >= 90 ? SimilarityRiskLevel.HIGH : overall >= 70 ? SimilarityRiskLevel.MEDIUM : SimilarityRiskLevel.LOW,
            RawJson = work.RawJson
        };
    }

    private static OpenAlexWorkDto? ParseWork(JsonElement work, string rawJson)
    {
        if (work.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return new OpenAlexWorkDto
        {
            Id = GetString(work, "id"),
            Doi = NormalizeDoi(GetString(work, "doi")),
            Title = GetString(work, "display_name") ?? GetString(work, "title"),
            PublicationYear = GetInt(work, "publication_year"),
            CitedByCount = GetInt(work, "cited_by_count"),
            Authors = ParseAuthors(work),
            Journal = ParseJournal(work),
            Abstract = ReconstructAbstract(work),
            ReferencedWorks = ParseStringArray(work, "referenced_works"),
            Concepts = ParseConcepts(work),
            RawJson = rawJson
        };
    }

    private static IReadOnlyList<AuthorDto> ParseAuthors(JsonElement work)
    {
        if (!work.TryGetProperty("authorships", out var authorships) || authorships.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<AuthorDto>();
        }

        return authorships.EnumerateArray()
            .Select(authorship =>
            {
                if (!authorship.TryGetProperty("author", out var author))
                {
                    return null;
                }

                return new AuthorDto
                {
                    FullName = GetString(author, "display_name")
                };
            })
            .Where(author => !string.IsNullOrWhiteSpace(author?.FullName))
            .Select(author => author!)
            .ToArray();
    }

    private static string? ParseJournal(JsonElement work)
    {
        if (work.TryGetProperty("primary_location", out var location)
            && location.ValueKind == JsonValueKind.Object
            && location.TryGetProperty("source", out var source)
            && source.ValueKind == JsonValueKind.Object)
        {
            return GetString(source, "display_name");
        }

        return null;
    }

    private static IReadOnlyList<string> ParseStringArray(JsonElement work, string propertyName)
    {
        if (!work.TryGetProperty(propertyName, out var array) || array.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<string>();
        }

        return array.EnumerateArray()
            .Where(x => x.ValueKind == JsonValueKind.String)
            .Select(x => x.GetString())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .ToArray();
    }

    private static IReadOnlyList<string> ParseConcepts(JsonElement work)
    {
        var concepts = new List<string>();
        foreach (var propertyName in new[] { "concepts", "topics" })
        {
            if (!work.TryGetProperty(propertyName, out var array) || array.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            concepts.AddRange(array.EnumerateArray()
                .Select(item => GetString(item, "display_name"))
                .Where(x => !string.IsNullOrWhiteSpace(x))!);
        }

        return concepts.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static double TextSimilarity(string? left, string? right)
    {
        var leftTokens = Tokenize(left);
        var rightTokens = Tokenize(right);
        if (leftTokens.Count == 0 || rightTokens.Count == 0)
        {
            return 0;
        }

        var intersection = leftTokens.Intersect(rightTokens, StringComparer.OrdinalIgnoreCase).Count();
        var union = leftTokens.Union(rightTokens, StringComparer.OrdinalIgnoreCase).Count();
        return Math.Round(intersection * 100.0 / union, 2);
    }

    private static double AuthorSimilarity(IReadOnlyList<AuthorDto> left, IReadOnlyList<AuthorDto> right)
    {
        var leftNames = left.Select(x => NormalizeText(x.FullName)).Where(x => !string.IsNullOrWhiteSpace(x)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var rightNames = right.Select(x => NormalizeText(x.FullName)).Where(x => !string.IsNullOrWhiteSpace(x)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (leftNames.Count == 0 || rightNames.Count == 0)
        {
            return 0;
        }

        var intersection = leftNames.Intersect(rightNames, StringComparer.OrdinalIgnoreCase).Count();
        var union = leftNames.Union(rightNames, StringComparer.OrdinalIgnoreCase).Count();
        return Math.Round(intersection * 100.0 / union, 2);
    }

    private static double ReferenceSimilarity(IReadOnlyList<ReferenceDto> references, IReadOnlyList<string> openAlexReferences)
    {
        var sourceDoiTokens = references
            .Select(x => NormalizeDoi(x.Doi))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var openAlexTokens = openAlexReferences
            .Select(NormalizeText)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (sourceDoiTokens.Count == 0 || openAlexTokens.Count == 0)
        {
            return 0;
        }

        var matches = sourceDoiTokens.Count(doi => openAlexTokens.Any(reference => reference.Contains(doi, StringComparison.OrdinalIgnoreCase)));
        return Math.Round(matches * 100.0 / sourceDoiTokens.Count, 2);
    }

    private static HashSet<string> Tokenize(string? value) =>
        TokenRegex().Matches(NormalizeText(value))
            .Select(match => match.Value)
            .Where(token => token.Length > 1)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static string NormalizeText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();

    private static string NormalizeDoi(string? doi)
    {
        if (string.IsNullOrWhiteSpace(doi))
        {
            return string.Empty;
        }

        return doi.Trim()
            .Replace("https://doi.org/", "", StringComparison.OrdinalIgnoreCase)
            .Replace("http://doi.org/", "", StringComparison.OrdinalIgnoreCase)
            .Replace("doi:", "", StringComparison.OrdinalIgnoreCase)
            .Trim();
    }

    private static string? GetString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    private static int? GetInt(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var value)
            ? value
            : null;

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

    [GeneratedRegex(@"[\p{L}\p{N}]+", RegexOptions.CultureInvariant)]
    private static partial Regex TokenRegex();
}
