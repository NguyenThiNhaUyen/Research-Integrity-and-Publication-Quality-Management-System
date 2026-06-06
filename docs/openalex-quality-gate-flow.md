# OpenAlex Quality Gate Flow

## Current Problem

OpenAlex similarity checks must run only after scholarly metadata has been extracted and quality-scored. The OCR/Markdown flow is responsible only for converting PDF content to Markdown and must not make metadata quality or OpenAlex decisions.

## New Event Flow

```text
PaperUploaded
  -> OCR Worker
      -> Nougat PDF to Markdown
      -> Save Markdown S3 key
      -> MarkdownGenerated

PaperUploaded
  -> Metadata Worker
      -> GROBID/Crossref metadata extraction
      -> Save PaperMetadata
      -> DOI integrity check
          -> Validate main paper DOI
          -> Validate reference DOIs from ReferencesJson
          -> Save PaperDoiCheck and PaperReferenceDoiCheck rows
      -> MetadataQualityScoringService.Calculate
      -> Persist MetadataQuality fields
      -> Publish MetadataQualityScoredIntegrationEvent

MetadataQualityScoredIntegrationEvent
  -> OpenAlex Gate Worker
      -> Load persisted PaperMetadata
      -> If MetadataQualityCanProceed == true:
           Publish OpenAlexSimilarityCheckRequestedIntegrationEvent
      -> Else:
           Persist skipped PaperSimilarityCheck
           Publish OpenAlexSimilarityCheckSkippedIntegrationEvent

OpenAlexSimilarityCheckRequestedIntegrationEvent
  -> OpenAlex Worker
      -> Load persisted PaperMetadata
      -> DOI lookup first, title search fallback
      -> Save PaperSimilarityCheck
```

## Event Contracts

### MetadataQualityScoredIntegrationEvent

- `eventId`
- `paperId`
- `paperVersionId`
- `paperMetadataId`
- `totalScore`
- `coreScore`
- `grade`
- `canProceed`
- `correlationId`
- `occurredAt`

Topic: `ripqms.metadata-quality-scored.v1`

### OpenAlexSimilarityCheckRequestedIntegrationEvent

- `eventId`
- `paperId`
- `paperVersionId`
- `paperMetadataId`
- `triggerReason`
- `correlationId`
- `occurredAt`

Topic: `ripqms.openalex-similarity-requested.v1`

### OpenAlexSimilarityCheckSkippedIntegrationEvent

- `eventId`
- `paperId`
- `paperVersionId`
- `paperMetadataId`
- `reason`
- `totalScore`
- `coreScore`
- `grade`
- `missingFieldsJson`
- `warningsJson`
- `correlationId`
- `occurredAt`

Topic: `ripqms.openalex-similarity-skipped.v1`

## Worker Ownership

- OCR worker owns only PDF to Markdown conversion.
- Metadata worker owns GROBID/Crossref extraction, DOI integrity checking, and metadata quality scoring.
- OpenAlex gate worker owns the decision to request or skip OpenAlex.
- OpenAlex worker is the only worker allowed to call `IOpenAlexService`.

## Why OpenAlex Runs After Quality Scoring

OpenAlex matching is useful only when core metadata is trustworthy enough. Running it on sparse or unreliable metadata creates noisy duplicate/similarity results. The quality gate prevents wasteful external calls and records a clear skipped state for the frontend.

## DOI Integrity Before Quality Gate

DOI integrity runs after `PaperMetadata` and `ReferencesJson` are persisted and before metadata quality scoring publishes the gate event. It validates the main DOI first, then validates each extracted reference DOI.

The DOI check emits stage-level processing events:

- `DoiCheckStarted`
- `MainDoiChecked`
- `ReferenceDoiCheckCompleted`
- `DoiCheckCompleted`
- `DoiCheckFailed`

Reference-level details are stored in `paper_reference_doi_checks`, not as one processing event per reference. If DOI validation fails due to an external service/runtime error, the failure is recorded and the OpenAlex gate flow continues.

## Why OCR Must Not Call OpenAlex

Markdown conversion output is not the source of truth for OpenAlex lookup. OpenAlex checks must use persisted `PaperMetadata` so DOI, title, authors, references, and quality scores are consistent and auditable.

## Skip Behavior

When `MetadataQualityCanProceed` is false:

- OpenAlex is not called.
- A `PaperSimilarityCheck` row is saved with:
  - `source = "OpenAlex"`
  - `status = "SKIPPED"`
  - `riskLevel = "SKIPPED"`
  - `overallScore = 0`
  - `skipReason = "Metadata quality gate failed"`
- `OpenAlexSimilarityCheckSkippedIntegrationEvent` is published.

## Idempotency

For each `PaperMetadataId`, only one `OpenAlex` similarity check should exist. If a completed, skipped, failed, or pending check already exists for `Source = OpenAlex`, duplicate gate/request events are ignored.

## Test Checklist

- Metadata worker publishes `MetadataQualityScoredIntegrationEvent` after quality fields are persisted.
- Metadata worker runs DOI integrity check before metadata quality scoring.
- DOI integrity validates main DOI before reference DOIs.
- DOI integrity failure does not block the OpenAlex gate.
- Gate worker publishes requested event when persisted `MetadataQualityCanProceed` is true.
- Gate worker publishes skipped event and persists skipped check when persisted `MetadataQualityCanProceed` is false.
- Gate worker ignores duplicate checks for the same `PaperMetadataId`.
- OpenAlex worker loads persisted `PaperMetadata`, not Markdown.
- OCR worker has no dependency on `IOpenAlexService`.
- OCR worker has no dependency on `IMetadataQualityScoringService`.
- OpenAlex failure saves failed check and does not fail metadata extraction.
