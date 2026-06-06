using PublicationQualitySystem.Application.DTOs.Grobid;
using PublicationQualitySystem.Infrastructure.Services.Implementations;

namespace PublicationQualitySystem.Infrastructure.Tests;

public class ReferenceQualityServiceTests
{
    private readonly ReferenceNormalizer normalizer = new();

    [Fact]
    public void NormalizeDoi_RemovesUrlPrefixAndLowercases()
    {
        var doi = normalizer.NormalizeDoi("https://doi.org/10.1145/ABC.DEF.");

        Assert.Equal("10.1145/abc.def", doi);
        Assert.True(normalizer.IsValidDoiFormat(doi));
    }

    [Fact]
    public void CleanTitle_RemovesDoiYearPagesAndVenueNoise()
    {
        var title = ReferenceNormalizer.CleanTitle(
            "SRedana OBulakci CMannweiler -View on 5G architecture 2019. 2024 252 110675 Version 3.0. Computer Networks",
            journal: "Computer Networks",
            year: 2019,
            volume: "252",
            pages: "110675");

        Assert.Equal("View on 5G architecture", title);
    }

    [Fact]
    public void ExtractAuthors_PreservesParticleSurname()
    {
        var authors = ReferenceNormalizer.ExtractAuthors("Eyal de Lara - The internet of tomorrow must sleep more and grow old. 2023.");

        var author = Assert.Single(authors);
        Assert.Equal("Eyal de Lara", author.FullName);
    }

    [Fact]
    public void ExtractAuthors_ParsesCompactInitialTokensAndParticles()
    {
        var authors = ReferenceNormalizer.ExtractAuthors("BRamprasad ADa Silva MVeith EGabel J.-MPierson AVVasilakos - Workload management in edge systems.");

        Assert.Contains(authors, x => x.FullName == "B. Ramprasad");
        Assert.Contains(authors, x => x.FullName == "A. da Silva");
        Assert.Contains(authors, x => x.FullName == "M. Veith");
        Assert.Contains(authors, x => x.FullName == "E. Gabel");
        Assert.Contains(authors, x => x.FullName == "J.-M. Pierson");
        Assert.Contains(authors, x => x.FullName == "A. V. Vasilakos");
    }

    [Fact]
    public void Normalize_TreatsCompactAuthorFragmentAsParseFailedTitle()
    {
        var result = normalizer.NormalizeDetailed(new ReferenceDto
        {
            Title = "J.-MPierson AVVasilakos",
            RawText = "J.-MPierson AVVasilakos"
        });

        Assert.Null(result.Reference.Title);
        Assert.Contains("REFERENCE_TITLE_PARSE_FAILED", result.IssueCodes);
        Assert.Contains("REFERENCE_PARSE_SUSPECT", result.IssueCodes);
    }

    [Fact]
    public void Evaluate_DetectsMissingAuthorAndVenue()
    {
        var service = new ReferenceQualityService(normalizer);

        var result = service.Evaluate(new ReferenceDto
        {
            Title = "Reference Without Author",
            Doi = "10.1000/example"
        });

        Assert.Contains("REFERENCE_MISSING_AUTHOR", result.IssueCodes);
        Assert.Contains("REFERENCE_YEAR_MISSING", result.IssueCodes);
        Assert.Contains("REFERENCE_MISSING_VENUE", result.IssueCodes);
    }

    [Fact]
    public void Evaluate_MissingDoiIsWarningQualityLossNotInvalidFormat()
    {
        var service = new ReferenceQualityService(normalizer);

        var result = service.Evaluate(new ReferenceDto
        {
            Title = "Reference Without Doi",
            Authors = [new AuthorDto { FullName = "Ada Lovelace" }],
            Journal = "Journal of Tests",
            PublicationYear = 2024
        });

        Assert.Contains("REFERENCE_DOI_MISSING", result.IssueCodes);
        Assert.True(result.DoiFormatValid);
    }

    [Fact]
    public void CalculateReferenceQualityScore_CombinesCompletenessCoverageAndCleanliness()
    {
        var service = new ReferenceQualityService(normalizer);
        var references = new[]
        {
            service.Evaluate(new ReferenceDto
            {
                Title = "Clean Reference",
                Authors = new[] { new AuthorDto { FullName = "Ada Lovelace" } },
                PublicationYear = 2024,
                Journal = "Journal of Tests",
                Doi = "10.1000/ref1"
            }),
            service.Evaluate(new ReferenceDto
            {
                Title = "No DOI Reference",
                Authors = new[] { new AuthorDto { FullName = "Grace Hopper" } },
                PublicationYear = 2023,
                Journal = "Conference of Tests"
            })
        };

        var score = service.CalculateReferenceQualityScore(
            references,
            validDoiCount: 1,
            invalidDoiCount: 0,
            titleMismatchCount: 0);

        Assert.InRange(score, 70, 100);
    }
}
