using PublicationQualitySystem.Application.DTOs.Crossref;
using PublicationQualitySystem.Application.DTOs.Grobid;
using PublicationQualitySystem.Infrastructure.Services.Implementations;

namespace PublicationQualitySystem.Infrastructure.Tests;

public class MetadataNormalizerServiceTests
{
    private readonly MetadataNormalizerService service = new(new ReferenceNormalizer());

    [Fact]
    public void Normalize_CorrectsPublisherFromElsevierDoi()
    {
        var result = service.Normalize(new GrobidMetadataResponse
        {
            Doi = "10.1016/j.comnet.2024.110675",
            Publisher = "ACM"
        });

        Assert.Equal("Elsevier", result.Metadata.Publisher);
        Assert.Contains("MAIN_PUBLISHER_MISMATCH", result.IssueCodes);
    }

    [Fact]
    public void Normalize_PrefersCrossrefPublisherOverDoiHeuristic()
    {
        var result = service.Normalize(
            new GrobidMetadataResponse
            {
                Doi = "10.1016/j.comnet.2024.110675",
                Publisher = "ACM"
            },
            new CrossrefMetadataResponse
            {
                Publisher = "Elsevier BV"
            });

        Assert.Equal("Elsevier BV", result.Metadata.Publisher);
    }

    [Fact]
    public void Normalize_SplitsLessOnCombinedKeywords()
    {
        var result = service.Normalize(new GrobidMetadataResponse
        {
            Keywords =
            [
                "Edge computing Energy efficiency 5G Wireless networks Sustainable communications"
            ]
        });

        Assert.Equal(
            [
                "Edge computing",
                "Energy efficiency",
                "5G",
                "Wireless networks",
                "Sustainable communications"
            ],
            result.Metadata.Keywords);
        Assert.Contains("KEYWORDS_NOT_SPLIT", result.IssueCodes);
    }

    [Fact]
    public void Normalize_CleansPollutedReferenceTitleAndExtractsAuthor()
    {
        var result = service.Normalize(new GrobidMetadataResponse
        {
            References =
            [
                new ReferenceDto
                {
                    Title = "SRedana OBulakci CMannweiler 5G PPP Architecture Working Group -View on 5G architecture 2019. 2024 252 110675 Version 3.0. Computer Networks",
                    Journal = "Computer Networks",
                    PublicationYear = 2019,
                    Pages = "110675",
                    RawText = "SRedana OBulakci CMannweiler 5G PPP Architecture Working Group -View on 5G architecture 2019. Computer Networks"
                }
            ]
        });

        var reference = Assert.Single(result.Metadata.References);
        Assert.Equal("View on 5G architecture", reference.Title);
        Assert.Equal("Computer Networks", reference.Journal);
        Assert.Contains(reference.Authors, x => x.FullName == "S. Redana");
        Assert.Contains("REFERENCE_TITLE_POLLUTED", result.IssueCodes);
    }

    [Fact]
    public void Normalize_DoesNotWriteLowConfidenceAuthorSuggestion()
    {
        var result = service.Normalize(new GrobidMetadataResponse
        {
            References =
            [
                new ReferenceDto
                {
                    Title = "Low confidence paper",
                    RawText = "Ada Lovelace; 2024. Low confidence paper."
                }
            ]
        });

        var reference = Assert.Single(result.Metadata.References);
        Assert.Empty(reference.Authors);
        Assert.Contains("REFERENCE_AUTHOR_LOW_CONFIDENCE", result.IssueCodes);
    }

    [Fact]
    public void Normalize_DetectsJournalTitleOverlap()
    {
        var result = service.Normalize(new GrobidMetadataResponse
        {
            References =
            [
                new ReferenceDto
                {
                    Title = "Energy Efficient Edge Computing",
                    Journal = "Energy Efficient Edge Computing"
                }
            ]
        });

        var reference = Assert.Single(result.Metadata.References);
        Assert.Null(reference.Journal);
        Assert.Contains("REFERENCE_JOURNAL_SUSPECT", result.IssueCodes);
    }

    [Fact]
    public void Normalize_ExtractsYearFromRawTextAndAvoidsDoiFragments()
    {
        var result = service.Normalize(new GrobidMetadataResponse
        {
            References =
            [
                new ReferenceDto
                {
                    Title = "A DOI year trap",
                    RawText = "Ada Lovelace - A DOI year trap. doi:10.1000/2024abc. Published 2023."
                }
            ]
        });

        var reference = Assert.Single(result.Metadata.References);
        Assert.Equal(2023, reference.PublicationYear);
    }

