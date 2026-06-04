# RIPQMS Code-To-Mermaid Event Pipeline

This document is derived from the current RIPQMS implementation. It shows the full upload processing lifecycle from PDF upload to OCR, metadata extraction, metadata quality scoring, OpenAlex similarity checking, tracker updates, and audit logging.

## Source Files / Classes Discovered

- API upload entrypoint: `PaperController.Upload`
- Upload orchestration: `PaperService.UploadPaperAsync`
- Outbox publisher: `KafkaOutboxPublisherBackgroundService`
- Upload event: `PaperUploadedIntegrationEvent`
- Kafka configuration: `KafkaOptions`
- OCR worker: `PaperOcrKafkaConsumerBackgroundService`
- OCR service: `INougatService`
- Metadata worker: `PaperMetadataKafkaConsumerBackgroundService`
- Metadata services: `IGrobidService`, `ICrossrefService`
- Metadata quality service: `IMetadataQualityScoringService`
- Metadata quality mapper: `MetadataQualityScoreMapper`
- OpenAlex gate worker: `OpenAlexGateKafkaConsumerBackgroundService`
- OpenAlex worker: `OpenAlexSimilarityKafkaConsumerBackgroundService`
- OpenAlex service: `IOpenAlexService`
- Similarity result entity: `PaperSimilarityCheck`
- Operational tracker: `PaperProcessingTrackerService`
- Historical audit trail: `AuditLogService`

Current Kafka topics:

- `ripqms.paper-uploaded.v1`
- `ripqms.metadata-quality-scored.v1`
- `ripqms.openalex-similarity-requested.v1`
- `ripqms.openalex-similarity-skipped.v1`

## Main Unified Sequence Diagram

```mermaid
sequenceDiagram
    autonumber
    participant PaperController
    participant PaperService
    participant Database
    participant ProcessingTracker
    participant AuditLog
    participant Outbox
    participant Kafka
    participant OCR Worker
    participant Nougat Service
    participant Metadata Worker
    participant GROBID Service
    participant Crossref Service
    participant MetadataQualityScoringService
    participant MetadataQualityScoreMapper
    participant OpenAlex Gate Worker
    participant OpenAlex Worker
    participant OpenAlex API
    participant AI Review Worker [Proposed]
    participant Integrity Screening Worker [Proposed]

    Note over PaperController, Integrity Screening Worker [Proposed]: CorrelationId runs through all events. Tracker is operational state. AuditLog is historical timeline.

    PaperController->>PaperService: process upload request
    PaperService->>PaperService: validate PDF
    PaperService->>Database: save Paper
    PaperService->>Database: save PaperVersion
    PaperService->>Database: save PaperMetadata placeholder
    PaperService->>ProcessingTracker: create PaperProcessingTracker
    PaperService->>PaperService: generate CorrelationId
    PaperService->>PaperService: create PaperUploadedIntegrationEvent
    PaperService->>Outbox: save OutboxMessage
    PaperService->>AuditLog: write AuditLog
    PaperService-->>PaperController: return success

    Outbox->>Kafka: publishes PaperUploadedIntegrationEvent to ripqms.paper-uploaded.v1
    Note over Outbox, Kafka: Event carries: CorrelationId, PaperId, PaperVersionId

    par OCR Branch
        Kafka->>OCR Worker: 
        Note over OCR Worker, MetadataQualityScoringService: OCR does not call quality/OpenAlex
        OCR Worker->>ProcessingTracker: MarkStepStarted MarkdownConversion
        OCR Worker->>Nougat Service: call Nougat
        Nougat Service-->>OCR Worker: 
        OCR Worker->>Database: save markdown result
        OCR Worker->>Database: update PaperVersion
        
        alt OCR Success
            OCR Worker->>ProcessingTracker: MarkStepCompleted
        else OCR Failure
            OCR Worker->>ProcessingTracker: MarkStepFailed
        end
        
        OCR Worker->>AuditLog: write AuditLog
        OCR Worker->>Kafka: [Proposed] publish MarkdownCompletedIntegrationEvent if needed later
        
    and Metadata Branch
        Kafka->>Metadata Worker: 
        Note over Metadata Worker, OpenAlex Worker: Metadata worker does not call OpenAlex directly
        Metadata Worker->>ProcessingTracker: MarkStepStarted MetadataExtraction
        Metadata Worker->>GROBID Service: call GROBID
        GROBID Service-->>Metadata Worker: 
        
        opt DOI exists
            Metadata Worker->>Crossref Service: optional Crossref enrichment
            Crossref Service-->>Metadata Worker: 
        end
        
        Metadata Worker->>Database: save PaperMetadata
        Metadata Worker->>ProcessingTracker: MarkStepCompleted MetadataExtraction
        Metadata Worker->>MetadataQualityScoringService: call MetadataQualityScoringService
        MetadataQualityScoringService-->>Metadata Worker: 
        Metadata Worker->>MetadataQualityScoreMapper: apply MetadataQualityScoreMapper
        MetadataQualityScoreMapper-->>Metadata Worker: 
        Metadata Worker->>Database: save MetadataQuality fields
        Metadata Worker->>ProcessingTracker: MarkStepCompleted MetadataQualityScoring
        Metadata Worker->>Kafka: publish MetadataQualityScoredIntegrationEvent
        Note over Metadata Worker, Kafka: Event carries: CorrelationId, PaperId, PaperVersionId, PaperMetadataId, TotalScore, CoreScore, Grade, CanProceed
    end

    Kafka->>OpenAlex Gate Worker: 
    OpenAlex Gate Worker->>Database: load persisted PaperMetadata
    OpenAlex Gate Worker->>OpenAlex Gate Worker: check MetadataQualityCanProceed
    OpenAlex Gate Worker->>Database: check existing PaperSimilarityCheck for idempotency

    alt CanProceed = false
        OpenAlex Gate Worker->>Database: save PaperSimilarityCheck SKIPPED
        OpenAlex Gate Worker->>Kafka: publish OpenAlexSimilarityCheckSkippedIntegrationEvent
        OpenAlex Gate Worker->>ProcessingTracker: MarkStepSkipped OpenAlexSimilarityCheck
        OpenAlex Gate Worker->>ProcessingTracker: RecalculateOverallStatus
    else CanProceed = true
        OpenAlex Gate Worker->>Database: create PaperSimilarityCheck PENDING
        OpenAlex Gate Worker->>Kafka: publish OpenAlexSimilarityCheckRequestedIntegrationEvent
        OpenAlex Gate Worker->>ProcessingTracker: MarkEventPublished OpenAlexSimilarityCheck
    end

    Kafka->>OpenAlex Worker: 
    Note over OpenAlex Worker, Database: OpenAlex worker uses persisted metadata, not raw markdown
    OpenAlex Worker->>Database: load persisted PaperMetadata from DB
    OpenAlex Worker->>ProcessingTracker: MarkStepStarted OpenAlexSimilarityCheck
    OpenAlex Worker->>OpenAlex API: call OpenAlex API by DOI first
    OpenAlex Worker->>OpenAlex API: fallback to title search if DOI missing
    OpenAlex API-->>OpenAlex Worker: 
    OpenAlex Worker->>OpenAlex Worker: compute similarity score
    
    alt OpenAlex success
        OpenAlex Worker->>Database: save PaperSimilarityCheck COMPLETED
        OpenAlex Worker->>ProcessingTracker: MarkStepCompleted
        OpenAlex Worker->>AuditLog: write AuditLog
        OpenAlex Worker->>Kafka: publish OpenAlexSimilarityCheckCompletedIntegrationEvent [Proposed]
    else OpenAlex failure
        OpenAlex Worker->>Database: save PaperSimilarityCheck FAILED
        OpenAlex Worker->>ProcessingTracker: MarkStepFailed
        OpenAlex Worker->>AuditLog: write AuditLog
        OpenAlex Worker->>Kafka: publish OpenAlexSimilarityCheckFailedIntegrationEvent [Proposed]
        Note over OpenAlex Worker, Metadata Worker: overall status becomes PartiallyCompleted, metadata extraction remains completed
    end

    Note over OpenAlex Worker, Integrity Screening Worker [Proposed]: After OpenAlexCompleted
    Kafka->>AI Review Worker [Proposed]: 
    Kafka->>Integrity Screening Worker [Proposed]: 
```

