# DOI Integrity Flow

## Purpose

The DOI integrity check verifies both:

- the main paper DOI from `PaperMetadata.Doi`
- every extracted reference DOI from `PaperMetadata.ReferencesJson`

This flow is an additional integrity signal. It does not replace metadata quality scoring and does not change the existing OpenAlex similarity check.

## Processing Order

```text
PaperUploadedIntegrationEvent
  -> Metadata Worker
      -> GROBID metadata/reference extraction
      -> Crossref enrichment for main extracted DOI
      -> Save PaperMetadata
      -> DoiCheckStarted event
      -> Validate main paper DOI
      -> MainDoiChecked event
      -> Validate each reference DOI from ReferencesJson
      -> ReferenceDoiCheckCompleted event
      -> DoiCheckCompleted event
      -> MetadataQualityScoringService.Calculate
      -> Publish MetadataQualityScoredIntegrationEvent
      -> OpenAlex gate / similarity flow continues unchanged
```

If the DOI integrity check fails because of a service/runtime error, the pipeline records `DoiCheckFailed` and continues to metadata quality scoring. DOI integrity must not block OpenAlex similarity.

## Main DOI Check

Source:

- `PaperMetadata.Doi`

Rules:

- Missing DOI is recorded as `MAIN_DOI_MISSING`.
- Invalid DOI format is recorded as `INVALID_FORMAT`.
- Valid-format DOI is checked against Crossref first.
- If Crossref cannot validate it, OpenAlex is used as fallback.
- Returned title is compared with `PaperMetadata.Title`.
- Returned publication year is compared with `PaperMetadata.PublicationYear` when both exist.

Stored on `paper_doi_checks`:

- `main_doi`
- `main_doi_status`
- `main_doi_title_similarity`
- `main_doi_year_matched`
- `main_doi_matched_title`
- `main_doi_matched_publisher`
- `main_doi_issue_code`
- `main_doi_issue_message`
- `main_doi_raw_json`

## Reference DOI Check

Source:

- `PaperMetadata.ReferencesJson`

There is no separate reference table today, so each detail row uses `reference_ordinal` to identify the reference position inside `ReferencesJson`.

Rules for each reference:

- Missing DOI is recorded as `REFERENCE_DOI_MISSING`.
- Invalid DOI format is recorded as `INVALID_FORMAT` and external APIs are not called.
- Valid-format DOI is checked against Crossref first.
- If Crossref cannot validate it, OpenAlex is used as fallback.
- Returned title is compared with the extracted reference title.
- Returned publication year is compared with the extracted reference year when both exist.

Stored on `paper_reference_doi_checks`:

- `paper_doi_check_id`
- `reference_ordinal`
- `extracted_doi`
- `extracted_title`
- `extracted_year`
- `doi_format_valid`
- `validation_source`
- `validation_status`
- `matched_doi`
- `matched_title`
- `matched_publisher`
- `matched_year`
- `title_similarity`
- `year_matched`
- `issue_code`
- `issue_message`
- `raw_json`
- `checked_at`

## Events

The processing event log records stage-level DOI check events:

- `DoiCheckStarted`
- `MainDoiChecked`
- `ReferenceDoiCheckCompleted`
- `DoiCheckCompleted`
- `DoiCheckFailed`

There is not one event per reference DOI. Per-reference details are stored in `paper_reference_doi_checks` to avoid bloating the processing event stream.

## Scoring

The score starts at `100` and is clamped to `0..100`.

Main DOI penalties:

- missing DOI: `-20`
- invalid/not found/service-error DOI: `-30`
- DOI/title mismatch: `-20`

Reference penalties:

- DOI coverage below `80%` reduces the score.
- DOI coverage below `50%` is high risk.
- invalid reference DOIs reduce the score.
- reference DOI/title mismatches reduce the score.

Risk levels:

- `LOW`: score `>= 80`
- `MEDIUM`: score `60..79`
- `HIGH`: score `< 60`

## API

Full result:

```http
GET /api/papers/{paperId}/doi-check
```

Summary:

```http
GET /api/papers/{paperId}/doi-check/summary
```

## Tables

- `paper_doi_checks`: one summary row per latest DOI integrity run for a paper metadata row.
- `paper_reference_doi_checks`: detail rows for the references from `ReferencesJson`.

## Non-Blocking Behavior

DOI integrity issues are stored as findings and scores. External API failures are recorded as `SERVICE_ERROR`, but the metadata quality and OpenAlex similarity pipeline continues.
