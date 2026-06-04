using System.Reflection;
using System.Text.Json;
using PublicationQualitySystem.Application.DTOs.Grobid;
using PublicationQualitySystem.Application.Services.Interfaces;
using PublicationQualitySystem.Domain.Entities;
using PublicationQualitySystem.Infrastructure.Services.Implementations;

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
}
