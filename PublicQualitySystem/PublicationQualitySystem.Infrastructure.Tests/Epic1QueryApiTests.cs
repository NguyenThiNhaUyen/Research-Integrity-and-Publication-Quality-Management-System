using System.Security.Claims;
using System.Text.Json;
using PublicationQualitySystem.Application.Mappings;
using PublicationQualitySystem.Application.Repositories.Interfaces;
using PublicationQualitySystem.Domain.Entities;
using PublicationQualitySystem.Domain.Enums;
using PublicationQualitySystem.Infrastructure.Security;
using PublicationQualitySystem.Infrastructure.Services.Implementations;
using PublicationQualitySystem.Shared.Constants;
using PublicationQualitySystem.Shared.Exceptions;

namespace PublicationQualitySystem.Infrastructure.Tests;

public class Epic1QueryApiTests
{
    [Fact]
    public void PaperMapper_MapsCurrentVersionSummary()
    {
        var paper = new Paper
        {
            Id = 10,
            Title = "Lifecycle Paper",
            CurrentVersion = 2,
            CreatedBy = "user-1",
            Versions =
            [
                new PaperVersion { Id = 1, VersionNumber = 1, OriginalFileName = "v1.pdf", PdfS3Key = "pdf/v1.pdf" },
                new PaperVersion { Id = 2, VersionNumber = 2, OriginalFileName = "v2.pdf", PdfS3Key = "pdf/v2.pdf", MarkdownS3Key = "md/v2.md", ConversionStatus = ConversionStatus.Completed }
            ]
        };

        var response = PaperMapper.ToResponse(paper);

        Assert.Equal(10, response.Id);
        Assert.Equal("Lifecycle Paper", response.Title);
        Assert.NotNull(response.CurrentVersionInfo);
        Assert.Equal(2, response.CurrentVersionInfo!.PaperVersionId);
        Assert.Equal(ConversionStatus.Completed, response.CurrentVersionInfo.ConversionStatus);
    }

    [Fact]
    public void MetadataQualityMapper_ReturnsPassFailAndExplanation()
    {
        var metadata = new PaperMetadata
        {
            Id = 5,
            PaperId = 10,
            MetadataQualityTotalScore = 65,
            MetadataQualityCoreScore = 45,
            MetadataQualityExtendedScore = 15,
            MetadataQualityEnrichmentScore = 5,
            MetadataQualityGrade = "POOR",
            MetadataQualityCanProceed = false,
            MetadataQualityMissingFieldsJson = JsonSerializer.Serialize(new[] { "doi" }),
            MetadataQualityWarningsJson = JsonSerializer.Serialize(new[] { "Human review required" }),
            MetadataQualityFieldScoresJson = JsonSerializer.Serialize(new Dictionary<string, int> { ["doi"] = 0 })
        };

        var response = MetadataQualityMapper.ToResponse(metadata, paperVersionId: 20);

        Assert.Equal(65, response.MetadataScore);
        Assert.False(response.Passed);
        Assert.Contains("failed", response.Explanation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("doi", response.MissingFields);
        Assert.Equal(0, response.FieldScores["doi"]);
    }

    [Fact]
    public async Task PaperVersionService_AllowsOwnerToReadVersions()
    {
        var paper = new Paper { Id = 10, Title = "Owned Paper", CreatedBy = "user-1" };
        var version = new PaperVersion
        {
            Id = 20,
            PaperId = paper.Id,
            Paper = paper,
            VersionNumber = 1,
            OriginalFileName = "paper.pdf",
            PdfS3Key = "papers/pdf/paper.pdf"
        };

        var service = new PaperVersionService(
            new StubPaperRepository(paper),
            new StubPaperVersionRepository([version]),
            new StubCurrentUserProvider("user-1"));

        var result = await service.GetVersionsByPaperIdAsync(paper.Id, CancellationToken.None);

        var response = Assert.Single(result);
        Assert.Equal(20, response.PaperVersionId);
        Assert.Equal("paper.pdf", response.OriginalFileName);
    }

    [Fact]
    public async Task PaperVersionService_BlocksNonOwnerWithoutReadAll()
    {
        var paper = new Paper { Id = 10, Title = "Owned Paper", CreatedBy = "user-1" };
        var service = new PaperVersionService(
            new StubPaperRepository(paper),
            new StubPaperVersionRepository([]),
            new StubCurrentUserProvider("user-2"));

        await Assert.ThrowsAsync<AppException>(() =>
            service.GetVersionsByPaperIdAsync(paper.Id, CancellationToken.None));
    }

    private sealed class StubPaperRepository(Paper? paper) : IPaperRepository
    {
        public Task<IReadOnlyList<Paper>> FindPagedAsync(
            string? search,
            string? ownerUserId,
            bool includeAll,
            int skip,
            int take,
            bool sortDescending,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Paper>>(paper is null ? Array.Empty<Paper>() : [paper]);

        public Task<Paper?> FindByIdAsync(long paperId, CancellationToken cancellationToken = default) =>
            Task.FromResult(paper?.Id == paperId ? paper : null);

        public Task<bool> ExistsAsync(long paperId, CancellationToken cancellationToken = default) =>
            Task.FromResult(paper?.Id == paperId);
    }

    private sealed class StubPaperVersionRepository(IReadOnlyList<PaperVersion> versions) : IPaperVersionRepository
    {
        public Task<IReadOnlyList<PaperVersion>> FindByPaperIdAsync(long paperId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PaperVersion>>(versions.Where(x => x.PaperId == paperId).ToArray());

        public Task<PaperVersion?> FindByIdAsync(long paperVersionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(versions.FirstOrDefault(x => x.Id == paperVersionId));
    }

    private sealed class StubCurrentUserProvider(string subject, bool canReadAll = false) : ICurrentUserProvider
    {
        public string? Email => "test@example.org";
        public string? Subject => subject;
        public ClaimsPrincipal User { get; } = new(new ClaimsIdentity(BuildClaims(subject, canReadAll)));

        private static IEnumerable<Claim> BuildClaims(string subject, bool canReadAll)
        {
            yield return new Claim("sub", subject);
            if (canReadAll)
            {
                yield return new Claim(ClaimConstants.Permission, nameof(PermissionName.PAPER_READ_ALL));
            }
        }
    }
}
