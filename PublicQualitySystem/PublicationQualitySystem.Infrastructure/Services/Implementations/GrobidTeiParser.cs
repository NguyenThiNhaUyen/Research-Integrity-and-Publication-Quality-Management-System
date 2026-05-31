using System.Text.RegularExpressions;
using System.Xml.Linq;
using PublicationQualitySystem.Application.DTOs.Grobid;
using PublicationQualitySystem.Shared.Exceptions;

namespace PublicationQualitySystem.Infrastructure.Services.Implementations;

public static partial class GrobidTeiParser
{
    private static readonly XNamespace Tei = "http://www.tei-c.org/ns/1.0";

    public static GrobidMetadataResponse Parse(string xml, ILogger? logger = null)
    {
        var document = XDocument.Parse(xml, LoadOptions.PreserveWhitespace);
        var tei = document.Root ?? throw new AppException(GrobidErrorCode.InvalidResponse);
        var fileDesc = tei.Descendants(Tei + "fileDesc").FirstOrDefault();
        var sourceDesc = fileDesc?.Element(Tei + "sourceDesc");
        var biblStruct = sourceDesc?.Descendants(Tei + "biblStruct").FirstOrDefault();
        var analytic = biblStruct?.Element(Tei + "analytic");
        var monogr = biblStruct?.Element(Tei + "monogr");
        var allText = CleanText(tei);

        var rawDoi = CleanText(analytic?.Elements(Tei + "idno").FirstOrDefault(x => AttrEquals(x, "type", "DOI")))
            ?? CleanText(biblStruct?.Descendants(Tei + "idno").FirstOrDefault(x => AttrEquals(x, "type", "DOI")));
        var doi = NormalizeDoi(rawDoi, logger);
        var venueInfo = ExtractVenueInfo(fileDesc, monogr, allText);
        var references = tei.Descendants(Tei + "listBibl")
            .Descendants(Tei + "biblStruct")
            .Select(x => ParseReference(x, logger))
            .ToArray();

        return new GrobidMetadataResponse
        {
            Title = CleanText(analytic?.Elements(Tei + "title").FirstOrDefault(x => AttrEquals(x, "level", "a")))
                ?? CleanText(fileDesc?.Descendants(Tei + "titleStmt").Elements(Tei + "title").FirstOrDefault())
                ?? CleanText(analytic?.Element(Tei + "title")),
            Authors = ParseAuthors(analytic?.Elements(Tei + "author") ?? Enumerable.Empty<XElement>(), logger),
            Abstract = CleanText(tei.Descendants(Tei + "profileDesc").Descendants(Tei + "abstract").FirstOrDefault()),
            Doi = doi,
            ArxivId = NormalizeArxivId(CleanText(biblStruct?.Descendants(Tei + "idno").FirstOrDefault(x => AttrEquals(x, "type", "arXiv")))),
            Journal = CleanText(monogr?.Elements(Tei + "title").FirstOrDefault(x => AttrEquals(x, "level", "j")))
                ?? CleanText(monogr?.Element(Tei + "title")),
            Publisher = venueInfo.Publisher,
            Venue = venueInfo.Venue,
            ConferenceName = venueInfo.ConferenceName,
            PublicationYear = ParseYear(monogr?.Descendants(Tei + "date").FirstOrDefault()),
            Keywords = ParseKeywords(tei),
            References = references
        };
    }

    private static ReferenceDto ParseReference(XElement biblStruct, ILogger? logger)
    {
        var analytic = biblStruct.Element(Tei + "analytic");
        var monogr = biblStruct.Element(Tei + "monogr");
        var rawText = CleanText(biblStruct);
        var title = CleanText(analytic?.Elements(Tei + "title").FirstOrDefault(x => AttrEquals(x, "level", "a")))
            ?? CleanText(analytic?.Element(Tei + "title"))
            ?? rawText;

        return new ReferenceDto
        {
            Title = title,
            Authors = ParseAuthors(analytic?.Elements(Tei + "author") ?? Enumerable.Empty<XElement>(), logger),
            Journal = CleanText(monogr?.Elements(Tei + "title").FirstOrDefault(x => AttrEquals(x, "level", "j")))
                ?? CleanText(monogr?.Element(Tei + "title")),
            Publisher = CleanText(monogr?.Descendants(Tei + "publisher").FirstOrDefault()),
            Doi = NormalizeDoi(CleanText(biblStruct.Descendants(Tei + "idno").FirstOrDefault(x => AttrEquals(x, "type", "DOI"))), logger),
            PublicationYear = ParseYear(monogr?.Descendants(Tei + "date").FirstOrDefault()),
            RawText = rawText
        };
    }

