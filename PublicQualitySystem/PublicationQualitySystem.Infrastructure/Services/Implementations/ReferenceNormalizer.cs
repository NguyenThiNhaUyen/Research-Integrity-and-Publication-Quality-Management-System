using System.Text.RegularExpressions;
using PublicationQualitySystem.Application.DTOs.Grobid;
using PublicationQualitySystem.Application.DTOs.References;
using PublicationQualitySystem.Application.Services.Interfaces;

namespace PublicationQualitySystem.Infrastructure.Services.Implementations;

public sealed partial class ReferenceNormalizer : IReferenceNormalizer
{
    private static readonly string[] KnownVenueTerms =
    [
        "IEEE",
        "ACM",
        "Elsevier",
        "Springer",
        "ETSI",
        "Computer Networks",
        "J. Supercomput",
        "Journal of Supercomputing",
        "IEEE Access",
        "IEEE Trans.",
        "IEEE Transactions on",
        "IEEE Communications Magazine",
        "IEEE Transactions",
        "ACM Computing Surveys",
        "Lecture Notes in Computer Science"
    ];

    private static readonly HashSet<string> SurnameParticles = new(StringComparer.OrdinalIgnoreCase)
    {
        "de",
        "da",
        "del",
        "di",
        "van",
        "von",
        "der",
        "den",
        "la",
        "le",
        "dos",
        "das"
    };

    private static readonly string[] TitleLikeAuthorTerms =
    [
        "architecture",
        "working group",
        "survey",
        "network",
        "networks",
        "computing",
        "energy",
        "server",
        "system",
        "systems",
        "communication",
        "communications"
    ];

    public ReferenceDto Normalize(ReferenceDto reference) => NormalizeDetailed(reference).Reference;

    public ReferenceNormalizationResult NormalizeDetailed(ReferenceDto reference)
    {
        var issues = new List<string>();
        var warnings = new List<string>();
        var normalizedDoi = NormalizeDoi(reference.Doi ?? ExtractDoi(reference.RawText));
        var title = CleanTitle(reference.Title, reference.RawText, reference.Journal, normalizedDoi, reference.PublicationYear, reference.Volume, reference.Issue, reference.Pages);
        if (LooksLikeAuthorFragmentTitle(title))
        {
            issues.Add("REFERENCE_TITLE_PARSE_FAILED");
            issues.Add("REFERENCE_PARSE_SUSPECT");
            warnings.Add("Reference title looks like an author fragment and was not trusted as canonical title.");
            title = null;
        }

        var journal = NormalizeJournal(reference.Journal, title, reference.RawText);
        var year = reference.PublicationYear ?? ExtractYear(reference.RawText);
        var pages = NormalizePages(reference.Pages, reference.RawText, normalizedDoi, issues);
        var authors = reference.Authors.Count > 0
            ? reference.Authors
            : ExtractAuthors(reference.RawText);
        var authorConfidence = reference.Authors.Count > 0
            ? 100
            : CalculateAuthorConfidence(reference.RawText, authors);

        if (string.IsNullOrWhiteSpace(normalizedDoi))
        {
            issues.Add("REFERENCE_DOI_MISSING");
        }

        if (IsTitlePolluted(reference.Title, title, reference.RawText))
        {
            issues.Add("REFERENCE_TITLE_POLLUTED");
        }

        if (reference.Authors.Count == 0 && authors.Count == 0)
        {
            issues.Add("REFERENCE_MISSING_AUTHOR");
        }
        else if (reference.Authors.Count == 0 && authorConfidence < 70)
        {
            authors = Array.Empty<AuthorDto>();
            issues.Add("REFERENCE_AUTHOR_LOW_CONFIDENCE");
            warnings.Add("Reference author prefix was detected but not trusted enough to write into canonical metadata.");
        }

        if (!reference.PublicationYear.HasValue && !year.HasValue)
        {
            issues.Add("REFERENCE_YEAR_MISSING");
        }

        if (IsJournalSuspect(reference.Journal, title) || (reference.Journal is not null && journal is null))
        {
            issues.Add("REFERENCE_JOURNAL_SUSPECT");
        }

        var normalized = new ReferenceDto
        {
            Title = title,
            Authors = authors,
            Journal = journal,
            Publisher = NullIfWhiteSpace(reference.Publisher),
            PublicationYear = year,
            Volume = NullIfWhiteSpace(reference.Volume),
            Issue = NullIfWhiteSpace(reference.Issue),
            Pages = pages,
            RawText = reference.RawText,
            Doi = normalizedDoi,
            DoiSource = reference.DoiSource,
            DoiConfidence = reference.DoiConfidence,
            IssueCodes = reference.IssueCodes,
            WarningMessages = reference.WarningMessages
        };

        return new ReferenceNormalizationResult
        {
            Reference = normalized,
            ParseConfidenceScore = CalculateParseConfidence(normalized, issues),
            IssueCodes = issues.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            WarningMessages = warnings
        };
    }

