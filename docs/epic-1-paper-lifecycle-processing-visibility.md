# Epic 1 - Paper Lifecycle & Processing Visibility

Date: 2026-06-04

Status: Implemented

## Goal

Expose the paper lifecycle and processing result data that already exists in the backend so Researcher, Reviewer, Lab Leader, and Admin users can inspect uploaded papers from FE/Postman.

This epic does not add new business domains. It only exposes data already produced by:

- Upload Service
- OCR Worker
- Metadata Worker
- Metadata Quality Worker
- OpenAlex Worker
- Processing Tracker
- Audit Log

## Implemented Endpoints

| Feature | Method | Endpoint | Response |
|---|---|---|---|
| Paper list | GET | `/api/papers?page=0&size=20&search=&sortDescending=true` | `BaseResponse<IReadOnlyList<PaperResponse>>` |
| Paper detail | GET | `/api/papers/{paperId}` | `BaseResponse<PaperResponse>` |
| Paper versions | GET | `/api/papers/{paperId}/versions` | `BaseResponse<IReadOnlyList<PaperVersionResponse>>` |
| Paper version detail | GET | `/api/paper-versions/{versionId}` | `BaseResponse<PaperVersionResponse>` |
| Processing tracker | GET | `/api/paper-versions/{versionId}/tracker` | `BaseResponse<PaperProcessingTrackerResponse>` |
| Metadata quality | GET | `/api/paper-versions/{versionId}/metadata-quality` | `BaseResponse<MetadataQualityResponse>` |
| OpenAlex similarity | GET | `/api/paper-versions/{versionId}/similarity` | `BaseResponse<SimilarityResultResponse>` |
| Audit logs | GET | `/api/paper-versions/{versionId}/audit-logs` | `BaseResponse<IReadOnlyList<AuditLogResponse>>` |

Existing compatibility endpoints are preserved:

- `GET /api/papers/{paperId}/metadata`
- `GET /api/papers/{paperId}/audit-logs`
- `GET /api/papers/{paperId}/processing-progress`
- `GET /api/papers/{paperId}/processing-tracker`
- `GET /api/paper-versions/{paperVersionId}/processing-tracker`

## Application Layer

Added DTOs:

- `PaperResponse`
- `PaperVersionSummaryResponse`
- `MetadataQualityResponse`
- `SimilarityResultResponse`
- `SimilarityMatchedPaperResponse`

Added repository interfaces:

- `IPaperRepository`
- `IPaperVersionRepository`
- `IProcessingTrackerRepository`
- `IMetadataQualityRepository`
- `IOpenAlexRepository`
- `IAuditLogRepository`

Added service interfaces:

- `IPaperVersionService`
- `IProcessingTrackerService`
- `IMetadataQualityService`

Extended existing service interfaces:

- `IPaperService`
- `IOpenAlexService`

Added manual mappers:

- `PaperMapper`
- `PaperVersionMapper`
- `MetadataQualityMapper`
- `SimilarityResultMapper`
- `AuditLogMapper`

## Infrastructure Layer

Added repositories:

- `PaperRepository`
- `PaperVersionRepository`
- `ProcessingTrackerRepository`
- `MetadataQualityRepository`
- `OpenAlexRepository`
- `AuditLogRepository`

Added services:

- `PaperVersionService`
- `ProcessingTrackerService`
- `MetadataQualityService`

Extended services:

- `PaperService`
- `OpenAlexService`
- `AuditLogService`

Updated DI:

- `RepositoryConfiguration`
- `DependencyInjection`

## API Layer

Added controllers:

- `ProcessingTrackerController`
- `MetadataQualityController`
- `OpenAlexController`
- `AuditLogController`

Updated controllers:

- `PaperController`
- `PaperVersionController`

## Authorization Behavior

Read APIs preserve the current permission style:

- Users with `PAPER_READ_ALL` or `AUDIT_LOG_READ` can read all papers.
- Paper owners can read their own papers and related versions/results.
- Other users receive `AppException(AuditLogErrorCode.Forbidden)`.

The global fallback authorization policy still requires authenticated users.

## Notes

Metadata quality and OpenAlex similarity are currently resolved through `PaperVersion -> Paper -> PaperMetadata` because the existing schema stores one `PaperMetadata` row per paper, not per paper version.

No database migration was added because this epic reuses existing entities and tables.

## Tests

Added focused tests in:

- `PublicationQualitySystem.Infrastructure.Tests/Epic1QueryApiTests.cs`

Updated:

- `OpenAlexServiceTests.cs`

Verification:

```text
dotnet build PublicationQualitySystem.sln --no-restore
Build succeeded.

dotnet test PublicationQualitySystem.sln --no-build
Passed: 45/45
```

## Definition Of Done Status

| Requirement | Status |
|---|---|
| Researcher can view papers | Done |
| Researcher can view paper versions | Done |
| Researcher can view processing status | Done |
| Researcher can view metadata quality | Done |
| Researcher can view similarity result | Done |
| Administrator can view audit logs | Done |
| Swagger sees endpoints through controllers | Done |
| Build passes | Done |
| Migration passes | Not applicable, no schema change |
| Tests pass | Done |
