using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using PublicationQualitySystem.Infrastructure.Services.Implementations;

namespace PublicationQualitySystem.Infrastructure.Tests;

public class CrossrefServiceTests
{
    [Fact]
    public async Task GetWorkByDoiAsync_ParsesAuthorOrcidAndPublishedDate()
    {
        var service = new CrossrefService(CreateClient("""
            {
              "message": {
                "title": ["Paper"],
                "DOI": "10.1000/example",
                "published-online": { "date-parts": [[2024, 5, 6]] },
                "author": [
                  {
                    "given": "Ada",
                    "family": "Lovelace",
                    "ORCID": "https://orcid.org/0000-0002-1825-0097"
                  }
                ]
              }
            }
            """), NullLogger<CrossrefService>.Instance);

        var result = await service.GetWorkByDoiAsync("10.1000/example", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(new DateOnly(2024, 5, 6), result.PublishedDate);
        Assert.Equal("0000-0002-1825-0097", Assert.Single(result.Authors).Orcid);
    }

    [Fact]
    public async Task SearchWorksByTitleAsync_ReturnsCandidates()
    {
        var service = new CrossrefService(CreateClient("""
            {
              "message": {
                "items": [
                  {
                    "title": ["Let's wait awhile"],
                    "DOI": "10.1145/example",
                    "issued": { "date-parts": [[2021]] }
                  }
                ]
              }
            }
            """), NullLogger<CrossrefService>.Instance);

        var results = await service.SearchWorksByTitleAsync("Let's wait awhile", 5, CancellationToken.None);

        var result = Assert.Single(results);
        Assert.Equal("Let's wait awhile", result.Title);
        Assert.Equal("10.1145/example", result.Doi);
        Assert.Equal(2021, result.PublicationYear);
    }

    private static HttpClient CreateClient(string json) =>
        new(new StubHandler(json))
        {
            BaseAddress = new Uri("https://api.crossref.org")
        };

    private sealed class StubHandler(string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            });
        }
    }
}
