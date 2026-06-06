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
        Assert.Contains(reference.Authors, x => x.FullName == "S Redana");
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
                    RawText = "Possibly A Name But Also A Sentence. Low confidence paper, 2024."
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
}
