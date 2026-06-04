using System.Text.Json;
using PublicationQualitySystem.Application.DTOs.Grobid;
using PublicationQualitySystem.Domain.Entities;
using PublicationQualitySystem.Infrastructure.Services.Implementations;

namespace PublicationQualitySystem.Infrastructure.Tests;

public class MetadataQualityScoringServiceTests
{
    private readonly MetadataQualityScoringService _service = new();

    [Fact]
    public void Calculate_FullJournalArticleMetadataScoresExcellent()
    {
        var metadata = CreateFullJournalMetadata();

        var score = _service.Calculate(metadata);

        Assert.True(score.TotalScore >= 90);
        Assert.Equal("EXCELLENT", score.Grade);
        Assert.True(score.CanProceed);
    }

    [Fact]
    public void Calculate_JournalArticleMissingConferenceNameDoesNotLoseSourcePoints()
    {
        var metadata = CreateFullJournalMetadata();
        metadata.ConferenceName = null;
        metadata.Venue = null;

        var score = _service.Calculate(metadata);

        Assert.Equal(10, score.FieldScores["sourceName"]);
        Assert.DoesNotContain("sourceName", score.MissingFields);
    }

    [Fact]
    public void Calculate_ConferencePaperMissingJournalNameDoesNotLoseSourcePoints()
    {
        var metadata = CreateFullJournalMetadata();
        metadata.Journal = null;
        metadata.ConferenceName = "International Conference on Metadata Quality";
        metadata.Venue = "MQ '26";

        var score = _service.Calculate(metadata);

        Assert.Equal(10, score.FieldScores["sourceName"]);
        Assert.DoesNotContain("sourceName", score.MissingFields);
        Assert.True(score.CanProceed);
    }

    [Fact]
    public void Calculate_MissingDoiReducesFifteenPoints()
    {
        var baseline = _service.Calculate(CreateFullJournalMetadata());
        var metadata = CreateFullJournalMetadata();
        metadata.Doi = null;

        var score = _service.Calculate(metadata);

        Assert.Equal(0, score.FieldScores["doi"]);
        Assert.Equal(baseline.TotalScore - 15, score.TotalScore);
        Assert.Contains("doi", score.MissingFields);
    }

    [Fact]
    public void Calculate_MissingAbstractReducesTenPoints()
    {
        var baseline = _service.Calculate(CreateFullJournalMetadata());
        var metadata = CreateFullJournalMetadata();
        metadata.Abstract = null;

        var score = _service.Calculate(metadata);

        Assert.Equal(0, score.FieldScores["abstract"]);
        Assert.Equal(baseline.TotalScore - 10, score.TotalScore);
        Assert.Contains("abstract", score.MissingFields);
    }

    [Fact]
    public void Calculate_MissingOrcidReducesOnlyFourPointsAndDoesNotBlock()
    {
        var baseline = _service.Calculate(CreateFullJournalMetadata());
        var metadata = CreateFullJournalMetadata();
        metadata.AuthorsJson = SerializeAuthors(includeOrcid: false, includeEmail: true, includeAffiliation: true);

        var score = _service.Calculate(metadata);

        Assert.Equal(0, score.FieldScores["orcid"]);
        Assert.Equal(baseline.TotalScore - 4, score.TotalScore);
        Assert.True(score.CanProceed);
    }

    [Fact]
    public void Calculate_ScoreBelowSeventyRequiresHumanReview()
    {
        var metadata = new PaperMetadata
        {
            PaperId = 1,
            Title = "Sparse Metadata",
            AuthorsJson = "[]",
            KeywordsJson = "[]",
            ReferencesJson = "[]"
        };

        var score = _service.Calculate(metadata);

        Assert.False(score.CanProceed);
        Assert.True(score.Grade is "POOR" or "FAIL");
        Assert.Contains(score.Warnings, warning => warning.Contains("Human review", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Calculate_WithFundingAndAffiliationsButNoOrcid_OnlyMarksOrcidMissing()
    {
        var metadata = CreateFullJournalMetadata();
        metadata.AuthorsJson = SerializeAuthors(includeOrcid: false, includeEmail: true, includeAffiliation: true);

        var score = _service.Calculate(metadata);

        Assert.Equal(["orcid"], score.MissingFields);
        Assert.DoesNotContain("funding", score.MissingFields);
        Assert.DoesNotContain("affiliations", score.MissingFields);
        Assert.DoesNotContain(score.Warnings, warning => warning.Contains("Funding metadata is missing", StringComparison.OrdinalIgnoreCase));
        Assert.True(score.CanProceed);
    }

    private static PaperMetadata CreateFullJournalMetadata() => new()
    {
        PaperId = 1,
        Title = "A Complete Metadata Paper",
        Abstract = "This paper has rich metadata.",
        Doi = "10.1000/xyz123",
        Journal = "Journal of Metadata Quality",
        Publisher = "RIPQMS Press",
        PublicationYear = 2026,
        CorrespondingAuthor = "Ada Lovelace <ada@example.org>",
        AuthorsJson = SerializeAuthors(includeOrcid: true, includeEmail: true, includeAffiliation: true),
        KeywordsJson = JsonSerializer.Serialize(new[] { "metadata quality", "GROBID" }, JsonOptions),
        FundingOrganizationsJson = JsonSerializer.Serialize(new[] { "European Social Fund" }, JsonOptions),
        ReferencesJson = JsonSerializer.Serialize(new[]
        {
            new ReferenceDto
            {
                Title = "Reference Paper"
            }
        }, JsonOptions)
    };

    private static string SerializeAuthors(bool includeOrcid, bool includeEmail, bool includeAffiliation)
    {
        var author = new Dictionary<string, string?>
        {
            ["fullName"] = "Ada Lovelace"
        };

        if (includeEmail)
        {
            author["email"] = "ada@example.org";
        }

        if (includeAffiliation)
        {
            author["affiliation"] = "RIPQMS University";
        }

        if (includeOrcid)
        {
            author["ORCID"] = "0000-0002-1825-0097";
        }

        return JsonSerializer.Serialize(new[] { author }, JsonOptions);
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
}
