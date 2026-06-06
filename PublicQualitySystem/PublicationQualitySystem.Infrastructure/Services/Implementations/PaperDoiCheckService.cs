using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using PublicationQualitySystem.Application.DTOs.Doi;
using PublicationQualitySystem.Application.DTOs.Grobid;
using PublicationQualitySystem.Application.DTOs.References;
using PublicationQualitySystem.Application.Services.Interfaces;
using PublicationQualitySystem.Domain.Entities;
using PublicationQualitySystem.Domain.Enums;
using PublicationQualitySystem.Infrastructure.Configurations;
using PublicationQualitySystem.Shared.Exceptions;

namespace PublicationQualitySystem.Infrastructure.Services.Implementations;

public sealed class PaperDoiCheckService(
    ApplicationDbContext db,
    ICrossrefService crossref,
    IOpenAlexService openAlex,
    IReferenceQualityService referenceQuality,
    IPaperProcessingTrackerService processingTracker,
    IAuditLogService auditLog,
    ILogger<PaperDoiCheckService> logger) : IPaperDoiCheckService
{
    private const double TitleMismatchThreshold = 75;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly Regex DoiRegex = new(
        @"^10\.\d{4,9}/[-._;()/:A-Z0-9]+$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public async Task<PaperDoiCheckResponse> RunAsync(
        long paperId,
        long paperMetadataId,
        long? paperVersionId = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        var metadata = await db.PaperMetadata
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == paperMetadataId && x.PaperId == paperId, cancellationToken);
        if (metadata is null)
        {
            throw new AppException(AuditLogErrorCode.NotFound);
        }

        if (paperVersionId.HasValue)
        {
            await processingTracker.RecordStepStartedAsync(
                paperVersionId.Value,
                ProcessingStage.METADATA_COMPLETED,
                "DoiCheckStarted",
                cancellationToken: cancellationToken);
        }

        await auditLog.StartStepAsync(
            ProcessingStep.DOI_INTEGRITY_CHECK,
            "Validate paper and reference DOIs",
            paperId: paperId,
            paperVersionId: paperVersionId,
            correlationId: correlationId,
            message: "DOI integrity check started.",
            cancellationToken: cancellationToken);

        var now = DateTime.UtcNow;
        var check = await db.PaperDoiChecks
            .Include(x => x.ReferenceChecks)
            .FirstOrDefaultAsync(x => x.PaperMetadataId == paperMetadataId, cancellationToken);
        if (check is null)
        {
            check = new PaperDoiCheck
            {
                PaperId = paperId,
                PaperMetadataId = paperMetadataId,
                PaperVersionId = paperVersionId
            };
            db.PaperDoiChecks.Add(check);
        }
        else
        {
            db.PaperReferenceDoiChecks.RemoveRange(check.ReferenceChecks);
            check.ReferenceChecks.Clear();
            check.PaperVersionId = paperVersionId ?? check.PaperVersionId;
        }

        check.Status = DoiCheckStatus.PROCESSING;
        check.ErrorMessage = null;
        check.CheckedAt = null;
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            var main = await ValidateDoiAsync(metadata.Doi, metadata.Title, metadata.PublicationYear, isMain: true, cancellationToken);
            ApplyMainResult(check, metadata.Doi, main);
            await db.SaveChangesAsync(cancellationToken);

            if (paperVersionId.HasValue)
            {
                await processingTracker.RecordStepCompletedAsync(
                    paperVersionId.Value,
                    ProcessingStage.METADATA_COMPLETED,
                    "MainDoiChecked",
                    JsonSerializer.Serialize(new { status = main.Status, doi = metadata.Doi }, JsonOptions),
                    cancellationToken);
                await processingTracker.RecordStepStartedAsync(
                    paperVersionId.Value,
                    ProcessingStage.METADATA_COMPLETED,
                    "ReferenceQualityCheckStarted",
                    cancellationToken: cancellationToken);
            }

            var references = DeserializeReferences(metadata.ReferencesJson);
            check.TotalReferences = references.Count;
            var qualityResults = new List<ReferenceQualityResult>();
            for (var i = 0; i < references.Count; i++)
            {
                try
                {
                    var quality = referenceQuality.Evaluate(references[i]);
                    var recovered = await RecoverReferenceDoiAsync(quality, cancellationToken);
                    if (recovered is not null)
                    {
                        quality.Reference.Doi = recovered.MatchedDoi;
                        quality.Reference.DoiSource = "CROSSREF_TITLE_SEARCH";
                        quality.Reference.DoiConfidence = "HIGH";
                        quality.NormalizedDoi = recovered.MatchedDoi;
                        quality.DoiFormatValid = true;
                        quality.IssueCodes = quality.IssueCodes
                            .Where(issue => !string.Equals(issue, "REFERENCE_DOI_MISSING", StringComparison.OrdinalIgnoreCase))
                            .ToArray();
                        quality.WarningMessages = quality.WarningMessages
                            .Append("Reference DOI recovered by high-confidence Crossref title search.")
                            .ToArray();
                    }
                    qualityResults.Add(quality);
                    var result = recovered is null
                        ? await ValidateDoiAsync(
                            quality.Reference.Doi,
                            quality.Reference.Title,
                            quality.Reference.PublicationYear,
                            isMain: false,
                            cancellationToken)
                        : ApplyMetadataComparison(recovered, quality.Reference.Title, quality.Reference.PublicationYear);
                    check.ReferenceChecks.Add(ToReferenceCheck(i, references[i], quality, result, now));
                }
                catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                {
                    logger.LogWarning(
                        ex,
                        "Reference quality check failed for one reference. PaperId={PaperId}, PaperMetadataId={PaperMetadataId}, ReferenceOrdinal={ReferenceOrdinal}",
                        paperId,
                        paperMetadataId,
                        i);
                    var fallbackQuality = referenceQuality.Evaluate(references[i]);
                    qualityResults.Add(fallbackQuality);
                    var fallbackResult = DoiValidationResult.Issue(
                        DoiValidationStatus.SERVICE_ERROR,
                        "REFERENCE_VALIDATION_SERVICE_ERROR",
                        ex.Message,
                        fallbackQuality.Reference.Doi);
                    check.ReferenceChecks.Add(ToReferenceCheck(i, references[i], fallbackQuality, fallbackResult, now));
                }
            }

            ApplySummary(check, qualityResults);
            check.Status = DoiCheckStatus.COMPLETED;
            check.CheckedAt = now;
            check.RawJson = BuildSummaryJson(check);
            await db.SaveChangesAsync(cancellationToken);

            if (paperVersionId.HasValue)
            {
                await processingTracker.RecordStepCompletedAsync(
                    paperVersionId.Value,
                    ProcessingStage.METADATA_COMPLETED,
                    "ReferenceNormalized",
                    JsonSerializer.Serialize(new
                    {
                        normalizedReferences = check.ReferenceChecks.Count,
                        check.MissingReferenceAuthors,
                        check.MissingReferenceVenues,
                        check.LowConfidenceReferences
                    }, JsonOptions),
                    cancellationToken);
                await processingTracker.RecordStepCompletedAsync(
                    paperVersionId.Value,
                    ProcessingStage.METADATA_COMPLETED,
                    "ReferenceDoiValidated",
                    JsonSerializer.Serialize(new
                    {
                        check.ValidReferenceDois,
                        check.InvalidReferenceDois,
                        check.ReferenceDoiTitleMismatches
                    }, JsonOptions),
                    cancellationToken);
                await processingTracker.RecordStepCompletedAsync(
                    paperVersionId.Value,
                    ProcessingStage.METADATA_COMPLETED,
                    "ReferenceDoiCheckCompleted",
                    JsonSerializer.Serialize(new
                    {
                        check.TotalReferences,
                        check.ReferencesWithDoi,
                        check.ReferencesMissingDoi,
                        check.ValidReferenceDois,
                        check.InvalidReferenceDois,
                        check.ReferenceDoiTitleMismatches
                    }, JsonOptions),
                    cancellationToken);
                await processingTracker.RecordStepCompletedAsync(
                    paperVersionId.Value,
                    ProcessingStage.METADATA_COMPLETED,
                    "ReferenceQualityCheckCompleted",
                    JsonSerializer.Serialize(new
                    {
                        check.ReferenceQualityScore,
                        check.ReferenceCleanlinessIssues,
                        check.DuplicateReferences
                    }, JsonOptions),
                    cancellationToken);
                await processingTracker.RecordStepCompletedAsync(
                    paperVersionId.Value,
                    ProcessingStage.METADATA_COMPLETED,
                    "DoiCheckCompleted",
                    JsonSerializer.Serialize(new { check.OverallScore, check.RiskLevel }, JsonOptions),
                    cancellationToken);
            }

            await auditLog.CompleteStepAsync(
                ProcessingStep.DOI_INTEGRITY_CHECK,
                "Validate paper and reference DOIs",
                paperId: paperId,
                paperVersionId: paperVersionId,
                correlationId: correlationId,
                message: "DOI integrity check completed.",
                metadata: new
                {
                    check.OverallScore,
                    check.RiskLevel,
                    check.ReferenceDoiCoveragePercent,
                    check.MainDoiStatus
                },
                cancellationToken: cancellationToken);

            return ToResponse(check);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            check.Status = DoiCheckStatus.FAILED;
            check.ErrorMessage = ex.Message;
            check.CheckedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(CancellationToken.None);

            if (paperVersionId.HasValue)
            {
                await processingTracker.RecordStepSkippedAsync(
                    paperVersionId.Value,
                    ProcessingStage.METADATA_COMPLETED,
                    "ReferenceQualityCheckFailed",
                    ex.Message,
                    CancellationToken.None);
                await processingTracker.RecordStepSkippedAsync(
                    paperVersionId.Value,
                    ProcessingStage.METADATA_COMPLETED,
                    "DoiCheckFailed",
                    ex.Message,
                    CancellationToken.None);
            }

            await auditLog.FailStepAsync(
                ProcessingStep.DOI_INTEGRITY_CHECK,
                "Validate paper and reference DOIs",
                paperId: paperId,
                paperVersionId: paperVersionId,
                correlationId: correlationId,
                message: "DOI integrity check failed.",
                errorMessage: ex.Message,
                cancellationToken: CancellationToken.None);

            logger.LogWarning(ex, "DOI integrity check failed. PaperId={PaperId}, PaperMetadataId={PaperMetadataId}", paperId, paperMetadataId);
            return ToResponse(check);
        }
    }

    public async Task<PaperDoiCheckResponse> GetByPaperIdAsync(long paperId, CancellationToken cancellationToken = default)
    {
        var check = await db.PaperDoiChecks
            .AsNoTracking()
            .Include(x => x.ReferenceChecks)
            .OrderByDescending(x => x.CheckedAt ?? x.UpdatedAt)
            .FirstOrDefaultAsync(x => x.PaperId == paperId, cancellationToken);
        if (check is null)
        {
            throw new AppException(AuditLogErrorCode.NotFound);
        }

        return ToResponse(check);
    }

    public async Task<PaperDoiCheckSummaryResponse> GetSummaryByPaperIdAsync(long paperId, CancellationToken cancellationToken = default)
    {
        var check = await db.PaperDoiChecks
            .AsNoTracking()
            .OrderByDescending(x => x.CheckedAt ?? x.UpdatedAt)
            .FirstOrDefaultAsync(x => x.PaperId == paperId, cancellationToken);
        if (check is null)
        {
            throw new AppException(AuditLogErrorCode.NotFound);
        }

        return new PaperDoiCheckSummaryResponse
        {
            PaperId = check.PaperId,
            PaperMetadataId = check.PaperMetadataId,
            PaperVersionId = check.PaperVersionId,
            MainDoi = check.MainDoi,
            MainDoiStatus = check.MainDoiStatus,
            MainDoiIssueCode = check.MainDoiIssueCode,
            MainDoiIssueMessage = check.MainDoiIssueMessage,
            ReferenceDoiCoveragePercent = check.ReferenceDoiCoveragePercent,
            MissingReferenceAuthors = check.MissingReferenceAuthors,
            MissingReferenceVenues = check.MissingReferenceVenues,
            LowConfidenceReferences = check.LowConfidenceReferences,
            ReferenceQualityScore = check.ReferenceQualityScore,
            OverallScore = check.OverallScore,
            RiskLevel = check.RiskLevel,
            Status = check.Status,
            CheckedAt = check.CheckedAt
        };
    }

    public static bool IsValidDoiFormat(string? doi)
    {
        var normalized = NormalizeDoi(doi);
        return !string.IsNullOrWhiteSpace(normalized) && DoiRegex.IsMatch(normalized);
    }

    public static int CalculateOverallScore(
        DoiValidationStatus mainStatus,
        bool mainTitleMismatch,
        double referenceCoveragePercent,
        int invalidReferenceDois,
        int referenceTitleMismatches)
    {
        var score = 100;
        score -= mainStatus switch
        {
            DoiValidationStatus.MISSING => 20,
            DoiValidationStatus.INVALID_FORMAT or DoiValidationStatus.NOT_FOUND or DoiValidationStatus.SERVICE_ERROR => 30,
            DoiValidationStatus.METADATA_MISMATCH => 20,
            _ => 0
        };
        if (mainTitleMismatch && mainStatus != DoiValidationStatus.METADATA_MISMATCH)
        {
            score -= 20;
        }

        if (referenceCoveragePercent < 50)
        {
            score -= 25;
        }
        else if (referenceCoveragePercent < 80)
        {
            score -= 10;
        }

        score -= invalidReferenceDois * 5;
        score -= referenceTitleMismatches * 5;
        return Math.Clamp(score, 0, 100);
    }

    public static DoiRiskLevel RiskForScore(int score) =>
        score >= 80 ? DoiRiskLevel.LOW : score >= 60 ? DoiRiskLevel.MEDIUM : DoiRiskLevel.HIGH;

    public static double TitleSimilarity(string? left, string? right)
    {
        var a = NormalizeText(left);
        var b = NormalizeText(right);
        if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
        {
            return 0;
        }

        if (a == b)
        {
            return 100;
        }

        if (IsMeaningfulPrefix(a, b))
        {
            return 100;
        }

        var distance = LevenshteinDistance(a, b);
        var maxLength = Math.Max(a.Length, b.Length);
        var levenshtein = 1.0 - (double)distance / maxLength;
        var jaccard = TokenJaccard(a, b);
        var score = Math.Max(levenshtein, jaccard) * 100;
        return Math.Round(score, 2);
    }

    private async Task<DoiValidationResult> ValidateDoiAsync(
        string? doi,
        string? expectedTitle,
        int? expectedYear,
        bool isMain,
        CancellationToken cancellationToken)
    {
        var normalizedDoi = NormalizeDoi(doi);
        if (string.IsNullOrWhiteSpace(normalizedDoi))
        {
            return DoiValidationResult.Issue(DoiValidationStatus.MISSING, isMain ? "MAIN_DOI_MISSING" : "REFERENCE_DOI_MISSING", "DOI is missing.");
        }

        if (!IsValidDoiFormat(normalizedDoi))
        {
            return DoiValidationResult.Issue(DoiValidationStatus.INVALID_FORMAT, "DOI_INVALID_FORMAT", "DOI format is invalid.", normalizedDoi);
        }

        DoiValidationResult? crossrefResult;
        try
        {
            var response = await crossref.GetWorkByDoiAsync(normalizedDoi, cancellationToken);
            crossrefResult = response is null
                ? null
                : DoiValidationResult.FromCrossref(normalizedDoi, response, JsonSerializer.Serialize(response, JsonOptions));
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Crossref DOI validation failed. Doi={Doi}", normalizedDoi);
            crossrefResult = DoiValidationResult.Issue(DoiValidationStatus.SERVICE_ERROR, "CROSSREF_SERVICE_ERROR", ex.Message, normalizedDoi);
        }

        if (crossrefResult is { Status: not DoiValidationStatus.SERVICE_ERROR })
        {
            return ApplyMetadataComparison(crossrefResult, expectedTitle, expectedYear);
        }

        try
        {
            var response = await openAlex.GetWorkByDoiAsync(normalizedDoi, cancellationToken);
            if (response is not null)
            {
                return ApplyMetadataComparison(DoiValidationResult.FromOpenAlex(normalizedDoi, response), expectedTitle, expectedYear);
            }
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(ex, "OpenAlex DOI validation failed. Doi={Doi}", normalizedDoi);
            return DoiValidationResult.Issue(DoiValidationStatus.SERVICE_ERROR, "OPENALEX_SERVICE_ERROR", ex.Message, normalizedDoi);
        }

        return crossrefResult?.Status == DoiValidationStatus.SERVICE_ERROR
            ? crossrefResult
            : DoiValidationResult.Issue(DoiValidationStatus.NOT_FOUND, "DOI_NOT_FOUND", "DOI was not found in Crossref or OpenAlex.", normalizedDoi);
    }

    private async Task<DoiValidationResult?> RecoverReferenceDoiAsync(
        ReferenceQualityResult quality,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(quality.Reference.Doi)
            || string.IsNullOrWhiteSpace(quality.Reference.Title))
        {
            return null;
        }

        var candidates = await crossref.SearchWorksByTitleAsync(quality.Reference.Title, rows: 5, cancellationToken);
        var best = candidates
            .Select(candidate => new
            {
                Candidate = candidate,
                Similarity = TitleSimilarity(quality.Reference.Title, candidate.Title)
            })
            .Where(x => x.Similarity >= TitleMismatchThreshold)
            .OrderByDescending(x => x.Similarity)
            .FirstOrDefault();
        if (best is null || string.IsNullOrWhiteSpace(best.Candidate.Doi))
        {
            return null;
        }

        if (quality.Reference.PublicationYear.HasValue
            && best.Candidate.PublicationYear.HasValue
            && quality.Reference.PublicationYear.Value != best.Candidate.PublicationYear.Value)
        {
            return null;
        }

        return DoiValidationResult.FromCrossrefTitleSearch(
            NormalizeDoi(best.Candidate.Doi)!,
            best.Candidate,
            best.Similarity,
            JsonSerializer.Serialize(new
            {
                source = "CROSSREF_TITLE_SEARCH",
                confidence = "HIGH",
                titleSimilarity = best.Similarity,
                candidate = best.Candidate
            }, JsonOptions));
    }

    private static DoiValidationResult ApplyMetadataComparison(DoiValidationResult result, string? expectedTitle, int? expectedYear)
    {
        var titleSimilarity = TitleSimilarity(expectedTitle, result.MatchedTitle);
        var hasTitleMismatch = !string.IsNullOrWhiteSpace(expectedTitle)
            && !string.IsNullOrWhiteSpace(result.MatchedTitle)
            && titleSimilarity < TitleMismatchThreshold;
        var yearMatched = expectedYear.HasValue && result.MatchedYear.HasValue
            ? expectedYear.Value == result.MatchedYear.Value
            : (bool?)null;

        return result with
        {
            Status = hasTitleMismatch ? DoiValidationStatus.METADATA_MISMATCH : DoiValidationStatus.VALID,
            TitleSimilarity = titleSimilarity,
            YearMatched = yearMatched,
            IssueCode = hasTitleMismatch ? "DOI_TITLE_MISMATCH" : result.IssueCode,
            IssueMessage = hasTitleMismatch ? "DOI metadata title does not match extracted title." : result.IssueMessage
        };
    }

    private static void ApplyMainResult(PaperDoiCheck check, string? doi, DoiValidationResult result)
    {
        check.MainDoi = NormalizeDoi(doi);
        check.MainDoiStatus = result.Status;
        check.MainDoiTitleSimilarity = result.TitleSimilarity;
        check.MainDoiYearMatched = result.YearMatched;
        check.MainDoiMatchedTitle = result.MatchedTitle;
        check.MainDoiMatchedPublisher = result.MatchedPublisher;
        check.MainDoiIssueCode = result.IssueCode;
        check.MainDoiIssueMessage = result.IssueMessage;
        check.MainDoiRawJson = result.RawJson;
    }

    private static PaperReferenceDoiCheck ToReferenceCheck(
        int ordinal,
        ReferenceDto originalReference,
        ReferenceQualityResult quality,
        DoiValidationResult result,
        DateTime now) => new()
    {
        ReferenceOrdinal = ordinal,
        RawText = originalReference.RawText,
        ExtractedDoi = NormalizeDoi(originalReference.Doi),
        ExtractedTitle = originalReference.Title,
        ExtractedYear = originalReference.PublicationYear,
        NormalizedTitle = quality.NormalizedTitle,
        NormalizedDoi = quality.NormalizedDoi,
        NormalizedJournal = quality.NormalizedJournal,
        NormalizedYear = quality.NormalizedYear,
        DoiFormatValid = quality.DoiFormatValid,
        ValidationSource = result.ValidationSource,
        ValidationStatus = result.Status,
        MatchedDoi = result.MatchedDoi,
        MatchedTitle = result.MatchedTitle,
        MatchedPublisher = result.MatchedPublisher,
        MatchedYear = result.MatchedYear,
        TitleSimilarity = result.TitleSimilarity,
        YearMatched = result.YearMatched,
        IssueCode = result.IssueCode,
        IssueMessage = result.IssueMessage,
        MetadataCompletenessScore = quality.MetadataCompletenessScore,
        ParseConfidenceScore = quality.ParseConfidenceScore,
        IssueCodesJson = JsonSerializer.Serialize(quality.IssueCodes, JsonOptions),
        RawJson = string.IsNullOrWhiteSpace(result.RawJson) ? "{}" : result.RawJson,
        CheckedAt = now
    };

    private void ApplySummary(PaperDoiCheck check, IReadOnlyList<ReferenceQualityResult> qualityResults)
    {
        var references = check.ReferenceChecks.ToArray();
        check.ReferencesWithDoi = references.Count(x => !string.IsNullOrWhiteSpace(x.NormalizedDoi ?? x.ExtractedDoi));
        check.ReferencesMissingDoi = references.Count(x => x.ValidationStatus == DoiValidationStatus.MISSING);
        check.ValidReferenceDois = references.Count(x => x.ValidationStatus == DoiValidationStatus.VALID);
        check.InvalidReferenceDois = references.Count(x => x.ValidationStatus is DoiValidationStatus.INVALID_FORMAT or DoiValidationStatus.NOT_FOUND or DoiValidationStatus.SERVICE_ERROR);
        check.ReferenceDoiTitleMismatches = references.Count(x => x.ValidationStatus == DoiValidationStatus.METADATA_MISMATCH);
        check.MissingReferenceAuthors = qualityResults.Count(x => x.IssueCodes.Contains("REFERENCE_MISSING_AUTHOR"));
        check.MissingReferenceVenues = qualityResults.Count(x => x.IssueCodes.Contains("REFERENCE_MISSING_VENUE"));
        check.LowConfidenceReferences = qualityResults.Count(x => x.IssueCodes.Contains("REFERENCE_AUTHOR_LOW_CONFIDENCE"));
        check.DuplicateReferences = CountDuplicateReferences(references);
        check.ReferenceCleanlinessIssues = qualityResults.Sum(x => x.IssueCodes.Count(issue =>
            issue is "REFERENCE_TITLE_POLLUTED" or "REFERENCE_JOURNAL_SUSPECT" or "REFERENCE_AUTHOR_LOW_CONFIDENCE" or "REFERENCE_PAGES_SUSPECT" or "REFERENCE_BOUNDARY_SUSPECT"));
        check.ReferenceDoiCoveragePercent = check.TotalReferences == 0
            ? 0
            : Math.Round(check.ReferencesWithDoi * 100.0 / check.TotalReferences, 2);
        check.ReferenceQualityScore = referenceQuality.CalculateReferenceQualityScore(
            qualityResults,
            check.ValidReferenceDois,
            check.InvalidReferenceDois,
            check.ReferenceDoiTitleMismatches);
        check.OverallScore = CalculateOverallScore(
            check.MainDoiStatus,
            check.MainDoiStatus == DoiValidationStatus.METADATA_MISMATCH,
            check.ReferenceDoiCoveragePercent,
            check.InvalidReferenceDois,
            check.ReferenceDoiTitleMismatches);
        check.RiskLevel = RiskForScore(check.OverallScore);
    }

    private static IReadOnlyList<ReferenceDto> DeserializeReferences(string? referencesJson)
    {
        if (string.IsNullOrWhiteSpace(referencesJson))
        {
            return Array.Empty<ReferenceDto>();
        }

        try
        {
            return JsonSerializer.Deserialize<IReadOnlyList<ReferenceDto>>(referencesJson, JsonOptions) ?? Array.Empty<ReferenceDto>();
        }
        catch (JsonException)
        {
            return Array.Empty<ReferenceDto>();
        }
    }

    private static string BuildSummaryJson(PaperDoiCheck check) =>
        JsonSerializer.Serialize(new
        {
            check.MainDoiStatus,
            check.TotalReferences,
            check.ReferencesWithDoi,
            check.ReferencesMissingDoi,
            check.ValidReferenceDois,
            check.InvalidReferenceDois,
            check.ReferenceDoiTitleMismatches,
            check.ReferenceDoiCoveragePercent,
            check.MissingReferenceAuthors,
            check.MissingReferenceVenues,
            check.LowConfidenceReferences,
            check.DuplicateReferences,
            check.ReferenceCleanlinessIssues,
            check.ReferenceQualityScore,
            check.OverallScore,
            check.RiskLevel
        }, JsonOptions);

    private static PaperDoiCheckResponse ToResponse(PaperDoiCheck check) => new()
    {
        PaperId = check.PaperId,
        PaperMetadataId = check.PaperMetadataId,
        PaperVersionId = check.PaperVersionId,
        MainDoi = check.MainDoi,
        MainDoiStatus = check.MainDoiStatus,
        MainDoiTitleSimilarity = check.MainDoiTitleSimilarity,
        MainDoiYearMatched = check.MainDoiYearMatched,
        MainDoiMatchedTitle = check.MainDoiMatchedTitle,
        MainDoiMatchedPublisher = check.MainDoiMatchedPublisher,
        MainDoiIssueCode = check.MainDoiIssueCode,
        MainDoiIssueMessage = check.MainDoiIssueMessage,
        TotalReferences = check.TotalReferences,
        ReferencesWithDoi = check.ReferencesWithDoi,
        ReferencesMissingDoi = check.ReferencesMissingDoi,
        ValidReferenceDois = check.ValidReferenceDois,
        InvalidReferenceDois = check.InvalidReferenceDois,
        ReferenceDoiTitleMismatches = check.ReferenceDoiTitleMismatches,
        ReferenceDoiCoveragePercent = check.ReferenceDoiCoveragePercent,
        MissingReferenceAuthors = check.MissingReferenceAuthors,
        MissingReferenceVenues = check.MissingReferenceVenues,
        LowConfidenceReferences = check.LowConfidenceReferences,
        DuplicateReferences = check.DuplicateReferences,
        ReferenceCleanlinessIssues = check.ReferenceCleanlinessIssues,
        ReferenceQualityScore = check.ReferenceQualityScore,
        OverallScore = check.OverallScore,
        RiskLevel = check.RiskLevel,
        Status = check.Status,
        ErrorMessage = check.ErrorMessage,
        CheckedAt = check.CheckedAt,
        ReferenceChecks = check.ReferenceChecks
            .OrderBy(x => x.ReferenceOrdinal)
            .Select(x => new PaperReferenceDoiCheckResponse
            {
                ReferenceOrdinal = x.ReferenceOrdinal,
                RawText = x.RawText,
                ExtractedDoi = x.ExtractedDoi,
                ExtractedTitle = x.ExtractedTitle,
                ExtractedYear = x.ExtractedYear,
                NormalizedTitle = x.NormalizedTitle,
                NormalizedDoi = x.NormalizedDoi,
                NormalizedJournal = x.NormalizedJournal,
                NormalizedYear = x.NormalizedYear,
                DoiFormatValid = x.DoiFormatValid,
                ValidationSource = x.ValidationSource,
                ValidationStatus = x.ValidationStatus,
                MatchedDoi = x.MatchedDoi,
                MatchedTitle = x.MatchedTitle,
                MatchedPublisher = x.MatchedPublisher,
                MatchedYear = x.MatchedYear,
                TitleSimilarity = x.TitleSimilarity,
                YearMatched = x.YearMatched,
                IssueCode = x.IssueCode,
                IssueMessage = x.IssueMessage,
                MetadataCompletenessScore = x.MetadataCompletenessScore,
                ParseConfidenceScore = x.ParseConfidenceScore,
                IssueCodes = DeserializeStringArray(x.IssueCodesJson),
                CheckedAt = x.CheckedAt
            })
            .ToArray()
    };

    private static int CountDuplicateReferences(IReadOnlyList<PaperReferenceDoiCheck> references)
    {
        return references
            .Select(x => x.NormalizedDoi ?? x.NormalizedTitle)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .GroupBy(x => x!, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Sum(group => group.Count() - 1);
    }

    private static IReadOnlyList<string> DeserializeStringArray(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<string>();
        }

        try
        {
            return JsonSerializer.Deserialize<IReadOnlyList<string>>(json, JsonOptions) ?? Array.Empty<string>();
        }
        catch (JsonException)
        {
            return Array.Empty<string>();
        }
    }

    private static string? NormalizeDoi(string? doi)
    {
        if (string.IsNullOrWhiteSpace(doi))
        {
            return null;
        }

        var value = doi.Trim();
        foreach (var prefix in new[] { "https://doi.org/", "http://doi.org/", "doi:" })
        {
            if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                value = value[prefix.Length..];
                break;
            }
        }

        return value.Trim().TrimEnd('.').ToLowerInvariant();
    }

    private static string NormalizeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var c in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : ' ');
        }

        return Regex.Replace(builder.ToString(), @"\s+", " ").Trim();
    }

    private static bool IsMeaningfulPrefix(string left, string right)
    {
        var shorter = left.Length <= right.Length ? left : right;
        var longer = left.Length <= right.Length ? right : left;
        return shorter.Length >= 12
            && longer.StartsWith(shorter, StringComparison.OrdinalIgnoreCase);
    }

    private static double TokenJaccard(string left, string right)
    {
        var leftTokens = left.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var rightTokens = right.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (leftTokens.Count == 0 || rightTokens.Count == 0)
        {
            return 0;
        }

        var intersection = leftTokens.Intersect(rightTokens, StringComparer.OrdinalIgnoreCase).Count();
        var union = leftTokens.Union(rightTokens, StringComparer.OrdinalIgnoreCase).Count();
        return union == 0 ? 0 : intersection * 1.0 / union;
    }

    private static int LevenshteinDistance(string a, string b)
    {
        var matrix = new int[a.Length + 1, b.Length + 1];
        for (var i = 0; i <= a.Length; i++)
        {
            matrix[i, 0] = i;
        }

        for (var j = 0; j <= b.Length; j++)
        {
            matrix[0, j] = j;
        }

        for (var i = 1; i <= a.Length; i++)
        {
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                matrix[i, j] = Math.Min(
                    Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost);
            }
        }

        return matrix[a.Length, b.Length];
    }

    private sealed record DoiValidationResult(
        DoiValidationStatus Status,
        string? ValidationSource,
        string? MatchedDoi,
        string? MatchedTitle,
        string? MatchedPublisher,
        int? MatchedYear,
        double? TitleSimilarity,
        bool? YearMatched,
        string? IssueCode,
        string? IssueMessage,
        string RawJson)
    {
        public static DoiValidationResult Issue(DoiValidationStatus status, string issueCode, string issueMessage, string? doi = null) =>
            new(status, null, doi, null, null, null, null, null, issueCode, issueMessage, "{}");

        public static DoiValidationResult FromCrossref(string requestedDoi, PublicationQualitySystem.Application.DTOs.Crossref.CrossrefMetadataResponse response, string rawJson) =>
            new(DoiValidationStatus.VALID, "Crossref", NormalizeDoi(response.Doi) ?? requestedDoi, response.Title, response.Publisher ?? response.Journal, response.PublicationYear, null, null, null, null, rawJson);

        public static DoiValidationResult FromCrossrefTitleSearch(string recoveredDoi, PublicationQualitySystem.Application.DTOs.Crossref.CrossrefMetadataResponse response, double titleSimilarity, string rawJson) =>
            new(DoiValidationStatus.VALID, "CROSSREF_TITLE_SEARCH", NormalizeDoi(response.Doi) ?? recoveredDoi, response.Title, response.Publisher ?? response.Journal, response.PublicationYear, titleSimilarity, null, null, null, rawJson);

        public static DoiValidationResult FromOpenAlex(string requestedDoi, PublicationQualitySystem.Application.DTOs.OpenAlex.OpenAlexWorkDto response) =>
            new(DoiValidationStatus.VALID, "OpenAlex", NormalizeDoi(response.Doi) ?? requestedDoi, response.Title, response.Journal, response.PublicationYear, null, null, null, null, string.IsNullOrWhiteSpace(response.RawJson) ? "{}" : response.RawJson);
    }
}