    private static IReadOnlyList<AuthorDto> ParseAuthors(IEnumerable<XElement> authorNodes, ILogger? logger)
    {
        var nodes = authorNodes.ToArray();
        var affiliations = nodes
            .SelectMany(x => x.Elements(Tei + "affiliation"))
            .Select(FormatAffiliation)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var authors = new List<AuthorDto>();
        foreach (var author in nodes)
        {
            var persName = author.Element(Tei + "persName");
            if (persName is null)
            {
                if (author.Element(Tei + "affiliation") is not null)
                {
                    logger?.LogInformation("Skipped affiliation-only author node while parsing GROBID TEI.");
                }

                continue;
            }

            var dto = ParsePersonAuthor(author, persName);
            if (string.IsNullOrWhiteSpace(dto.Affiliation) && affiliations.Length > 0)
            {
                dto.Affiliation = affiliations.Length == 1
                    ? affiliations[0]
                    : affiliations.ElementAtOrDefault(authors.Count);
            }

            if (!string.IsNullOrWhiteSpace(dto.FullName))
            {
                authors.Add(dto);
            }
        }

        return authors;
    }

    private static AuthorDto ParsePersonAuthor(XElement author, XElement persName)
    {
        var firstName = CleanText(persName.Elements(Tei + "forename").FirstOrDefault(x => AttrEquals(x, "type", "first")))
            ?? CleanText(persName.Elements(Tei + "forename").FirstOrDefault());
        var middleName = CleanText(persName.Elements(Tei + "forename").FirstOrDefault(x => AttrEquals(x, "type", "middle")));
        var lastName = CleanText(persName.Element(Tei + "surname"));
        var fullName = JoinNonEmpty(firstName, middleName, lastName) ?? CleanText(persName);

        return new AuthorDto
        {
            FirstName = firstName,
            MiddleName = middleName,
            LastName = lastName,
            FullName = fullName,
            Email = CleanText(author.Descendants(Tei + "email").FirstOrDefault()),
            Affiliation = FormatAffiliation(author.Elements(Tei + "affiliation").FirstOrDefault())
        };
    }

    private static IReadOnlyList<string> ParseKeywords(XElement tei)
    {
        var keywords = new List<string>();
        foreach (var term in tei.Descendants(Tei + "textClass")
            .Descendants(Tei + "keywords")
            .Descendants(Tei + "term")
            .Select(CleanText)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!))
        {
            foreach (var part in term.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                foreach (var keywordPart in AcronymKeywordBoundaryRegex().Split(part))
                {
                    var cleaned = NormalizeSpaces(keywordPart);
                    if (!string.IsNullOrWhiteSpace(cleaned)
                        && !keywords.Contains(cleaned, StringComparer.OrdinalIgnoreCase))
                    {
                        keywords.Add(cleaned);
                    }
                }
            }
        }