## Compact Overview

The detailed lifecycle is represented by the sequence diagram above.

## Current Implemented Components

- `PaperController.Upload` receives PDF uploads and delegates to `PaperService.UploadPaperAsync`.
- `PaperService.UploadPaperAsync` validates the file, creates `Paper`, `PaperVersion`, `PaperMetadata` placeholder, audit records, `PaperProcessingTracker`, and `PaperUploadedIntegrationEvent` outbox message.
- `KafkaOutboxPublisherBackgroundService` publishes outbox messages to Kafka.
- `PaperOcrKafkaConsumerBackgroundService` consumes `PaperUploadedIntegrationEvent`, calls Nougat, saves Markdown result, updates `PaperVersion`, `PaperProcessingTracker`, and `AuditLog`.
- `PaperMetadataKafkaConsumerBackgroundService` consumes `PaperUploadedIntegrationEvent`, calls GROBID, optionally calls Crossref, saves `PaperMetadata`, scores metadata quality, persists quality fields, and publishes `MetadataQualityScoredIntegrationEvent`.
- `OpenAlexGateKafkaConsumerBackgroundService` consumes `MetadataQualityScoredIntegrationEvent`, loads persisted metadata, checks `MetadataQualityCanProceed`, and publishes requested or skipped OpenAlex events.
- `OpenAlexSimilarityKafkaConsumerBackgroundService` consumes requested events, loads persisted `PaperMetadata`, calls OpenAlex by DOI first and title fallback, saves `PaperSimilarityCheck`, and updates tracker state.
- `PaperProcessingTrackerService` stores latest operational processing state for UI progress.
- `AuditLogService` stores historical processing events.

## Proposed Components Or Future Split Points

- A separate `MarkdownGenerated` event is not implemented yet. Add it later only if section extraction, AI review, or report generation should react directly to Markdown completion.
- The current upload fan-out uses one `PaperUploadedIntegrationEvent`. Future stricter separation could introduce `ConvertPdfToMarkdownRequested` and `MetadataExtractionRequested`.
- Verify or add a public similarity result endpoint if the frontend needs to read `PaperSimilarityCheck` directly.
- Future AI publication quality review, integrity screening, risk classification, and report generation should follow the same tracker and audit update pattern.

## Recommended Next Implementation Order

1. Verify/finish `PaperProcessingTracker` coverage.
2. Verify/finish `MetadataQualityScoredIntegrationEvent` publication.
3. Verify/finish OpenAlex gate worker behavior.
4. Verify/finish `PaperSimilarityCheck` persistence.
5. Verify/finish OpenAlex worker behavior.
6. Add or expose API endpoints to read tracker and similarity result.
