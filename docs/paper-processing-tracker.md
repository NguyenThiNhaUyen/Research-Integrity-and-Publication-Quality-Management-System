# Paper Processing Tracker

## Purpose

`PaperProcessingTracker` stores the latest operational state for a paper processing workflow. It is designed for frontend progress UI and support dashboards.

`AuditLog` remains the historical audit trail. The tracker is not append-only; it keeps one current state row per `PaperVersionId`.

## Entity Summary

The tracker belongs to both `Paper` and `PaperVersion`.

Important fields:

- `paperId`
- `paperVersionId`
- `correlationId`
- `overallStatus`
- `currentStep`
- `progressPercent`
- step statuses for upload, markdown, metadata extraction, Crossref, metadata quality, OpenAlex, AI review, and integrity screening
- event ids for uploaded, metadata quality scored, OpenAlex requested, and OpenAlex skipped
- last error and retry fields
- timestamps for started/completed/failed steps
- `stepDetailsJson` and `warningsJson`

Indexes:

- unique `paper_version_id`
- `paper_id`
- `correlation_id`
- `overall_status`

## Status Meaning

Overall status:

- `Pending`: tracker exists before meaningful work starts.
- `Processing`: required work is running or waiting on an event.
- `Completed`: critical steps are complete and optional steps are completed or skipped.
- `Failed`: critical step failed.
- `PartiallyCompleted`: optional/enrichment step failed.
- `Cancelled`: reserved for future cancellation support.

Step status:

- `NotStarted`
- `EventPublished`
- `Processing`
- `Completed`
- `Failed`
- `Skipped`
- `RetryScheduled`

## Event Update Points

- Upload creates tracker and marks upload completed.
- Paper upload outbox publish is recorded as `PaperUploadedEventId`.
- OCR worker marks Markdown conversion started/completed/failed.
- Metadata worker marks metadata extraction started/completed/failed.
- Metadata worker marks Crossref enrichment completed or skipped.
- Metadata worker marks metadata quality scoring completed and records `MetadataQualityScoredEventId`.
- OpenAlex gate marks OpenAlex event published or skipped.
- OpenAlex worker marks OpenAlex started/completed/failed.

## Progress Rules

- Upload completed: `10`
- Markdown completed: `35`
- Metadata extraction completed: `60`
- Metadata quality completed: `75`
- OpenAlex completed/skipped: `85`
- AI review completed/skipped: `95`
- All required work done: `100`

## Overall Status Rules

Critical failures set `Failed`:

- Upload
- Metadata extraction

Optional/enrichment failures set `PartiallyCompleted`:

- Markdown conversion
- Crossref enrichment
- OpenAlex similarity check
- AI publication quality review
- Integrity screening

Completed means:

- upload completed
- metadata extraction completed
- metadata quality scoring completed
- OpenAlex completed or skipped

## API Examples

Get tracker by paper:

```http
GET /api/papers/{paperId}/processing-tracker
```

Get tracker by paper version:

```http
GET /api/paper-versions/{paperVersionId}/processing-tracker
```

Example response:

```json
{
  "paperId": 1,
  "paperVersionId": 1,
  "overallStatus": "Processing",
  "currentStep": "MarkdownConversion",
  "progressPercent": 35,
  "lastErrorCode": null,
  "lastErrorMessage": null,
  "steps": [
    {
      "name": "Upload",
      "step": "Upload",
      "status": "Completed",
      "startedAt": "2026-06-04T00:00:00Z",
      "completedAt": "2026-06-04T00:00:02Z",
      "errorMessage": null
    },
    {
      "name": "Markdown Conversion",
      "step": "MarkdownConversion",
      "status": "Processing",
      "startedAt": "2026-06-04T00:00:05Z",
      "completedAt": null,
      "errorMessage": null
    }
  ]
}
```

## Troubleshooting

- If progress stays at `10`, Kafka may not have delivered upload events to OCR/metadata workers.
- If metadata extraction is `Failed`, check GROBID/S3 logs and `lastErrorMessage`.
- If OpenAlex is `Skipped`, metadata quality gate likely failed.
- If OpenAlex is `Failed`, metadata extraction can still be completed; this is an enrichment failure.
- If duplicate Kafka events arrive, tracker updates are idempotent and do not downgrade completed steps.

## Safety Notes

Do not store raw API keys, full markdown, raw PDF text, or sensitive payloads in `stepDetailsJson`, `errorDetailsJson`, or `warningsJson`.
