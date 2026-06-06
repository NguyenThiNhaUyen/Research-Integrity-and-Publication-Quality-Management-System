using System.Net;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PublicationQualitySystem.Application.DTOs.Grobid;
using PublicationQualitySystem.Application.DTOs.OpenAlex;
using PublicationQualitySystem.Application.Repositories.Interfaces;
using PublicationQualitySystem.Application.Services.Interfaces;
using PublicationQualitySystem.Domain.Entities;
using PublicationQualitySystem.Infrastructure.Options;
using PublicationQualitySystem.Infrastructure.Security;
using PublicationQualitySystem.Infrastructure.Services.Implementations;
using System.Security.Claims;

namespace PublicationQualitySystem.Infrastructure.Tests;

public class OpenAlexServiceTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void ReconstructAbstract_BuildsTextFromInvertedIndex()
    {
        const string json = """
        {
          "abstract_inverted_index": {
            "Edge": [0],
            "computing": [1],
            "saves": [2],
            "energy": [3]
          }
        }
        """;
        using var document = JsonDocument.Parse(json);

        var text = OpenAlexService.ReconstructAbstract(document.RootElement);

        Assert.Equal("Edge computing saves energy", text);
    }

    [Fact]
    public void CalculateSimilarity_UsesWeightedScoresAndHighRiskForStrongMatch()
    {
        var metadata = new PaperMetadata
        {
            PaperId = 1,
            Title = "Edge Computing for Sustainable Wireless Networks",
            Abstract = "Edge computing improves energy efficiency in wireless networks.",
            AuthorsJson = JsonSerializer.Serialize(new[] { new AuthorDto { FullName = "Ada Lovelace" } }, JsonOptions),
            ReferencesJson = JsonSerializer.Serialize(new[]
            {
                new ReferenceDto { Doi = "10.1000/ref1" }
            }, JsonOptions)
        };
        var work = new Application.DTOs.OpenAlex.OpenAlexWorkDto
        {
            Id = "https://openalex.org/W123",
            Doi = "10.1000/example",
            Title = "Edge Computing for Sustainable Wireless Networks",
            Abstract = "Edge computing improves energy efficiency in wireless networks.",
            Authors = new[] { new AuthorDto { FullName = "Ada Lovelace" } },
            ReferencedWorks = new[] { "https://doi.org/10.1000/ref1" },
            RawJson = "{}"
        };

        var result = OpenAlexService.CalculateSimilarity(metadata, work);

        Assert.True(result.OverallScore >= 90);
        Assert.Equal(Domain.Enums.SimilarityRiskLevel.HIGH, result.RiskLevel);
    }

    [Fact]
    public void CalculateSimilarity_MatchesAuthorsWithDiacriticsInitialsAndDifferentOrder()
    {
        var metadata = new PaperMetadata
        {
            PaperId = 1,
            Title = "LESS-ON",
            Abstract = "Edge computing saves energy.",
            AuthorsJson = JsonSerializer.Serialize(new[]
            {
                new AuthorDto { FullName = "A. Garrido" },
                new AuthorDto { FullName = "Jose Villalon" },
                new AuthorDto { FullName = "Blas Gomez" }
            }, JsonOptions),
            ReferencesJson = "[]"
        };
        var work = new OpenAlexWorkDto
        {
            Title = "LESS-ON",
            Abstract = "Edge computing saves energy.",
            Authors = new[]
            {
                new AuthorDto { FullName = "Blas Gómez" },
                new AuthorDto { FullName = "José Villalón" },
                new AuthorDto { FullName = "Antonio Garrido" }
            },
            RawJson = "{}"
        };

        var result = OpenAlexService.CalculateSimilarity(metadata, work);

        Assert.Equal(100, result.AuthorSimilarity);
    }

    [Fact]
    public async Task CheckSimilarityAsync_ResolvesReferenceDoiToOpenAlexIdBeforeOverlap()
    {
        var service = CreateService(request =>
        {
            var uri = request.RequestUri!.ToString();
            if (uri.Contains("/works/doi:10.1000%2Fmain", StringComparison.OrdinalIgnoreCase))
            {
                return WorkJson("https://openalex.org/WMAIN", "10.1000/main", "Main Paper", referencedWorks: ["https://openalex.org/WREF1"]);
            }

            if (uri.Contains("/works/doi:10.1000%2Fref1", StringComparison.OrdinalIgnoreCase))
            {
                return WorkJson("https://openalex.org/WREF1", "10.1000/ref1", "Reference Paper");
            }

            return "{}";
        });
        var metadata = new PaperMetadata
        {
            PaperId = 1,
            Doi = "10.1000/main",
            Title = "Main Paper",
            AuthorsJson = JsonSerializer.Serialize(new[] { new AuthorDto { FullName = "Ada Lovelace" } }, JsonOptions),
            ReferencesJson = JsonSerializer.Serialize(new[] { new ReferenceDto { Doi = "10.1000/ref1" } }, JsonOptions)
        };

        var result = await service.CheckSimilarityAsync(metadata);

        Assert.NotNull(result);
        Assert.Equal(100, result.ReferenceSimilarity);
    }

    [Fact]
    public async Task CheckSimilarityAsync_ResolvesReferenceTitleWhenDoiIsMissing()
    {
        var service = CreateService(request =>
        {
            var uri = request.RequestUri!.ToString();
            if (uri.Contains("/works/doi:10.1000%2Fmain", StringComparison.OrdinalIgnoreCase))
            {
                return WorkJson("https://openalex.org/WMAIN", "10.1000/main", "Main Paper", referencedWorks: ["https://openalex.org/WREF2"]);
            }

            if (request.RequestUri!.AbsolutePath.Equals("/works", StringComparison.OrdinalIgnoreCase))
            {
                return SearchJson(WorkJson("https://openalex.org/WREF2", "10.1000/ref2", "Reference Title", publicationYear: 2024));
            }

            return "{}";
        });
        var metadata = new PaperMetadata
        {
            PaperId = 1,
            Doi = "10.1000/main",
            Title = "Main Paper",
            AuthorsJson = JsonSerializer.Serialize(new[] { new AuthorDto { FullName = "Ada Lovelace" } }, JsonOptions),
            ReferencesJson = JsonSerializer.Serialize(new[]
            {
                new ReferenceDto { Title = "Reference Title", PublicationYear = 2024 }
            }, JsonOptions)
        };

        var result = await service.CheckSimilarityAsync(metadata);

        Assert.NotNull(result);
        Assert.Equal(100, result.ReferenceSimilarity);
    }

    [Fact]
    public void OcrWorkers_DoNotDependOnOpenAlexOrMetadataQualityScoring()
    {
        AssertDoesNotDependOnForbiddenServices(typeof(PaperOcrKafkaConsumerBackgroundService));
        AssertDoesNotDependOnForbiddenServices(typeof(PaperOcrBackgroundService));
    }

    private static void AssertDoesNotDependOnForbiddenServices(Type type)
    {
        var constructorParameterTypes = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .SelectMany(constructor => constructor.GetParameters())
            .Select(parameter => parameter.ParameterType)
            .ToArray();

        Assert.DoesNotContain(typeof(IOpenAlexService), constructorParameterTypes);
        Assert.DoesNotContain(typeof(IMetadataQualityScoringService), constructorParameterTypes);
    }

    private static OpenAlexService CreateService(Func<HttpRequestMessage, string> responder)
    {
        var httpClient = new HttpClient(new StubHttpMessageHandler(responder))
        {
            BaseAddress = new Uri("https://api.openalex.org")
        };

        return new OpenAlexService(
            httpClient,
            Microsoft.Extensions.Options.Options.Create(new OpenAlexOptions
            {
                ApiKey = "test-key",
                MaxCandidates = 10,
                MaxReferenceResolution = 30,
                ReferenceTitleThreshold = 85,
                AuthorNameThreshold = 75
            }),
            new StubOpenAlexRepository(),
            new StubPaperVersionRepository(),
            new StubCurrentUserProvider(),
            NullLogger<OpenAlexService>.Instance);
    }

    private static string WorkJson(
        string id,
        string doi,
        string title,
        int publicationYear = 2024,
        IReadOnlyList<string>? referencedWorks = null)
    {
        return JsonSerializer.Serialize(new
        {
            id,
            doi = $"https://doi.org/{doi}",
            display_name = title,
            publication_year = publicationYear,
            cited_by_count = 1,
            authorships = new[]
            {
                new
                {
                    author = new
                    {
                        display_name = "Ada Lovelace",
                        orcid = "https://orcid.org/0000-0001-0000-0000"
                    },
                    raw_author_name = "Ada Lovelace",
                    raw_orcid = "https://orcid.org/0000-0001-0000-0000",
                    is_corresponding = true,
                    raw_affiliation_strings = new[] { "Analytical Engine Institute" }
                }
            },
            referenced_works = referencedWorks ?? Array.Empty<string>(),
            abstract_inverted_index = new Dictionary<string, int[]>
            {
                ["Main"] = [0],
                ["abstract"] = [1]
            }
        }, JsonOptions);
    }

    private static string SearchJson(string workJson)
    {
        using var work = JsonDocument.Parse(workJson);
        return JsonSerializer.Serialize(new
        {
            results = new[] { work.RootElement.Clone() }
        }, JsonOptions);
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, string> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responder(request))
            };

            return Task.FromResult(response);
        }
    }

    private sealed class StubOpenAlexRepository : IOpenAlexRepository
    {
        public Task<PaperSimilarityCheck?> FindSimilarityByPaperVersionIdAsync(long paperVersionId, CancellationToken cancellationToken = default) =>
            Task.FromResult<PaperSimilarityCheck?>(null);
    }

    private sealed class StubPaperVersionRepository : IPaperVersionRepository
    {
        public Task<IReadOnlyList<PaperVersion>> FindByPaperIdAsync(long paperId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PaperVersion>>(Array.Empty<PaperVersion>());

        public Task<PaperVersion?> FindByIdAsync(long paperVersionId, CancellationToken cancellationToken = default) =>
            Task.FromResult<PaperVersion?>(null);
    }

    private sealed class StubCurrentUserProvider : ICurrentUserProvider
    {
        public string? Email => "test@example.org";
        public string? Subject => "test-user";
        public ClaimsPrincipal User { get; } = new(new ClaimsIdentity());
    }
}