    public string? NormalizeDoi(string? doi)
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

        return IsValidDoiFormat(value) ? value : NullIfWhiteSpace(value);
    }

    public bool IsValidDoiFormat(string? doi)
    {
        var value = doi?.Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        value = value
            .Replace("https://doi.org/", "", StringComparison.OrdinalIgnoreCase)
            .Replace("http://doi.org/", "", StringComparison.OrdinalIgnoreCase)
            .Replace("https://dx.doi.org/", "", StringComparison.OrdinalIgnoreCase)
            .Replace("http://dx.doi.org/", "", StringComparison.OrdinalIgnoreCase)
            .Replace("doi:", "", StringComparison.OrdinalIgnoreCase)
            .Trim();

        return DoiRegex().IsMatch(value);
    }

    public static string? CleanTitle(
        string? title,
        string? rawText = null,
        string? journal = null,
        string? doi = null,
        int? year = null,
        string? volume = null,
        string? issue = null,
        string? pages = null)
    {
        var value = NullIfWhiteSpace(title);
        if (string.IsNullOrWhiteSpace(value) || LooksLikeWholeReference(value))
        {
            value = ExtractLikelyTitle(rawText) ?? value;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        value = DoiInTextRegex().Replace(value, " ");
        if (!string.IsNullOrWhiteSpace(doi))
        {
            value = value.Replace(doi, " ", StringComparison.OrdinalIgnoreCase);
        }

        foreach (var token in new[] { volume, issue, pages, year?.ToString() })
        {
            if (!string.IsNullOrWhiteSpace(token))
            {
                value = RemoveToken(value, token);
            }
        }

        if (!IsSameComparableText(journal, value) && !string.IsNullOrWhiteSpace(journal))
        {
            value = RemoveToken(value, journal);
        }

        value = LeadingAuthorListRegex().Replace(value, " ");
        value = TrailingNumericNoiseRegex().Replace(value, " ");
        value = LeadingPunctuationRegex().Replace(value, " ");
        value = OrphanPunctuationRegex().Replace(value, " ");
        value = NormalizeSpaces(value);
        return NullIfWhiteSpace(value);
    }

    public static IReadOnlyList<AuthorDto> ExtractAuthors(string? rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return Array.Empty<AuthorDto>();
        }

        var prefix = rawText;
        var dashIndex = prefix.IndexOf(" -", StringComparison.Ordinal);
        if (dashIndex > 0)
        {
            prefix = prefix[..dashIndex];
        }
        else
        {
            var yearMatch = YearRegex().Match(prefix);
            if (yearMatch.Success && yearMatch.Index > 0)
            {
                prefix = prefix[..yearMatch.Index];
            }
        }

        prefix = NormalizeSpaces(prefix) ?? string.Empty;
        if (prefix.Length > 250)
        {
            return Array.Empty<AuthorDto>();
        }

        var authors = ExtractCompactAuthors(prefix).ToList();
        if (authors.Count > 0)
        {
            return authors;
        }

        var parts = prefix.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 1 && prefix.Contains(" and ", StringComparison.OrdinalIgnoreCase))
        {
            parts = prefix.Split([" and "], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        foreach (var part in parts)
        {
            var cleaned = NormalizeSpaces(part);
            if (IsPlausibleAuthorName(cleaned))
            {
                authors.Add(new AuthorDto { FullName = NormalizeAuthorCasing(cleaned!) });
            }
        }

        return authors;
    }

    public static bool LooksLikeAuthorFragmentTitle(string? title)
    {
        var value = NormalizeSpaces(title);
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (KnownVenueTerms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        var tokens = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length > 10)
        {
            return false;
        }

        if (CompactAuthorRegex().Matches(value).Count >= 1
            && !TitleLikeAuthorTerms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (tokens.Length is >= 2 and <= 6
            && tokens.All(token => InitialTokenRegex().IsMatch(token) || SurnameParticles.Contains(token) || SurnameTokenRegex().IsMatch(token))
            && tokens.Any(token => InitialTokenRegex().IsMatch(token)))
        {
            return true;
        }

        return false;
    }

    public static string? NormalizeJournal(string? journal, string? title, string? rawText)
    {
        var value = NullIfWhiteSpace(journal);
        if (IsJournalSuspect(value, title))
        {
            value = null;
        }

        if (string.IsNullOrWhiteSpace(value) && !string.IsNullOrWhiteSpace(rawText))
        {
            value = KnownVenueTerms.FirstOrDefault(term => rawText.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        return NullIfWhiteSpace(value);
    }

    private static string? ExtractDoi(string? rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return null;
        }

        var match = DoiInTextRegex().Match(rawText);
        return match.Success ? match.Groups["doi"].Value : null;
    }

    private static int? ExtractYear(string? rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return null;
        }

        foreach (Match match in YearRegex().Matches(rawText))
        {
            if (IsInsideDoi(rawText, match.Index))
            {
                continue;
            }

            if (int.TryParse(match.Value, out var year)
                && year >= 1900
                && year <= DateTime.UtcNow.Year + 1)
            {
                return year;
            }
        }

        return null;
    }

    private static string? ExtractLikelyTitle(string? rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return null;
        }

        var value = NormalizeSpaces(rawText) ?? rawText;
        var dashIndex = value.IndexOf(" -", StringComparison.Ordinal);
        if (dashIndex >= 0 && dashIndex + 2 < value.Length)
        {
            value = value[(dashIndex + 2)..];
        }

        var yearMatch = YearRegex().Match(value);
        if (yearMatch.Success && yearMatch.Index > 20)
        {
            value = value[..yearMatch.Index];
        }

        value = DoiInTextRegex().Replace(value, " ");
        return NormalizeSpaces(value);
    }

    private static bool LooksLikeWholeReference(string value) =>
        value.Length > 180
        || DoiInTextRegex().IsMatch(value)
        || TrailingNumericNoiseRegex().IsMatch(value)
        || LeadingAuthorListRegex().IsMatch(value);

    private static string? NormalizePages(string? pages, string? rawText, string? doi, List<string> issues)
    {
        var value = NullIfWhiteSpace(pages);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        value = value.Trim().Trim(',', ';', '.');
        var looksLikeDoiFragment = !string.IsNullOrWhiteSpace(doi)
            && doi.Contains(value, StringComparison.OrdinalIgnoreCase)
            && !StandaloneArticleNumberRegex().IsMatch(value);
        if (looksLikeDoiFragment || DoiSuffixPageFragmentRegex().IsMatch(value))
        {
            issues.Add("REFERENCE_PAGES_SUSPECT");
            return null;
        }

        if (SafePagesRegex().IsMatch(value))
        {
            return value;
        }

        issues.Add("REFERENCE_PAGES_SUSPECT");
        return null;
    }

    private static bool IsTitlePolluted(string? originalTitle, string? normalizedTitle, string? rawText) =>
        !string.IsNullOrWhiteSpace(originalTitle)
        && !string.Equals(originalTitle, normalizedTitle, StringComparison.Ordinal)
        && (originalTitle.Length > 180
            || DoiInTextRegex().IsMatch(originalTitle)
            || TrailingNumericNoiseRegex().IsMatch(originalTitle)
            || (!string.IsNullOrWhiteSpace(rawText) && originalTitle.Length > (normalizedTitle?.Length ?? 0) + 30));

    private static bool IsJournalSuspect(string? journal, string? title)
    {
        if (string.IsNullOrWhiteSpace(journal) || string.IsNullOrWhiteSpace(title))
        {
            return false;
        }

        var normalizedJournal = NormalizeComparable(journal);
        var normalizedTitle = NormalizeComparable(title);
        if (normalizedJournal.Length == 0 || normalizedTitle.Length == 0)
        {
            return false;
        }

        return normalizedJournal == normalizedTitle
            || normalizedJournal.Contains(normalizedTitle, StringComparison.OrdinalIgnoreCase)
            || normalizedTitle.Contains(normalizedJournal, StringComparison.OrdinalIgnoreCase)
            || CalculateTokenOverlap(normalizedJournal, normalizedTitle) >= 0.75;
    }

    private static bool IsSameComparableText(string? left, string? right) =>
        !string.IsNullOrWhiteSpace(left)
        && !string.IsNullOrWhiteSpace(right)
        && NormalizeComparable(left) == NormalizeComparable(right);

    private static double CalculateTokenOverlap(string left, string right)
    {
        var leftTokens = left.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var rightTokens = right.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (leftTokens.Count == 0 || rightTokens.Count == 0)
        {
            return 0;
        }

        var denominator = Math.Min(leftTokens.Count, rightTokens.Count);
        leftTokens.IntersectWith(rightTokens);
        return leftTokens.Count * 1.0 / denominator;
    }

    private static string NormalizeComparable(string value) =>
        string.Join(" ", value.ToLowerInvariant().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static int CalculateAuthorConfidence(string? rawText, IReadOnlyList<AuthorDto> authors)
    {
        if (authors.Count == 0)
        {
            return 0;
        }

        if (authors.Count >= 2)
        {
            return IsSentenceLikeAuthorPrefix(rawText) ? 50 : 80;
        }

        var authorName = authors[0].FullName;
        if (!string.IsNullOrWhiteSpace(rawText)
            && !string.IsNullOrWhiteSpace(authorName)
            && rawText.StartsWith(authorName.Replace(" ", "", StringComparison.Ordinal).Replace(".", "", StringComparison.Ordinal), StringComparison.OrdinalIgnoreCase))
        {
            return 75;
        }

        return 65;
    }

    private static bool IsSentenceLikeAuthorPrefix(string? rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return false;
        }

        var prefix = rawText.Split(['-', '.'], StringSplitOptions.TrimEntries).FirstOrDefault() ?? rawText;
        return prefix.Contains(" but ", StringComparison.OrdinalIgnoreCase)
            || prefix.Contains(" also ", StringComparison.OrdinalIgnoreCase)
            || prefix.Contains(" possibly ", StringComparison.OrdinalIgnoreCase)
            || prefix.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length > 12;
    }

    private static double CalculateParseConfidence(ReferenceDto reference, IReadOnlyList<string> issues)
    {
        var score = 100;
        score -= issues.Count(issue => issue is "REFERENCE_TITLE_POLLUTED" or "REFERENCE_JOURNAL_SUSPECT" or "REFERENCE_PAGES_SUSPECT" or "REFERENCE_PARSE_SUSPECT") * 15;
        score -= issues.Count(issue => issue is "REFERENCE_MISSING_AUTHOR" or "REFERENCE_AUTHOR_LOW_CONFIDENCE" or "REFERENCE_YEAR_MISSING") * 20;
        score -= issues.Count(issue => issue is "REFERENCE_TITLE_PARSE_FAILED") * 25;
        if (string.IsNullOrWhiteSpace(reference.Title))
        {
            score -= 30;
        }

        return Math.Clamp(score, 0, 100);
    }

    private static bool IsInsideDoi(string rawText, int index)
    {
        foreach (Match doiMatch in DoiInTextRegex().Matches(rawText))
        {
            if (index >= doiMatch.Index && index < doiMatch.Index + doiMatch.Length)
            {
                return true;
            }
        }

        return false;
    }

    private static string RemoveToken(string value, string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return value;
        }

        return value.Replace(token, " ", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPlausibleAuthorName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (TitleLikeAuthorTerms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase))
            || KnownVenueTerms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        var words = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return words is { Length: >= 2 and <= 5 }
            && words.All(word => word.Any(char.IsLetter))
            && words.Any(word => SurnameParticles.Contains(word) || word.Length > 1);
    }

    private static IReadOnlyList<AuthorDto> ExtractCompactAuthors(string prefix)
    {
        var matches = CompactAuthorRegex().Matches(prefix);
        if (matches.Count == 0)
        {
            return Array.Empty<AuthorDto>();
        }

        var authors = new List<AuthorDto>();
        foreach (Match match in matches)
        {
            var author = CreateCompactAuthor(match);
            if (author is not null
                && authors.All(x => !string.Equals(x.FullName, author.FullName, StringComparison.OrdinalIgnoreCase)))
            {
                authors.Add(author);
            }
        }

        return authors;
    }

    private static AuthorDto? CreateCompactAuthor(Match match)
    {
        var initials = match.Groups["initials"].Value;
        var surname = NormalizeAuthorCasing(match.Groups["surname"].Value);
        if (string.IsNullOrWhiteSpace(initials) || string.IsNullOrWhiteSpace(surname))
        {
            return null;
        }

        var formattedInitials = FormatInitials(initials);
        var fullName = $"{formattedInitials} {surname}".Trim();
        return new AuthorDto
        {
            FullName = fullName,
            LastName = surname
        };
    }

    private static string FormatInitials(string initials)
    {
        var letters = initials.Where(char.IsLetter).Select(char.ToUpperInvariant).ToArray();
        if (letters.Length == 0)
        {
            return initials;
        }

        if (initials.Contains('-'))
        {
            return $"{letters[0]}.-{letters[^1]}.";
        }

        return string.Join(" ", letters.Select(letter => $"{letter}."));
    }

    private static string NormalizeAuthorCasing(string value)
    {
        var words = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < words.Length; i++)
        {
            if (SurnameParticles.Contains(words[i]))
            {
                words[i] = words[i].ToLowerInvariant();
            }
        }

        return string.Join(" ", words);
    }

    private static string? NormalizeSpaces(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return NullIfWhiteSpace(string.Join(" ", value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)));
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    [GeneratedRegex(@"(?<doi>10\.\d{4,9}/[-._;()/:A-Z0-9]+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DoiRegex();

    [GeneratedRegex(@"(?:https?://(?:dx\.)?doi\.org/|doi:\s*)?(?<doi>10\.\d{4,9}/[-._;()/:A-Z0-9]+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DoiInTextRegex();

    [GeneratedRegex(@"\b(?:19|20)\d{2}\b", RegexOptions.CultureInvariant)]
    private static partial Regex YearRegex();

    [GeneratedRegex(@"\b(?:(?<initials>[A-Z]\.-?[A-Z]\.?)(?<surname>[A-Z][a-z]{2,})|(?<initials>[A-Z]{2})(?<surname>[A-Z][a-z]{2,})|(?<initials>[A-Z])(?<surname>(?:Da|De|Del|Di|Van|Von|Der|Den|La|Le|Dos|Das)\s+[A-Z][A-Za-z]+|[A-Z][a-z]{2,}))\b", RegexOptions.CultureInvariant)]
    private static partial Regex CompactAuthorRegex();

    [GeneratedRegex(@"^(?:[A-Z]\.|[A-Z]\.-[A-Z]\.|[A-Z])$", RegexOptions.CultureInvariant)]
    private static partial Regex InitialTokenRegex();

    [GeneratedRegex(@"^[A-Z][A-Za-z]{2,}$", RegexOptions.CultureInvariant)]
    private static partial Regex SurnameTokenRegex();

    [GeneratedRegex(@"^(?:[A-Z]\.?\s*[A-Z][a-z]+\s*){2,}", RegexOptions.CultureInvariant)]
    private static partial Regex LeadingAuthorListRegex();

    [GeneratedRegex(@"\b(?:19|20)\d{2}\b.*\b\d{1,4}\b.*\b\d{1,6}\b", RegexOptions.CultureInvariant)]
    private static partial Regex TrailingNumericNoiseRegex();

    [GeneratedRegex(@"^[\s\-–—:;,.]+", RegexOptions.CultureInvariant)]
    private static partial Regex LeadingPunctuationRegex();

    [GeneratedRegex(@"\s+[.]\s*(?:[.]\s*)*$", RegexOptions.CultureInvariant)]
    private static partial Regex OrphanPunctuationRegex();

    [GeneratedRegex(@"^\d{1,6}(?:-\d{1,6})?$", RegexOptions.CultureInvariant)]
    private static partial Regex SafePagesRegex();

    [GeneratedRegex(@"^\d{5,6}$", RegexOptions.CultureInvariant)]
    private static partial Regex StandaloneArticleNumberRegex();

    [GeneratedRegex(@"^[A-Z0-9._;()/:/-]*[A-Z][A-Z0-9._;()/:/-]*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DoiSuffixPageFragmentRegex();
}
