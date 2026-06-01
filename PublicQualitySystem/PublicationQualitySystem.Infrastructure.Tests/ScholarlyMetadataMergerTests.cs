using PublicationQualitySystem.Application.DTOs.Crossref;
using PublicationQualitySystem.Application.DTOs.Grobid;
using PublicationQualitySystem.Infrastructure.Services.Implementations;

namespace PublicationQualitySystem.Infrastructure.Tests;

public class ScholarlyMetadataMergerTests
{
    [Fact]
    public void Merge_FillsMissingFieldsFromCrossrefWithoutOverwritingGrobid()
    {
        var grobid = new GrobidMetadataResponse
        {
            Title = "GROBID Title",
            Doi = "10.1000/original",
            DoiSource = "GROBID",
            Journal = "GROBID Journal",
            JournalSource = "GROBID",
            Authors =
            [
                new AuthorDto
                {
                    FullName = "Grobid Author"
                }
            ]
        };
        var crossref = new CrossrefMetadataResponse
        {
            Title = "Crossref Title",
            Doi = "10.1000/crossref",
            Journal = "Crossref Journal",
            Publisher = "Crossref Publisher",
            PublicationYear = 2024,
            Volume = "8",
            Issue = "2",
            Pages = "10-20",
            Authors =
            [
                new AuthorDto
                {
                    FullName = "Crossref Author"
                }
            ]
        };

        var merged = ScholarlyMetadataMerger.Merge(grobid, crossref);

        Assert.Equal("GROBID Title", merged.Title);
        Assert.Equal("10.1000/original", merged.Doi);
        Assert.Equal("GROBID", merged.DoiSource);
        Assert.Equal("GROBID Journal", merged.Journal);
        Assert.Equal("GROBID", merged.JournalSource);
        Assert.Equal("Crossref Publisher", merged.Publisher);
        Assert.Equal(2024, merged.PublicationYear);
        Assert.Equal("8", merged.Volume);
        Assert.Equal("2", merged.Issue);
        Assert.Equal("10-20", merged.Pages);
        Assert.Single(merged.Authors);
        Assert.Equal("Grobid Author", merged.Authors[0].FullName);
        Assert.Equal("GROBID+CROSSREF", merged.MetadataSource);
    }

    [Fact]
    public void Merge_UsesCrossrefJournalAndAuthorsWhenGrobidMissesThem()
    {
        var grobid = new GrobidMetadataResponse
        {
            Title = "Paper",
            Doi = "10.1000/body",
            DoiSource = "REGEX"
        };
        var crossref = new CrossrefMetadataResponse
        {
            Journal = "Crossref Journal",
            Authors =
            [
                new AuthorDto
                {
                    FullName = "Jane Doe",
                    Affiliation = "Crossref Institute"
                }
            ]
        };

        var merged = ScholarlyMetadataMerger.Merge(grobid, crossref);

        Assert.Equal("Crossref Journal", merged.Journal);
        Assert.Equal("CROSSREF", merged.JournalSource);
        Assert.Single(merged.Authors);
        Assert.Equal("Jane Doe", merged.Authors[0].FullName);
        Assert.Equal("REGEX", merged.DoiSource);
        Assert.Equal("GROBID+CROSSREF", merged.MetadataSource);
    }
}