    [Fact]
    public void Normalize_DetectsSuspectPages()
    {
        var result = service.Normalize(new GrobidMetadataResponse
        {
            References =
            [
                new ReferenceDto
                {
                    Title = "Reference",
                    Doi = "10.1038/s41586-020-2649-2",
                    Pages = "s41586-020-2649-2"
                }
            ]
        });

        var reference = Assert.Single(result.Metadata.References);
        Assert.Null(reference.Pages);
        Assert.Contains("REFERENCE_PAGES_SUSPECT", result.IssueCodes);
    }

    [Fact]
    public void Normalize_CalculatesCleanlinessScoreAndDirtyFieldCount()
    {
        var result = service.Normalize(new GrobidMetadataResponse
        {
            Doi = "https://doi.org/10.1016/j.comnet.2024.110675",
            Publisher = "ACM",
            Keywords = ["Edge computing Energy efficiency 5G Wireless networks Sustainable communications"]
        });

        Assert.InRange(result.MainMetadataCleanlinessScore, 0, 99);
        Assert.True(result.ReferenceCleanlinessScore >= 0);
        Assert.True(result.DirtyFieldCount >= 2);
    }

    [Fact]
    public void Normalize_MarksParentMetadataLeakAsReferenceBoundarySuspect()
    {
        var result = service.Normalize(new GrobidMetadataResponse
        {
            Title = "LESS-ON: Load-aware edge server shutdown for energy saving in cellular networks",
            Journal = "Computer Networks",
            Volume = "252",
            Pages = "110675",
            References =
            [
                new ReferenceDto
                {
                    Title = "5G PPP Architecture Working Group -View on 5G architecture",
                    Journal = "Computer Networks",
                    Volume = "252",
                    Pages = "110675",
                    RawText = "5G PPP Architecture Working Group -View on 5G architecture"
                }
            ]
        });

        var reference = Assert.Single(result.Metadata.References);
        Assert.Contains("REFERENCE_BOUNDARY_SUSPECT", reference.IssueCodes);
        Assert.Contains("REFERENCE_BOUNDARY_SUSPECT", result.IssueCodes);
        Assert.True(result.ReferenceResults[0].MetadataCompletenessScore < 60);
    }

    [Fact]
    public void Normalize_MarksBoundarySuspectWhenTwoParentFieldsMatchEvenWithDoi()
    {
        var result = service.Normalize(new GrobidMetadataResponse
        {
            Title = "LESS-ON: Load-aware edge server shutdown for energy saving in cellular networks",
            Journal = "Computer Networks",
            Volume = "252",
            Pages = "110675",
            References =
            [
                new ReferenceDto
                {
                    Title = "Independent edge computing reference",
                    Journal = "Computer Networks",
                    Volume = "252",
                    Doi = "10.1109/example.2024.1",
                    RawText = "Independent edge computing reference"
                }
            ]
        });

        var reference = Assert.Single(result.Metadata.References);
        Assert.Contains("REFERENCE_BOUNDARY_SUSPECT", reference.IssueCodes);
        Assert.True(result.ReferenceResults[0].MetadataCompletenessScore < 75);
    }

    [Fact]
    public void Normalize_DoesNotMarkBoundarySuspectForJournalOnlyMatch()
    {
        var result = service.Normalize(new GrobidMetadataResponse
        {
            Title = "LESS-ON: Load-aware edge server shutdown for energy saving in cellular networks",
            Journal = "Computer Networks",
            Volume = "252",
            Pages = "110675",
            References =
            [
                new ReferenceDto
                {
                    Title = "Independent edge computing reference",
                    Journal = "Computer Networks",
                    RawText = "Independent edge computing reference"
                }
            ]
        });

        var reference = Assert.Single(result.Metadata.References);
        Assert.DoesNotContain("REFERENCE_BOUNDARY_SUSPECT", reference.IssueCodes);
    }

    [Fact]
    public void Normalize_PreservesLifecycleDatesAndLicense()
    {
        var result = service.Normalize(new GrobidMetadataResponse
        {
            ReceivedDate = new DateOnly(2024, 1, 1),
            RevisedDate = new DateOnly(2024, 2, 1),
            AcceptedDate = new DateOnly(2024, 3, 1),
            PublishedDate = new DateOnly(2024, 4, 1),
            OpenAccessLicense = "https://creativecommons.org/licenses/by/4.0/"
        });

        Assert.Equal(new DateOnly(2024, 1, 1), result.Metadata.ReceivedDate);
        Assert.Equal(new DateOnly(2024, 2, 1), result.Metadata.RevisedDate);
        Assert.Equal(new DateOnly(2024, 3, 1), result.Metadata.AcceptedDate);
        Assert.Equal(new DateOnly(2024, 4, 1), result.Metadata.PublishedDate);
        Assert.Equal("CC BY 4.0", result.Metadata.OpenAccessLicense);
    }
}
