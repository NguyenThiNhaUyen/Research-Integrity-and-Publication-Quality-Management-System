# Paper Processing Event Identity

`paper_processing_events` is the workflow history table. Each row describes one processing event, while `paper_processing_trackers` stores the current workflow snapshot.

## Identity Fields

- `EventId`: internal unique identity for one event-history row. It is generated with `Guid.NewGuid().ToString("N")`, is never null, and is unique across `paper_processing_events`.
- `CorrelationId`: workflow trace id copied from the owning `PaperProcessingTracker`. All events for one paper-processing workflow share this value.
- `TrackerId`: foreign key to the tracker snapshot row. It repeats across all events for the same tracker.
- `PaperId`: paper aggregate id.
- `PaperVersionId`: uploaded version and processing-scope id.

`EventId` is not `TrackerId`, `PaperId`, `PaperVersionId`, an outbox id, or a Kafka/integration event id. External event ids may be stored in `payload_json` as `integrationEventId`.

## Audit Queries

```sql
SELECT
    id,
    event_id,
    correlation_id,
    tracker_id,
    paper_id,
    paper_version_id,
    event_type,
    stage,
    status,
    payload_json,
    created_at
FROM paper_processing_events
ORDER BY id ASC;
```

```sql
SELECT *
FROM paper_processing_events
WHERE event_id IS NULL
   OR event_id = ''
   OR event_id ~ '^[0-9]+$'
   OR correlation_id IS NULL
   OR correlation_id = ''
   OR payload_json IS NULL
   OR event_type IS NULL
   OR event_type = ''
   OR stage IS NULL
   OR status IS NULL;
```

```sql
SELECT event_id, COUNT(*)
FROM paper_processing_events
GROUP BY event_id
HAVING COUNT(*) > 1;
```
