using PublicationQualitySystem.Application.DTOs.Metadata;
using PublicationQualitySystem.Domain.Entities;
using PublicationQualitySystem.Infrastructure.Services.Implementations;

namespace PublicationQualitySystem.Infrastructure.Tests;

public class MetadataQualityScoreMapperTests
{
    [Fact]
    public void Apply_AndToResponse_PreserveScoreDetails()
    {
        var metadata = new PaperMetadata
        {
            PaperId = 1
        };
        var scoredAt = new DateTime(2026, 6, 3, 1, 2, 3, DateTimeKind.Utc);
        var score = new MetadataQualityScoreResponse
        {
            TotalScore = 75,
            CoreScore = 60,
            ExtendedScore = 10,
            EnrichmentScore = 5,
            Grade = "ACCEPTABLE",
            CanProceed = true,
            MissingFields = ["doi", "funding"],
            Warnings = ["Metadata quality is acceptable but should be reviewed when possible."],
            FieldScores = new Dictionary<string, int>
            {
                ["title"] = 10,
                ["doi"] = 0
            }
        };

        MetadataQualityScoreMapper.Apply(metadata, score, scoredAt);
        var response = MetadataQualityScoreMapper.ToResponse(metadata);

        Assert.NotNull(response);
        Assert.Equal(75, response.TotalScore);
        Assert.Equal(60, response.CoreScore);
        Assert.Equal(10, response.ExtendedScore);
        Assert.Equal(5, response.EnrichmentScore);
        Assert.Equal("ACCEPTABLE", response.Grade);
        Assert.True(response.CanProceed);
        Assert.Equal(["doi", "funding"], response.MissingFields);
        Assert.Single(response.Warnings);
        Assert.Equal(10, response.FieldScores["title"]);
        Assert.Equal(0, response.FieldScores["doi"]);
        Assert.Equal(scoredAt, metadata.MetadataQualityScoredAt);
    }

    [Fact]
    public void ToResponse_ReturnsNull_WhenScoreWasNeverPersisted()
    {
        var response = MetadataQualityScoreMapper.ToResponse(new PaperMetadata { PaperId = 1 });

        Assert.Null(response);
    }
}
