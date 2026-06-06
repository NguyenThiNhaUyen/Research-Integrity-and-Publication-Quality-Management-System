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
