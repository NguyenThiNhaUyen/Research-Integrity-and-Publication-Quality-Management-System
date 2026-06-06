using PublicationQualitySystem.Domain.Enums;
using PublicationQualitySystem.Infrastructure.Services.Implementations;

namespace PublicationQualitySystem.Infrastructure.Tests;

public class PaperDoiCheckServiceTests
{
    [Theory]
    [InlineData("10.1000/xyz123")]
    [InlineData("https://doi.org/10.1145/3368089.3409703")]
    [InlineData("doi:10.1038/s41586-020-2649-2")]
    public void IsValidDoiFormat_AcceptsValidDoiForms(string doi)
    {
        Assert.True(PaperDoiCheckService.IsValidDoiFormat(doi));
    }

    [Theory]
    [InlineData("")]
    [InlineData("1")]
    [InlineData("abc")]
    [InlineData("10/invalid")]
    public void IsValidDoiFormat_RejectsInvalidDoiForms(string doi)
    {
        Assert.False(PaperDoiCheckService.IsValidDoiFormat(doi));
    }

    [Fact]
    public void TitleSimilarity_DetectsEquivalentNormalizedTitles()
    {
        var score = PaperDoiCheckService.TitleSimilarity(
            "Edge Computing for Sustainable Wireless Networks",
            "edge computing for sustainable wireless networks");

        Assert.Equal(100, score);
    }

    [Fact]
    public void TitleSimilarity_DetectsTitleMismatch()
    {
        var score = PaperDoiCheckService.TitleSimilarity(
            "Edge Computing for Sustainable Wireless Networks",
            "A Survey of Marine Biology");

        Assert.True(score < 80);
    }

    [Fact]
    public void TitleSimilarity_AcceptsMeaningfulPrefixForShortCrossrefTitle()
    {
        var score = PaperDoiCheckService.TitleSimilarity(
            "Let's wait awhile: how temporal workload shifting can reduce carbon emissions in the cloud",
            "Let's wait awhile");

        Assert.True(score >= 75);
    }

    [Fact]
    public void CalculateOverallScore_PenalizesMissingMainDoiAndLowReferenceCoverage()
    {
        var score = PaperDoiCheckService.CalculateOverallScore(
            DoiValidationStatus.MISSING,
            mainTitleMismatch: false,
            referenceCoveragePercent: 40,
            invalidReferenceDois: 2,
            referenceTitleMismatches: 1);

        Assert.Equal(40, score);
        Assert.Equal(DoiRiskLevel.HIGH, PaperDoiCheckService.RiskForScore(score));
    }

    [Fact]
    public void CalculateOverallScore_MapsHealthyDoiDataToLowRisk()
    {
        var score = PaperDoiCheckService.CalculateOverallScore(
            DoiValidationStatus.VALID,
            mainTitleMismatch: false,
            referenceCoveragePercent: 90,
            invalidReferenceDois: 0,
            referenceTitleMismatches: 0);

        Assert.Equal(100, score);
        Assert.Equal(DoiRiskLevel.LOW, PaperDoiCheckService.RiskForScore(score));
    }

    [Theory]
    [InlineData(80, DoiRiskLevel.LOW)]
    [InlineData(60, DoiRiskLevel.MEDIUM)]
    [InlineData(59, DoiRiskLevel.HIGH)]
    public void RiskForScore_UsesConfiguredThresholds(int score, DoiRiskLevel expected)
    {
        Assert.Equal(expected, PaperDoiCheckService.RiskForScore(score));
    }
}
