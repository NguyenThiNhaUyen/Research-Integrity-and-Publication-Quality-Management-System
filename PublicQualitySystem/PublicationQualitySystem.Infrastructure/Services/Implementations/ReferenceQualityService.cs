using PublicationQualitySystem.Application.DTOs.Grobid;
using PublicationQualitySystem.Application.DTOs.References;
using PublicationQualitySystem.Application.Services.Interfaces;

namespace PublicationQualitySystem.Infrastructure.Services.Implementations;

public sealed class ReferenceQualityService(IReferenceNormalizer normalizer) : IReferenceQualityService
{
    public ReferenceQualityResult Evaluate(ReferenceDto reference)
    {
        var normalization = normalizer.NormalizeDetailed(reference);
        var normalized = normalization.Reference;
        var issues = new List<string>(normalization.IssueCodes.Concat(normalized.IssueCodes));

        var hasTitle = !string.IsNullOrWhiteSpace(normalized.Title);
        var hasAuthor = normalized.Authors.Count > 0;
        var hasYear = normalized.PublicationYear.HasValue;
        var boundarySuspect = issues.Contains("REFERENCE_BOUNDARY_SUSPECT");
        var hasVenue = !boundarySuspect && (!string.IsNullOrWhiteSpace(normalized.Journal) || !string.IsNullOrWhiteSpace(normalized.Publisher));
        var hasDoi = !string.IsNullOrWhiteSpace(normalized.Doi);
        var doiFormatValid = !hasDoi || normalizer.IsValidDoiFormat(normalized.Doi);

        if (!hasDoi)
        {
            issues.Add("REFERENCE_DOI_MISSING");
        }
        else if (!doiFormatValid)
        {
            issues.Add("REFERENCE_INVALID_DOI_FORMAT");
        }

        if (!hasAuthor)
        {
            issues.Add("REFERENCE_MISSING_AUTHOR");
        }

        if (!hasYear)
        {
            issues.Add("REFERENCE_YEAR_MISSING");
        }

        if (!hasVenue)
        {
            issues.Add("REFERENCE_MISSING_VENUE");
        }

        if (IsTitlePolluted(reference.Title, normalized.Title, reference.RawText))
        {
            issues.Add("REFERENCE_TITLE_POLLUTED");
        }

        if (ReferenceNormalizer.LooksLikeAuthorFragmentTitle(reference.Title))
        {
            issues.Add("REFERENCE_TITLE_PARSE_FAILED");
            issues.Add("REFERENCE_PARSE_SUSPECT");
        }

        if (IsJournalSuspect(normalized.Journal, normalized.Title))
        {
            issues.Add("REFERENCE_JOURNAL_SUSPECT");
        }

        var completeness = CalculateCompletenessScore(hasTitle, hasAuthor, hasYear, hasVenue, hasDoi);
        var confidence = Math.Min(normalization.ParseConfidenceScore, CalculateParseConfidence(completeness, issues));
        if (confidence < 60)
        {
            issues.Add("REFERENCE_AUTHOR_LOW_CONFIDENCE");
        }

        return new ReferenceQualityResult
        {
            Reference = normalized,
            NormalizedTitle = normalized.Title,
            NormalizedDoi = normalized.Doi,
            NormalizedJournal = normalized.Journal,
            NormalizedYear = normalized.PublicationYear,
            DoiFormatValid = doiFormatValid,
            MetadataCompletenessScore = completeness,
            ParseConfidenceScore = confidence,
            IssueCodes = issues.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            WarningMessages = normalization.WarningMessages.Concat(normalized.WarningMessages).ToArray()
        };
    }

    public int CalculateReferenceQualityScore(
        IReadOnlyList<ReferenceQualityResult> references,
        int validDoiCount,
        int invalidDoiCount,
        int titleMismatchCount)
    {
        if (references.Count == 0)
        {
            return 0;
        }

        var completeness = references.Average(x => x.MetadataCompletenessScore) * 0.40;
        var referencesWithDoi = references.Count(x => !string.IsNullOrWhiteSpace(x.NormalizedDoi));
        var doiCoverage = referencesWithDoi * 100.0 / references.Count * 0.20;
        var doiValidation = referencesWithDoi == 0 ? 0 : validDoiCount * 100.0 / Math.Max(referencesWithDoi, 1) * 0.20;
        var titleYearMatch = Math.Max(0, 100 - (titleMismatchCount * 10)) * 0.10;
        var cleanlinessIssues = references.Sum(x => x.IssueCodes.Count(issue =>
            issue is "REFERENCE_TITLE_POLLUTED" or "REFERENCE_TITLE_PARSE_FAILED" or "REFERENCE_PARSE_SUSPECT" or "REFERENCE_JOURNAL_SUSPECT" or "REFERENCE_AUTHOR_LOW_CONFIDENCE" or "REFERENCE_PAGES_SUSPECT" or "REFERENCE_BOUNDARY_SUSPECT"));
        var cleanliness = Math.Max(0, 100 - (cleanlinessIssues * 10) - (invalidDoiCount * 5)) * 0.10;

        return Math.Clamp((int)Math.Round(completeness + doiCoverage + doiValidation + titleYearMatch + cleanliness), 0, 100);
    }

    public static int CalculateCompletenessScore(bool hasTitle, bool hasAuthor, bool hasYear, bool hasVenue, bool hasDoi)
    {
        var score = 0;
        if (hasTitle)
        {
            score += 30;
        }

        if (hasAuthor)
        {
            score += 25;
        }

        if (hasYear)
        {
            score += 15;
        }

        if (hasVenue)
        {
            score += 15;
        }

        if (hasDoi)
        {
            score += 15;
        }

        return score;
    }

    private static double CalculateParseConfidence(int completeness, IReadOnlyList<string> issues)
    {
        var penalty = issues.Count(issue => issue is "REFERENCE_TITLE_POLLUTED" or "REFERENCE_JOURNAL_SUSPECT" or "REFERENCE_PAGES_SUSPECT") * 15
            + issues.Count(issue => issue is "REFERENCE_TITLE_PARSE_FAILED") * 25
            + issues.Count(issue => issue is "REFERENCE_PARSE_SUSPECT" or "REFERENCE_AUTHOR_LOW_CONFIDENCE") * 10;
        return Math.Clamp(completeness - penalty, 0, 100);
    }

    private static bool IsTitlePolluted(string? originalTitle, string? normalizedTitle, string? rawText) =>
        !string.IsNullOrWhiteSpace(originalTitle)
        && !string.Equals(originalTitle, normalizedTitle, StringComparison.Ordinal)
        && (originalTitle.Length > 180
            || originalTitle.Contains("doi", StringComparison.OrdinalIgnoreCase)
            || (!string.IsNullOrWhiteSpace(rawText) && originalTitle.Length > (normalizedTitle?.Length ?? 0) + 30));

    private static bool IsJournalSuspect(string? journal, string? title) =>
        !string.IsNullOrWhiteSpace(journal)
        && !string.IsNullOrWhiteSpace(title)
        && (journal.Contains(title, StringComparison.OrdinalIgnoreCase)
            || title.Contains(journal, StringComparison.OrdinalIgnoreCase));
}
