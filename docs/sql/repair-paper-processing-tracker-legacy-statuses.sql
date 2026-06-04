-- Repair PaperProcessingTracker legacy snapshot columns from paper_processing_events.
-- paper_processing_events is authoritative; legacy columns are kept for old UI compatibility.
-- Safe to rerun.

BEGIN;

WITH event_flags AS (
    SELECT
        e.tracker_id,
        bool_or(e.event_type = 'NougatMarkdownConversionCompleted') AS markdown_completed,
        bool_or(e.event_type = 'NougatMarkdownConversionSkipped') AS markdown_skipped,
        bool_or(e.event_type = 'NougatMarkdownConversionFailed' OR (e.status = 'FAILED' AND e.event_type ILIKE '%Nougat%')) AS markdown_failed,
        bool_or(e.event_type = 'NougatMarkdownConversionStarted') AS markdown_started,

        bool_or(e.event_type = 'GrobidMetadataExtractionCompleted') AS metadata_completed,
        bool_or(e.event_type = 'GrobidMetadataExtractionSkipped') AS metadata_skipped,
        bool_or(e.event_type = 'GrobidMetadataExtractionFailed' OR (e.status = 'FAILED' AND e.event_type ILIKE '%Grobid%')) AS metadata_failed,
        bool_or(e.event_type = 'GrobidMetadataExtractionStarted') AS metadata_started,

        bool_or(e.event_type = 'CrossrefEnrichmentCompleted') AS crossref_completed,
        bool_or(e.event_type = 'CrossrefEnrichmentSkipped') AS crossref_skipped,
        bool_or(e.event_type = 'CrossrefEnrichmentFailed' OR (e.status = 'FAILED' AND e.event_type ILIKE '%Crossref%')) AS crossref_failed,
        bool_or(e.event_type = 'CrossrefEnrichmentStarted') AS crossref_started,

        bool_or(e.event_type = 'MetadataQualityScoringCompleted') AS quality_completed,
        bool_or(e.event_type = 'MetadataQualityScoringSkipped') AS quality_skipped,
        bool_or(e.event_type = 'MetadataQualityScoringFailed' OR (e.status = 'FAILED' AND e.event_type ILIKE '%Quality%')) AS quality_failed,
        bool_or(e.event_type = 'MetadataQualityScoringStarted') AS quality_started,

        bool_or(e.event_type = 'OpenAlexSimilarityCheckCompleted') AS openalex_completed,
        bool_or(e.event_type = 'OpenAlexSimilarityCheckSkipped') AS openalex_skipped,
        bool_or(e.event_type = 'OpenAlexSimilarityCheckFailed' OR (e.status = 'FAILED' AND e.event_type ILIKE '%OpenAlex%')) AS openalex_failed,
        bool_or(e.event_type = 'OpenAlexSimilarityCheckStarted') AS openalex_started,

        bool_or(e.event_type = 'PaperProcessingCompleted') AS processing_completed
    FROM paper_processing_events e
    WHERE coalesce(e."Deleted", false) = false
    GROUP BY e.tracker_id
)
UPDATE paper_processing_trackers t
SET
    markdown_status = CASE
        WHEN f.markdown_failed THEN 'Failed'
        WHEN f.markdown_completed THEN 'Completed'
        WHEN f.markdown_skipped THEN 'Skipped'
        WHEN f.markdown_started THEN 'Processing'
        ELSE t.markdown_status
    END,
    metadata_extraction_status = CASE
        WHEN f.metadata_failed THEN 'Failed'
        WHEN f.metadata_completed THEN 'Completed'
        WHEN f.metadata_skipped THEN 'Skipped'
        WHEN f.metadata_started THEN 'Processing'
        ELSE t.metadata_extraction_status
    END,
    crossref_status = CASE
        WHEN f.crossref_failed THEN 'Failed'
        WHEN f.crossref_completed THEN 'Completed'
        WHEN f.crossref_skipped THEN 'Skipped'
        WHEN f.crossref_started THEN 'Processing'
        ELSE t.crossref_status
    END,
    metadata_quality_status = CASE
        WHEN f.quality_failed THEN 'Failed'
        WHEN f.quality_completed THEN 'Completed'
        WHEN f.quality_skipped THEN 'Skipped'
        WHEN f.quality_started THEN 'Processing'
        ELSE t.metadata_quality_status
    END,
    openalex_status = CASE
        WHEN f.openalex_failed THEN 'Failed'
        WHEN f.openalex_completed THEN 'Completed'
        WHEN f.openalex_skipped THEN 'Skipped'
        WHEN f.openalex_started THEN 'Processing'
        ELSE t.openalex_status
    END,
    current_stage = CASE
        WHEN f.processing_completed THEN 'COMPLETED'
        ELSE t.current_stage
    END,
    current_status = CASE
        WHEN f.processing_completed THEN 'COMPLETED'
        ELSE t.current_status
    END,
    overall_status = CASE
        WHEN f.processing_completed THEN 'COMPLETED'
        ELSE t.overall_status
    END,
    progress_percent = CASE
        WHEN f.processing_completed THEN 100
        ELSE t.progress_percent
    END,
    current_step = CASE
        WHEN f.processing_completed THEN 'Completed'
        WHEN f.openalex_completed OR f.openalex_skipped THEN 'OpenAlex Completed'
        WHEN f.openalex_started THEN 'OpenAlex Requested'
        WHEN f.quality_completed OR f.quality_skipped THEN 'Quality Scoring Completed'
        WHEN f.quality_started THEN 'Quality Scoring Requested'
        WHEN f.metadata_completed OR f.metadata_skipped THEN 'Metadata Completed'
        WHEN f.metadata_started THEN 'Metadata Requested'
        WHEN f.markdown_completed OR f.markdown_skipped THEN 'OCR Completed'
        WHEN f.markdown_started THEN 'OCR Requested'
        ELSE t.current_step
    END,
    last_updated_at = now(),
    "UpdatedAt" = now()
FROM event_flags f
WHERE f.tracker_id = t."Id";

COMMIT;