        return keywords;
    }

    private static VenueInfo ExtractVenueInfo(XElement? fileDesc, XElement? monogr, string? allText)
    {
        var publisher = CleanText(fileDesc?.Descendants(Tei + "publicationStmt").Descendants(Tei + "publisher").FirstOrDefault())
            ?? CleanText(monogr?.Descendants(Tei + "publisher").FirstOrDefault());
        var venue = CleanText(monogr?.Elements(Tei + "title").FirstOrDefault(x => AttrEquals(x, "level", "m")))
            ?? CleanText(monogr?.Descendants(Tei + "meeting").FirstOrDefault());
        string? conferenceName = null;

        if (!string.IsNullOrWhiteSpace(allText))
        {
            var acmMatch = AcmProceedingsRegex().Match(allText);
            if (acmMatch.Success)
            {
                conferenceName = NormalizeSpaces(acmMatch.Groups["conference"].Value);
                var matchedVenue = NormalizeSpaces(acmMatch.Groups["venue"].Value);
                if (string.IsNullOrWhiteSpace(venue)
                    || venue.Contains("Proceedings", StringComparison.OrdinalIgnoreCase)
                    || venue.Contains("Conference", StringComparison.OrdinalIgnoreCase))
                {
                    venue = matchedVenue;
                }
            }

            if (string.IsNullOrWhiteSpace(publisher) && AcmPublisherRegex().IsMatch(allText))
            {
                publisher = "ACM";
            }
        }

        return new VenueInfo(
            NullIfWhiteSpace(publisher),
            NullIfWhiteSpace(venue),
            NullIfWhiteSpace(conferenceName));
    }

    private static string? FormatAffiliation(XElement? affiliation)
    {
        if (affiliation is null)
        {
            return null;
        }

        var organizations = affiliation.Elements(Tei + "orgName")
            .Select(CleanText)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var address = affiliation.Element(Tei + "address");
        var addressParts = address?.Elements()
            .Where(x => x.Name == Tei + "addrLine" || x.Name == Tei + "settlement" || x.Name == Tei + "region" || x.Name == Tei + "country")
            .Select(CleanText)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .ToArray() ?? Array.Empty<string>();

        var parts = organizations.Concat(addressParts).ToArray();
        return parts.Length > 0 ? string.Join(", ", parts) : CleanText(affiliation);
    }

    private static string? NormalizeDoi(string? rawDoi, ILogger? logger)
    {
        if (string.IsNullOrWhiteSpace(rawDoi))
        {
            return null;
        }

        var doi = rawDoi.Trim()
            .Replace("https://doi.org/", "", StringComparison.OrdinalIgnoreCase)
            .Replace("http://doi.org/", "", StringComparison.OrdinalIgnoreCase)
            .Replace("doi:", "", StringComparison.OrdinalIgnoreCase)
            .Trim()
            .TrimEnd('.', ',', ';');

        if (doi.StartsWith("10.", StringComparison.OrdinalIgnoreCase))
        {
            return doi;
        }

        logger?.LogInformation("Invalid DOI detected and ignored. RawDoi={RawDoi}", rawDoi);
        return null;
    }

    private static string? NormalizeArxivId(string? rawArxiv)
    {
        if (string.IsNullOrWhiteSpace(rawArxiv))
        {
            return null;
        }

        var value = rawArxiv.Trim()
            .Replace("arXiv:", "", StringComparison.OrdinalIgnoreCase)
            .Trim();
        var bracketIndex = value.IndexOf('[', StringComparison.Ordinal);
        if (bracketIndex >= 0)
        {
            value = value[..bracketIndex];
        }

        return NullIfWhiteSpace(value);
    }

    private static bool AttrEquals(XElement element, string name, string value) =>
        string.Equals((string?)element.Attribute(name), value, StringComparison.OrdinalIgnoreCase);

    private static int? ParseYear(XElement? date)
    {
        var value = (string?)date?.Attribute("when") ?? CleanText(date);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var yearText = value.Length >= 4 ? value[..4] : value;
        return int.TryParse(yearText, out var year) ? year : null;
    }

    private static string? CleanText(XElement? element)
    {
        if (element is null)
        {
            return null;
        }

        return NormalizeSpaces(element.Value);
    }

    private static string? NormalizeSpaces(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var text = string.Join(" ", value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return NullIfWhiteSpace(text);
    }

    private static string? JoinNonEmpty(params string?[] parts)
    {
        var joined = string.Join(" ", parts.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!.Trim()));
        return NullIfWhiteSpace(joined);
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed record VenueInfo(string? Publisher, string? Venue, string? ConferenceName);

    [GeneratedRegex(@"(?:In\s+)?(?:Proceedings\s+of\s+the\s+)?(?<conference>\d{1,2}(?:st|nd|rd|th)\s+ACM\s+International\s+Conference\s+on\s+[^()]+?)\s*\((?<venue>[^)]+)\)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AcmProceedingsRegex();

    [GeneratedRegex(@"\b(ACM|Association for Computing Machinery|ACM Reference Format)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AcmPublisherRegex();

    [GeneratedRegex(@"(?<=[a-z])\s+(?=[A-Z]{2,}\b)", RegexOptions.CultureInvariant)]
    private static partial Regex AcronymKeywordBoundaryRegex();
}
