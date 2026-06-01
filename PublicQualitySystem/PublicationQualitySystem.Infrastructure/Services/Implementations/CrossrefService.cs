using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using PublicationQualitySystem.Application.DTOs.Crossref;
using PublicationQualitySystem.Application.DTOs.Grobid;
using PublicationQualitySystem.Application.Services.Interfaces;

namespace PublicationQualitySystem.Infrastructure.Services.Implementations;

public sealed class CrossrefService(HttpClient httpClient, ILogger<CrossrefService> logger) : ICrossrefService
{
    public async Task<CrossrefMetadataResponse?> GetWorkByDoiAsync(
        string doi,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var encodedDoi = Uri.EscapeDataString(doi);

        logger.LogInformation("Crossref lookup started. Doi={Doi}", doi);

        HttpResponseMessage response;
        try
        {
            response = await httpClient.GetAsync($"/works/{encodedDoi}", cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(
                ex,
                "Crossref lookup failed. Doi={Doi}, ElapsedMs={ElapsedMs}",
                doi,
                stopwatch.ElapsedMilliseconds);
            return null;
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            logger.LogInformation(
                "Crossref lookup returned not found. Doi={Doi}, ElapsedMs={ElapsedMs}",
                doi,
                stopwatch.ElapsedMilliseconds);
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "Crossref lookup returned non-success status. Doi={Doi}, StatusCode={StatusCode}, ElapsedMs={ElapsedMs}",
                doi,
                (int)response.StatusCode,
                stopwatch.ElapsedMilliseconds);
            return null;
        }

        var payload = await response.Content.ReadFromJsonAsync<CrossrefEnvelope>(cancellationToken: cancellationToken);
        var message = payload?.Message;
        if (message is null)
        {
            logger.LogWarning("Crossref lookup response is empty. Doi={Doi}", doi);
            return null;
        }

        var result = new CrossrefMetadataResponse
        {
            Title = First(message.Title),
            Abstract = StripMarkup(message.Abstract),
            Doi = message.Doi,
            Journal = First(message.ContainerTitle) ?? First(message.ShortContainerTitle),
            Publisher = message.Publisher,
            PublicationYear = ParseYear(message),
            Volume = message.Volume,
            Issue = message.Issue,
            Pages = message.Page,
            Authors = message.Author?
                .Select(ToAuthor)
                .Where(x => !string.IsNullOrWhiteSpace(x.FullName))
                .ToArray() ?? Array.Empty<AuthorDto>()
        };

        logger.LogInformation(
            "Crossref lookup success. Doi={Doi}, HasJournal={HasJournal}, HasPublisher={HasPublisher}, HasYear={HasYear}, AuthorCount={AuthorCount}, ElapsedMs={ElapsedMs}",
            doi,
            !string.IsNullOrWhiteSpace(result.Journal),
            !string.IsNullOrWhiteSpace(result.Publisher),
            result.PublicationYear.HasValue,
            result.Authors.Count,
            stopwatch.ElapsedMilliseconds);

        return result;
    }

    private static AuthorDto ToAuthor(CrossrefAuthor author)
    {
        var given = NullIfWhiteSpace(author.Given);
        var family = NullIfWhiteSpace(author.Family);
        var fullName = NullIfWhiteSpace(author.Name) ?? JoinNonEmpty(given, family);

        return new AuthorDto
        {
            FirstName = given,
            LastName = family,
            FullName = fullName,
            Affiliation = author.Affiliation is { Count: > 0 }
                ? string.Join("; ", author.Affiliation.Select(x => x.Name).Where(x => !string.IsNullOrWhiteSpace(x)))
                : null
        };
    }

    private static int? ParseYear(CrossrefMessage message)
    {
        return FirstYear(message.Issued)
            ?? FirstYear(message.PublishedPrint)
            ?? FirstYear(message.PublishedOnline)
            ?? FirstYear(message.Published);
    }

    private static int? FirstYear(CrossrefDateParts? dateParts)
    {
        var year = dateParts?.DateParts?.FirstOrDefault()?.FirstOrDefault();
        return year is > 0 ? year : null;
    }

    private static string? First(IReadOnlyList<string>? values) =>
        values?.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

    private static string? JoinNonEmpty(params string?[] values)
    {
        var joined = string.Join(" ", values.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!.Trim()));
        return NullIfWhiteSpace(joined);
    }

    private static string? StripMarkup(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return System.Text.RegularExpressions.Regex.Replace(value, "<.*?>", string.Empty).Trim();
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed class CrossrefEnvelope
    {
        [JsonPropertyName("message")]
        public CrossrefMessage? Message { get; set; }
    }

    private sealed class CrossrefMessage
    {
        [JsonPropertyName("title")]
        public IReadOnlyList<string>? Title { get; set; }

        [JsonPropertyName("abstract")]
        public string? Abstract { get; set; }

        [JsonPropertyName("DOI")]
        public string? Doi { get; set; }

        [JsonPropertyName("container-title")]
        public IReadOnlyList<string>? ContainerTitle { get; set; }

        [JsonPropertyName("short-container-title")]
        public IReadOnlyList<string>? ShortContainerTitle { get; set; }

        [JsonPropertyName("publisher")]
        public string? Publisher { get; set; }

        [JsonPropertyName("issued")]
        public CrossrefDateParts? Issued { get; set; }

        [JsonPropertyName("published-print")]
        public CrossrefDateParts? PublishedPrint { get; set; }

        [JsonPropertyName("published-online")]
        public CrossrefDateParts? PublishedOnline { get; set; }

        [JsonPropertyName("published")]
        public CrossrefDateParts? Published { get; set; }

        [JsonPropertyName("volume")]
        public string? Volume { get; set; }

        [JsonPropertyName("issue")]
        public string? Issue { get; set; }

        [JsonPropertyName("page")]
        public string? Page { get; set; }

        [JsonPropertyName("author")]
        public IReadOnlyList<CrossrefAuthor>? Author { get; set; }
    }

    private sealed class CrossrefDateParts
    {
        [JsonPropertyName("date-parts")]
        public IReadOnlyList<IReadOnlyList<int>>? DateParts { get; set; }
    }

    private sealed class CrossrefAuthor
    {
        [JsonPropertyName("given")]
        public string? Given { get; set; }

        [JsonPropertyName("family")]
        public string? Family { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("affiliation")]
        public IReadOnlyList<CrossrefAffiliation>? Affiliation { get; set; }
    }

    private sealed class CrossrefAffiliation
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}
