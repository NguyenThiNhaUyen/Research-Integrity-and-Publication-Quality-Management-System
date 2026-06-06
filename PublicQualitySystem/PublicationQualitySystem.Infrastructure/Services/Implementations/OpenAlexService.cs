using System.Net;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using PublicationQualitySystem.Application.DTOs.Grobid;
using PublicationQualitySystem.Application.DTOs.OpenAlex;
using PublicationQualitySystem.Application.Mappings;
using PublicationQualitySystem.Application.Repositories.Interfaces;
using PublicationQualitySystem.Application.Services.Interfaces;
using PublicationQualitySystem.Domain.Entities;
using PublicationQualitySystem.Domain.Enums;
using PublicationQualitySystem.Infrastructure.Options;
using PublicationQualitySystem.Infrastructure.Security;
using PublicationQualitySystem.Shared.Exceptions;
using PublicationQualitySystem.Shared.Extensions;

namespace PublicationQualitySystem.Infrastructure.Services.Implementations;

public sealed partial class OpenAlexService(
    HttpClient httpClient,
    IOptions<OpenAlexOptions> options,
    IOpenAlexRepository openAlexRepository,
    IPaperVersionRepository versions,
    ICurrentUserProvider currentUser,
    ILogger<OpenAlexService> logger) : IOpenAlexService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<SimilarityResultResponse> GetSimilarityByPaperVersionIdAsync(
        long paperVersionId,
        CancellationToken cancellationToken = default)
    {
        var version = await versions.FindByIdAsync(paperVersionId, cancellationToken);
        if (version is null)
        {
            throw new AppException(AuditLogErrorCode.NotFound);
        }

        EnsureCanViewPaper(version.Paper);

        var check = await openAlexRepository.FindSimilarityByPaperVersionIdAsync(paperVersionId, cancellationToken);
        if (check is null)
        {
            throw new AppException(PaperErrorCode.MetadataNotFound);
        }

        return SimilarityResultMapper.ToResponse(check, paperVersionId);
    }

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
                .Select(candidate => new
                {
                    Candidate = candidate,
                    Score = CalculateSimilarity(
                        metadata,
                        candidate,
                        referenceSimilarityOverride: null,
                        authorNameThreshold: AuthorNameThreshold()).OverallScore
                })
                .OrderByDescending(x => x.Score)
                .FirstOrDefault()
                ?.Candidate;
        }

        if (bestWork is null)
        {
            return null;
        }

        var referenceSimilarity = await ResolveReferenceSimilarityAsync(metadata, bestWork, cancellationToken);
        return CalculateSimilarity(metadata, bestWork, referenceSimilarity, AuthorNameThreshold());
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

    public static OpenAlexSimilarityResult CalculateSimilarity(PaperMetadata metadata, OpenAlexWorkDto work) =>
        CalculateSimilarity(metadata, work, referenceSimilarityOverride: null, authorNameThreshold: 75);

    private static OpenAlexSimilarityResult CalculateSimilarity(
        PaperMetadata metadata,
        OpenAlexWorkDto work,
        double? referenceSimilarityOverride,
        int authorNameThreshold)
    {
        var sourceAuthors = Deserialize<IReadOnlyList<AuthorDto>>(metadata.AuthorsJson) ?? Array.Empty<AuthorDto>();
        var sourceReferences = Deserialize<IReadOnlyList<ReferenceDto>>(metadata.ReferencesJson) ?? Array.Empty<ReferenceDto>();

        var titleSimilarity = TextSimilarity(metadata.Title, work.Title);
        var authorSimilarity = AuthorSimilarity(sourceAuthors, work.Authors, authorNameThreshold);
        var abstractSimilarity = TextSimilarity(metadata.Abstract, work.Abstract);
        var referenceSimilarity = referenceSimilarityOverride ?? ReferenceSimilarity(sourceReferences, work.ReferencedWorks);
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

                var rawAffiliations = ParseRawAffiliations(authorship);
                return new AuthorDto
                {
                    FullName = GetString(author, "display_name"),
                    RawAuthorName = GetString(authorship, "raw_author_name"),
                    Orcid = NormalizeOrcid(GetString(author, "orcid") ?? GetString(authorship, "raw_orcid")),
                    IsCorresponding = GetBool(authorship, "is_corresponding"),
                    Affiliation = rawAffiliations.Count == 0 ? null : string.Join("; ", rawAffiliations)
                };
            })
            .Where(author => !string.IsNullOrWhiteSpace(author?.FullName))
            .Select(author => author!)
            .ToArray();
    }

    private static IReadOnlyList<string> ParseRawAffiliations(JsonElement authorship)
    {
        if (!authorship.TryGetProperty("raw_affiliation_strings", out var array) || array.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<string>();
        }

        return array.EnumerateArray()
            .Where(x => x.ValueKind == JsonValueKind.String)
            .Select(x => x.GetString())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => NormalizeSpaces(x!))
            .Distinct(StringComparer.OrdinalIgnoreCase)
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

    private async Task<double> ResolveReferenceSimilarityAsync(
        PaperMetadata metadata,
        OpenAlexWorkDto work,
        CancellationToken cancellationToken)
    {
        var references = Deserialize<IReadOnlyList<ReferenceDto>>(metadata.ReferencesJson) ?? Array.Empty<ReferenceDto>();
        var openAlexReferenceIds = work.ReferencedWorks
            .Select(NormalizeOpenAlexWorkId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (references.Count == 0 || openAlexReferenceIds.Count == 0)
        {
            logger.LogInformation(
                "Reference similarity could not be computed because references were not available. PaperId={PaperId}, SourceReferenceCount={SourceReferenceCount}, OpenAlexReferenceCount={OpenAlexReferenceCount}",
                metadata.PaperId,
                references.Count,
                openAlexReferenceIds.Count);
            return 0;
        }

        var maxReferences = Math.Clamp(options.Value.MaxReferenceResolution <= 0 ? 30 : options.Value.MaxReferenceResolution, 1, 100);
        var resolvedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var unresolved = 0;

        foreach (var reference in references.Take(maxReferences))
        {
            var resolvedId = await ResolveReferenceToOpenAlexIdAsync(reference, cancellationToken);
            if (string.IsNullOrWhiteSpace(resolvedId))
            {
                unresolved++;
                continue;
            }

            resolvedIds.Add(resolvedId);
        }

        if (resolvedIds.Count == 0)
        {
            logger.LogWarning(
                "Reference similarity could not be computed because references were not resolvable. PaperId={PaperId}, TriedReferenceCount={TriedReferenceCount}, UnresolvedReferenceCount={UnresolvedReferenceCount}",
                metadata.PaperId,
                Math.Min(references.Count, maxReferences),
                unresolved);
            return ReferenceSimilarity(references, work.ReferencedWorks);
        }

        var matches = resolvedIds.Count(openAlexReferenceIds.Contains);
        var score = Math.Round(matches * 100.0 / resolvedIds.Count, 2);
        logger.LogInformation(
            "Reference similarity computed with OpenAlex IDs. PaperId={PaperId}, ResolvedReferenceCount={ResolvedReferenceCount}, MatchedReferenceCount={MatchedReferenceCount}, UnresolvedReferenceCount={UnresolvedReferenceCount}, Score={Score}",
            metadata.PaperId,
            resolvedIds.Count,
            matches,
            unresolved,
            score);

        return score;
    }

    private async Task<string?> ResolveReferenceToOpenAlexIdAsync(ReferenceDto reference, CancellationToken cancellationToken)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(reference.Doi))
            {
                var byDoi = await GetWorkByDoiAsync(reference.Doi, cancellationToken);
                var doiResolvedId = NormalizeOpenAlexWorkId(byDoi?.Id);
                if (!string.IsNullOrWhiteSpace(doiResolvedId))
                {
                    return doiResolvedId;
                }
            }

            if (string.IsNullOrWhiteSpace(reference.Title))
            {
                return null;
            }

            var candidates = await SearchWorksByTitleAsync(reference.Title, maxCandidates: 1, cancellationToken);
            var candidate = candidates.FirstOrDefault();
            if (candidate is null)
            {
                return null;
            }

            var titleScore = TextSimilarity(reference.Title, candidate.Title);
            var threshold = Math.Clamp(options.Value.ReferenceTitleThreshold <= 0 ? 85 : options.Value.ReferenceTitleThreshold, 1, 100);
            if (titleScore < threshold)
            {
                return null;
            }

            if (reference.PublicationYear.HasValue
                && candidate.PublicationYear.HasValue
                && Math.Abs(reference.PublicationYear.Value - candidate.PublicationYear.Value) > 1)
            {
                return null;
            }

            return NormalizeOpenAlexWorkId(candidate.Id);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(
                ex,
                "OpenAlex reference resolution failed. ReferenceTitle={ReferenceTitle}, ReferenceDoi={ReferenceDoi}",
                reference.Title,
                reference.Doi);
            return null;
        }
    }

    private int AuthorNameThreshold() =>
        Math.Clamp(options.Value.AuthorNameThreshold <= 0 ? 75 : options.Value.AuthorNameThreshold, 1, 100);

    private static double AuthorSimilarity(IReadOnlyList<AuthorDto> left, IReadOnlyList<AuthorDto> right, int authorNameThreshold)
    {
        var leftAuthors = left.Where(HasAuthorIdentity).ToArray();
        var rightAuthors = right.Where(HasAuthorIdentity).ToArray();
        if (leftAuthors.Length == 0 || rightAuthors.Length == 0)
        {
            return 0;
        }

        var matchedRightIndexes = new HashSet<int>();
        var matches = 0;

        foreach (var leftAuthor in leftAuthors)
        {
            var bestIndex = -1;
            var bestScore = 0.0;
            for (var i = 0; i < rightAuthors.Length; i++)
            {
                if (matchedRightIndexes.Contains(i))
                {
                    continue;
                }

                var score = AuthorMatchScore(leftAuthor, rightAuthors[i], authorNameThreshold);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestIndex = i;
                }
            }

            if (bestIndex >= 0 && bestScore > 0)
            {
                matchedRightIndexes.Add(bestIndex);
                matches++;
            }
        }

        return Math.Round(matches * 100.0 / Math.Max(leftAuthors.Length, rightAuthors.Length), 2);
    }

    private static double ReferenceSimilarity(IReadOnlyList<ReferenceDto> references, IReadOnlyList<string> openAlexReferences)
    {
        var sourceDoiTokens = references
            .Select(x => NormalizeDoi(x.Doi))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var openAlexTokens = openAlexReferences
            .Select(NormalizeReferenceToken)
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
        NormalizeSpaces(value).ToLowerInvariant();

    private static string NormalizePersonName(string? value)
    {
        var text = RemoveDiacritics(NormalizeSpaces(value)).ToLowerInvariant();
        return NameTokenRegex().Matches(text)
            .Select(match => match.Value)
            .Where(token => !string.IsNullOrWhiteSpace(token))
            .Aggregate(string.Empty, (current, token) => string.IsNullOrEmpty(current) ? token : $"{current} {token}");
    }

    private static string RemoveDiacritics(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Normalize(NormalizationForm.FormD);
        var chars = normalized
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .ToArray();
        return new string(chars).Normalize(NormalizationForm.FormC);
    }

    private static string NormalizeSpaces(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : SpaceRegex().Replace(value.Trim(), " ");

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

    private static string NormalizeOpenAlexWorkId(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim();
        var slashIndex = trimmed.LastIndexOf('/');
        var id = slashIndex >= 0 ? trimmed[(slashIndex + 1)..] : trimmed;
        return id.StartsWith("W", StringComparison.OrdinalIgnoreCase)
            ? id.ToUpperInvariant()
            : trimmed.ToLowerInvariant();
    }

    private static string NormalizeReferenceToken(string? value)
    {
        var doi = NormalizeDoi(value);
        return doi.StartsWith("10.", StringComparison.OrdinalIgnoreCase)
            ? doi
            : NormalizeOpenAlexWorkId(value);
    }

    private static string NormalizeOrcid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.Trim()
            .Replace("https://orcid.org/", "", StringComparison.OrdinalIgnoreCase)
            .Replace("http://orcid.org/", "", StringComparison.OrdinalIgnoreCase)
            .ToUpperInvariant();
    }

    private static bool HasAuthorIdentity(AuthorDto author) =>
        !string.IsNullOrWhiteSpace(author.FullName)
        || !string.IsNullOrWhiteSpace(author.RawAuthorName)
        || !string.IsNullOrWhiteSpace(author.Orcid);

    private static double AuthorMatchScore(AuthorDto left, AuthorDto right, int authorNameThreshold)
    {
        var leftOrcid = NormalizeOrcid(left.Orcid);
        var rightOrcid = NormalizeOrcid(right.Orcid);
        if (!string.IsNullOrWhiteSpace(leftOrcid)
            && !string.IsNullOrWhiteSpace(rightOrcid)
            && leftOrcid.Equals(rightOrcid, StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        var leftName = NormalizePersonName(left.FullName ?? left.RawAuthorName);
        var rightName = NormalizePersonName(right.FullName ?? right.RawAuthorName);
        if (string.IsNullOrWhiteSpace(leftName) || string.IsNullOrWhiteSpace(rightName))
        {
            return 0;
        }

        if (leftName.Equals(rightName, StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        if (SurnameAndInitialMatch(leftName, rightName))
        {
            return 0.95;
        }

        var tokenScore = PersonNameTokenSimilarity(leftName, rightName);
        return tokenScore * 100 >= authorNameThreshold ? tokenScore : 0;
    }

    private static bool SurnameAndInitialMatch(string leftName, string rightName)
    {
        var leftTokens = leftName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var rightTokens = rightName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (leftTokens.Length == 0 || rightTokens.Length == 0)
        {
            return false;
        }

        var leftSurname = leftTokens[^1];
        var rightSurname = rightTokens[^1];
        if (!leftSurname.Equals(rightSurname, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var leftInitial = leftTokens[0][0];
        var rightInitial = rightTokens[0][0];
        return char.ToUpperInvariant(leftInitial) == char.ToUpperInvariant(rightInitial);
    }

    private static double PersonNameTokenSimilarity(string leftName, string rightName)
    {
        var leftTokens = leftName.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var rightTokens = rightName.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (leftTokens.Count == 0 || rightTokens.Count == 0)
        {
            return 0;
        }

        var intersection = leftTokens.Intersect(rightTokens, StringComparer.OrdinalIgnoreCase).Count();
        return intersection * 1.0 / Math.Max(leftTokens.Count, rightTokens.Count);
    }

    private static string? GetString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    private static int? GetInt(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var value)
            ? value
            : null;

    private static bool? GetBool(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) && property.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? property.GetBoolean()
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

    private bool CanReadAllPapers() =>
        currentUser.User.HasPermission(nameof(PermissionName.PAPER_READ_ALL))
        || currentUser.User.HasPermission(nameof(PermissionName.AUDIT_LOG_READ));

    private void EnsureCanViewPaper(Paper paper)
    {
        if (CanReadAllPapers())
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(currentUser.Subject)
            && !string.IsNullOrWhiteSpace(paper.CreatedBy)
            && string.Equals(paper.CreatedBy, currentUser.Subject, StringComparison.Ordinal))
        {
            return;
        }

        throw new AppException(AuditLogErrorCode.Forbidden);
    }

    [GeneratedRegex(@"[\p{L}\p{N}]+", RegexOptions.CultureInvariant)]
    private static partial Regex TokenRegex();

    [GeneratedRegex(@"[\p{L}\p{N}]+", RegexOptions.CultureInvariant)]
    private static partial Regex NameTokenRegex();

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex SpaceRegex();
}
