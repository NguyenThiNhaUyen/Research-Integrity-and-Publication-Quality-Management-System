using System.Text.RegularExpressions;
using PublicationQualitySystem.Application.DTOs.Crossref;
using PublicationQualitySystem.Application.DTOs.Grobid;
using PublicationQualitySystem.Application.DTOs.Metadata;
using PublicationQualitySystem.Application.DTOs.References;
using PublicationQualitySystem.Application.Services.Interfaces;

namespace PublicationQualitySystem.Infrastructure.Services.Implementations;

public sealed partial class MetadataNormalizerService(IReferenceNormalizer referenceNormalizer) : IMetadataNormalizerService
{
    public MetadataNormalizationResult Normalize(
        GrobidMetadataResponse metadata,
        CrossrefMetadataResponse? crossrefMetadata = null)
    {
        var issues = new List<string>();
        var warnings = new List<string>();
        var dirtyFields = 0;

        var normalizedDoi = NormalizeDoi(metadata.Doi ?? crossrefMetadata?.Doi);
        if (!string.Equals(metadata.Doi, normalizedDoi, StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(metadata.Doi))
        {
            dirtyFields++;
        }

        var publisher = NormalizePublisher(metadata.Publisher, crossrefMetadata?.Publisher, normalizedDoi, issues, warnings, ref dirtyFields);
        var keywords = NormalizeKeywords(metadata.Keywords, issues, warnings, ref dirtyFields);
        var references = NormalizeReferences(
            metadata.References,
            metadata.Title,
            metadata.Journal,
            metadata.Volume,
            metadata.Pages,
            out var referenceResults);
        var journal = NormalizeJournal(metadata.Journal, metadata.Title, metadata.Venue, crossrefMetadata?.Journal, ref dirtyFields);
        var pages = NormalizePages(metadata.Pages, normalizedDoi, issues, ref dirtyFields);
        var publicationYear = NormalizePublicationYear(metadata.PublicationYear ?? crossrefMetadata?.PublicationYear, ref dirtyFields);

        var normalized = new GrobidMetadataResponse
        {
            Title = NormalizeSpaces(metadata.Title),
            Authors = metadata.Authors,
            Abstract = NormalizeSpaces(metadata.Abstract),
            Doi = normalizedDoi,
            ArxivId = NormalizeSpaces(metadata.ArxivId),
            Journal = journal,
            Publisher = publisher,
            Venue = NormalizeSpaces(metadata.Venue),
            ConferenceName = NormalizeSpaces(metadata.ConferenceName),
            PublicationYear = publicationYear,
            Volume = NormalizeSimpleToken(metadata.Volume),
            Issue = NormalizeSimpleToken(metadata.Issue),
            Pages = pages,
            CorrespondingAuthor = NormalizeSpaces(metadata.CorrespondingAuthor),
            ReceivedDate = metadata.ReceivedDate,
            RevisedDate = metadata.RevisedDate,
            AcceptedDate = metadata.AcceptedDate,
            PublishedDate = metadata.PublishedDate ?? crossrefMetadata?.PublishedDate,
            OpenAccessLicense = NormalizeLicense(metadata.OpenAccessLicense),
            MetadataSource = metadata.MetadataSource,
            DoiSource = metadata.DoiSource,
            JournalSource = metadata.JournalSource,
            Keywords = keywords,
            FundingOrganizations = metadata.FundingOrganizations
                .Select(NormalizeSpaces)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            References = references,
            RawGrobidXml = metadata.RawGrobidXml
        };

        var referenceIssueCodes = referenceResults
            .SelectMany(x => x.IssueCodes)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var allIssueCodes = issues
            .Concat(referenceIssueCodes)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var referenceDirtyCount = referenceResults.Sum(x => x.IssueCodes.Count);
        dirtyFields += referenceDirtyCount;

        return new MetadataNormalizationResult
        {
            Metadata = normalized,
            MainMetadataCleanlinessScore = CalculateMainCleanlinessScore(issues.Count),
            ReferenceCleanlinessScore = CalculateReferenceCleanlinessScore(referenceResults),
            DirtyFieldCount = dirtyFields,
            IssueCodes = allIssueCodes,
            WarningMessages = warnings
                .Concat(referenceResults.SelectMany(x => x.WarningMessages))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            ReferenceResults = referenceResults
        };
    }

    private static string? NormalizePublisher(
        string? grobidPublisher,
        string? crossrefPublisher,
        string? doi,
        List<string> issues,
        List<string> warnings,
        ref int dirtyFields)
    {
        var grobid = NormalizeSpaces(grobidPublisher);
        var crossref = NormalizeSpaces(crossrefPublisher);
        var heuristic = PublisherFromDoi(doi);
        var chosen = crossref ?? heuristic ?? grobid;

        if (!string.IsNullOrWhiteSpace(grobid)
            && !string.IsNullOrWhiteSpace(chosen)
            && !PublishersEquivalent(grobid, chosen))
        {
            issues.Add("MAIN_PUBLISHER_MISMATCH");
            warnings.Add($"Publisher normalized from '{grobid}' to '{chosen}'.");
            dirtyFields++;
        }

        return chosen;
    }

    private static IReadOnlyList<string> NormalizeKeywords(
        IReadOnlyList<string> keywords,
        List<string> issues,
        List<string> warnings,
        ref int dirtyFields)
    {
        var values = keywords
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .SelectMany(SplitKeyword)
            .Select(NormalizeSpaces)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (keywords.Count == 1 && values.Length > 1)
        {
            issues.Add("KEYWORDS_NOT_SPLIT");
            warnings.Add("Combined keyword string was split into separate keywords.");
            dirtyFields++;
        }

        return values;
    }

    private IReadOnlyList<ReferenceDto> NormalizeReferences(
        IReadOnlyList<ReferenceDto> references,
        string? metadataTitle,
        string? parentJournal,
        string? parentVolume,
        string? parentPages,
        out IReadOnlyList<ReferenceQualityResult> referenceResults)
    {
        var results = references
            .Select(reference =>
            {
                var normalization = referenceNormalizer.NormalizeDetailed(reference);
                var normalized = normalization.Reference;
                var boundarySuspect = ApplyParentBoundaryCheck(normalized, metadataTitle, parentJournal, parentVolume, parentPages);
                var hasTitle = !string.IsNullOrWhiteSpace(normalized.Title);
                var hasAuthor = normalized.Authors.Count > 0;
                var hasYear = normalized.PublicationYear.HasValue;
                var hasVenue = !boundarySuspect && (!string.IsNullOrWhiteSpace(normalized.Journal) || !string.IsNullOrWhiteSpace(normalized.Publisher));
                var hasDoi = !string.IsNullOrWhiteSpace(normalized.Doi);
                var issueCodes = normalization.IssueCodes
                    .Concat(normalized.IssueCodes)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                return new ReferenceQualityResult
                {
                    Reference = normalized,
                    NormalizedTitle = normalized.Title,
                    NormalizedDoi = normalized.Doi,
                    NormalizedJournal = normalized.Journal,
                    NormalizedYear = normalized.PublicationYear,
                    DoiFormatValid = !hasDoi || referenceNormalizer.IsValidDoiFormat(normalized.Doi),
                    MetadataCompletenessScore = ReferenceQualityService.CalculateCompletenessScore(hasTitle, hasAuthor, hasYear, hasVenue, hasDoi),
                    ParseConfidenceScore = normalization.ParseConfidenceScore,
                    IssueCodes = issueCodes,
                    WarningMessages = normalization.WarningMessages.Concat(normalized.WarningMessages).ToArray()
                };
            })
            .ToArray();

        referenceResults = results;
        return results.Select(x => x.Reference).ToArray();
    }

    private static bool ApplyParentBoundaryCheck(ReferenceDto reference, string? metadataTitle, string? parentJournal, string? parentVolume, string? parentPages)
    {
        var titleIsMain = AreSameText(reference.Title, metadataTitle);
        var missingDoi = string.IsNullOrWhiteSpace(reference.Doi);
        var inherited = missingDoi
            && !titleIsMain
            && (AreSameText(reference.Journal, parentJournal)
                || AreSameText(reference.Volume, parentVolume)
                || AreSameText(reference.Pages, parentPages));
        if (!inherited)
        {
            return false;
        }

        reference.IssueCodes = reference.IssueCodes
            .Append("REFERENCE_BOUNDARY_SUSPECT")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        reference.WarningMessages = reference.WarningMessages
            .Append("Reference journal/volume/pages look inherited from the main paper and are excluded from quality scoring.")
            .ToArray();
        return true;
    }

    private static string? NormalizeJournal(string? journal, string? title, string? venue, string? crossrefJournal, ref int dirtyFields)
    {
        var value = NormalizeSpaces(crossrefJournal) ?? ReferenceNormalizer.NormalizeJournal(journal, title, venue);
        if (!string.Equals(journal, value, StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(journal))
        {
            dirtyFields++;
        }

        return value;
    }

    private static string? NormalizePages(string? pages, string? doi, List<string> issues, ref int dirtyFields)
    {
        var value = NormalizeSimpleToken(pages);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!SafePagesRegex().IsMatch(value)
            || (!string.IsNullOrWhiteSpace(doi)
                && doi.Contains(value, StringComparison.OrdinalIgnoreCase)
                && !StandaloneArticleNumberRegex().IsMatch(value)))
        {
            issues.Add("REFERENCE_PAGES_SUSPECT");
            dirtyFields++;
            return null;
        }

        return value;
    }

    private static int? NormalizePublicationYear(int? year, ref int dirtyFields)
    {
        if (!year.HasValue)
        {
            return null;
        }

        if (year.Value < 1900 || year.Value > DateTime.UtcNow.Year + 1)
        {
            dirtyFields++;
            return null;
        }

        return year;
    }

    private static IEnumerable<string> SplitKeyword(string keyword)
    {
        foreach (var part in keyword.Split([';', ',', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var value = part;
            string next;
            do
            {
                next = ElsevierKeywordBoundaryRegex().Replace(value, "${left}|${right}");
                if (next == value)
                {
                    break;
                }

                value = next;
            }
            while (true);

            foreach (var split in value.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                yield return split;
            }
        }
    }

    private static string? PublisherFromDoi(string? doi)
    {
        if (string.IsNullOrWhiteSpace(doi))
        {
            return null;
        }

        return doi.ToLowerInvariant() switch
        {
            var value when value.StartsWith("10.1016/", StringComparison.Ordinal) => "Elsevier",
            var value when value.StartsWith("10.1109/", StringComparison.Ordinal) => "IEEE",
            var value when value.StartsWith("10.1145/", StringComparison.Ordinal) => "ACM",
            var value when value.StartsWith("10.1007/", StringComparison.Ordinal) => "Springer",
            _ => null
        };
    }

    private static bool PublishersEquivalent(string left, string right) =>
        NormalizePublisherComparable(left) == NormalizePublisherComparable(right);

    private static string NormalizePublisherComparable(string value) =>
        value.Trim().ToLowerInvariant() switch
        {
            "association for computing machinery" => "acm",
            "institute of electrical and electronics engineers" => "ieee",
            "springer nature" => "springer",
            var normalized => normalized
        };

    private static string? NormalizeDoi(string? doi)
    {
        if (string.IsNullOrWhiteSpace(doi))
        {
            return null;
        }

        var value = doi.Trim()
            .Replace("https://doi.org/", "", StringComparison.OrdinalIgnoreCase)
            .Replace("http://doi.org/", "", StringComparison.OrdinalIgnoreCase)
            .Replace("https://dx.doi.org/", "", StringComparison.OrdinalIgnoreCase)
            .Replace("http://dx.doi.org/", "", StringComparison.OrdinalIgnoreCase)
            .Replace("doi:", "", StringComparison.OrdinalIgnoreCase)
            .Trim()
            .TrimEnd('.', ',', ';', ')', ']')
            .ToLowerInvariant();

        return DoiRegex().IsMatch(value) ? value : NormalizeSpaces(value);
    }

    private static string? NormalizeSimpleToken(string? value) =>
        NormalizeSpaces(value)?.Trim(',', ';', '.');

    private static string? NormalizeLicense(string? license)
    {
        var value = NormalizeSpaces(license);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (value.Contains("creativecommons.org/licenses/by/4.0", StringComparison.OrdinalIgnoreCase)
            || value.Contains("CC BY 4.0", StringComparison.OrdinalIgnoreCase)
            || value.Contains("Creative Commons Attribution 4.0", StringComparison.OrdinalIgnoreCase))
        {
            return "CC BY 4.0";
        }

        return value;
    }

    private static bool AreSameText(string? left, string? right) =>
        !string.IsNullOrWhiteSpace(left)
        && !string.IsNullOrWhiteSpace(right)
        && string.Equals(NormalizeComparable(left), NormalizeComparable(right), StringComparison.OrdinalIgnoreCase);

    private static string NormalizeComparable(string value) =>
        Regex.Replace(value.ToLowerInvariant(), @"\s+", " ").Trim();

    private static string? NormalizeSpaces(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return string.Join(" ", value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    private static int CalculateMainCleanlinessScore(int issueCount) =>
        Math.Clamp(100 - (issueCount * 15), 0, 100);

    private static int CalculateReferenceCleanlinessScore(IReadOnlyList<ReferenceQualityResult> references)
    {
        if (references.Count == 0)
        {
            return 100;
        }

        var averageConfidence = references.Average(x => x.ParseConfidenceScore);
        var issuePenalty = references.Sum(x => x.IssueCodes.Count(issue =>
            issue is "REFERENCE_TITLE_POLLUTED" or "REFERENCE_JOURNAL_SUSPECT" or "REFERENCE_AUTHOR_LOW_CONFIDENCE" or "REFERENCE_PAGES_SUSPECT" or "REFERENCE_BOUNDARY_SUSPECT")) * 5;
        return Math.Clamp((int)Math.Round(averageConfidence - issuePenalty), 0, 100);
    }

    [GeneratedRegex(@"(?<doi>10\.\d{4,9}/[-._;()/:A-Z0-9]+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DoiRegex();

    [GeneratedRegex(@"(?<left>Edge computing)\s+(?<right>Energy efficiency)|(?<left>Energy efficiency)\s+(?<right>5G)|(?<left>5G)\s+(?<right>Wireless networks)|(?<left>Wireless networks)\s+(?<right>Sustainable communications)", RegexOptions.CultureInvariant)]
    private static partial Regex ElsevierKeywordBoundaryRegex();

    [GeneratedRegex(@"^\d{1,6}(?:-\d{1,6})?$", RegexOptions.CultureInvariant)]
    private static partial Regex SafePagesRegex();

    [GeneratedRegex(@"^\d{5,6}$", RegexOptions.CultureInvariant)]
    private static partial Regex StandaloneArticleNumberRegex();
}
