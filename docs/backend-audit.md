# RIPQMS Backend Audit

Date: 2026-06-04

Scope: documents in `docs/`, `docker-compose.yml`, `postman_collection.json`, and the current backend source under `PublicQualitySystem/`. The older `docs/architecture-review.md` and `docs/api-inventory.md` describe the pre-refactor monolith in several places; current source and `docs/modular-monolith-refactor.md` are treated as authoritative when they conflict.

## Executive Finding

RIPQMS is now a Clean Architecture style .NET 8 backend with a working paper-upload processing backbone: S3 upload, PostgreSQL persistence, audit logs, processing tracker, Kafka outbox publishing, OCR via Nougat, metadata extraction via GROBID/Crossref, metadata quality scoring, OpenAlex quality gate, and OpenAlex similarity persistence.

The system is not yet a complete Research Integrity and Publication Quality Management System. It is strongest in infrastructure and early paper-processing automation. It is weak or missing in COPE governance, authorship verification, conflict/funding/ethics/data-availability workflows, review/submission/publication workflows, dashboards, and publication-quality assessment beyond metadata quality.

## Part 1 - Current Implementation Inventory

| Area | Implemented components | Evidence | Completion |
|---|---|---|---:|
| Architecture | 5-project split: Api, Application, Domain, Infrastructure, Shared; no circular references documented. | `docs/modular-monolith-refactor.md`; `PublicationQualitySystem.sln`; project refs. | 70% |
| Infrastructure | EF Core, Npgsql, AWS/S3/Cognito, HttpClients, Kafka hosted services, seeders. | `Infrastructure/DependencyInjection.cs`, `ApplicationDbContext.cs`, options classes. | 75% |
| Database | 11 DbSets: User, Role, Permission, Paper, PaperVersion, PaperMetadata, PaperSimilarityCheck, PaperProcessingTracker, PaperProcessingEvent, OutboxMessage, AuditLog. | `ApplicationDbContext.cs:11-21`; migrations. | 65% |
| Authentication | Cognito/JWT bearer, register/login/refresh/logout/me, token events, claims transformers. | `AuthController.cs`, `AuthenticationConfiguration.cs`, `AuthService.cs`. | 70% |
| Authorization | Fallback authenticated policy; permission enum; role/permission seed; policy handler. | `AuthorizationConfiguration.cs`; `PermissionName.cs`; `DataSeeder.cs`. | 55% |
| API Layer | Auth, User CRUD, Role CRUD/assignment, Paper upload/metadata/audit/progress/tracker, PaperVersion audit/tracker. | Controllers under `Api/Controllers`; inherited CRUD via `BaseCrudController`. | 45% |
| Application Layer | DTOs, service interfaces, repository interfaces, integration event DTOs. | `Application/DTOs`, `Services/Interfaces`, `Repositories/Interfaces`. | 60% |
| Domain Layer | Core entities and enums for identity, paper processing, similarity, outbox, audit. | `Domain/Entities`, `Domain/Enums`. | 50% |
| Event-Driven Processing | PaperUploaded -> OCR and metadata; MetadataQualityScored -> OpenAlex gate; OpenAlex requested/skipped. | `docs/openalex-quality-gate-flow.md`; Kafka consumer services. | 70% |
| Kafka Integration | Topic initializer, outbox publisher, four consumers, topic options. | `KafkaOptions.cs`, `KafkaTopicInitializerHostedService.cs`. | 65% |
| Outbox Pattern | Outbox table, pending/processing/published/failed statuses, Kafka publisher. | `OutboxMessage.cs`; `KafkaOutboxPublisherBackgroundService.cs`. | 65% |
| OCR Pipeline | Nougat service container, API client, OCR consumer, markdown S3 key persistence. | `Services/NougatService`; `PaperOcrKafkaConsumerBackgroundService.cs`. | 70% |
| Metadata Pipeline | GROBID extraction, Crossref DOI enrichment, JSON authors/references/funding, metadata API. | `PaperMetadataKafkaConsumerBackgroundService.cs`; `GrobidTeiParser.cs`; `CrossrefService.cs`. | 75% |
| OpenAlex Pipeline | Quality gate, requested/skipped events, DOI/title matching, similarity scores. | `OpenAlexGateKafkaConsumerBackgroundService.cs`; `OpenAlexSimilarityKafkaConsumerBackgroundService.cs`; tests. | 70% |
| Tracking & Monitoring | Processing tracker snapshot, processing event history, audit log APIs, progress API. | `PaperProcessingTracker.cs`, `PaperProcessingEvent.cs`, controllers. | 65% |
| Audit Logging | AuditLog entity/service; paper and paper-version query APIs. | `AuditLog.cs`; `AuditLogService.cs`; `PaperController.cs`. | 55% |
| Docker & Deployment | API Dockerfile, Kafka/Zookeeper, GROBID, Nougat compose services. API/Postgres not in compose. | `docker-compose.yml`; `Api/Dockerfile`. | 45% |
| External Integrations | AWS S3, AWS Cognito, GROBID, Nougat, Crossref, OpenAlex. | service implementations and appsettings. | 70% |

## Part 2 - Domain Model Review

### Core Research Domain

| Entity | Purpose | Relationships | Status | Missing fields/relationships |
|---|---|---|---|---|
| Paper | Manuscript aggregate root. | Has many PaperVersions; has one PaperMetadata. | Implemented basic. | Owner/user, research group/lab, lifecycle status, submission/review state, authorship declarations, COI/ethics/data-availability fields. |
| PaperVersion | Versioned PDF/Markdown file record. | Belongs to Paper. | Implemented basic. | UploadedBy, file size/hash, version reason, previous-version linkage, restore/compare data. |
| PaperMetadata | Extracted scholarly metadata and quality score storage. | Belongs to Paper. | Implemented. | Version scope is missing; current unique PaperId prevents separate metadata per version. Authorship is JSON rather than normalized. |
| PaperSimilarityCheck | OpenAlex similarity/duplicate check. | Belongs to Paper and PaperMetadata. | Implemented for OpenAlex. | Other sources, adjudication, reviewer decision, false-positive notes. |

### Review Domain

No review entities exist. Permissions imply intended Review, ReviewerResponse, QualityReport, IntegrityReport, and decision workflows, but there are no domain entities, APIs, or services.

### Publication Domain

No Venue, VenueRecommendation, Submission, PublicationDecision, Correction, Retraction, or Publication record exists. Permissions exist for venue and submission actions only.

### Identity Domain

| Entity | Purpose | Relationships | Status | Missing fields/relationships |
|---|---|---|---|---|
| User | Internal account shadow of Cognito user. | Many-to-many Roles. | Implemented. | Password should not be stored when Cognito is source of truth; profile/lab relation missing in current refactor. |
| Role | RBAC role. | Many-to-many Users and Permissions. | Implemented. | Role immutability/system role constraints. |
| Permission | Fine-grained permissions. | Many-to-many Roles. | Implemented. | Permission management/list API absent. |

### Infrastructure Domain

| Entity | Purpose | Relationships | Status | Missing fields/relationships |
|---|---|---|---|---|
| OutboxMessage | Durable event publishing. | None. | Implemented. | Max retry/dead-letter state, lock owner, next-attempt timestamp. |
| PaperProcessingTracker | Current processing snapshot. | Paper, PaperVersion, events. | Implemented. | Rich step statuses in docs are simplified in code to current stage/status/progress. |
| PaperProcessingEvent | Workflow history. | Tracker, Paper, PaperVersion. | Implemented. | Event contract/version field. |
| AuditLog | Operational audit trail. | Optional paper/version/user ids. | Implemented. | Actor IP/user agent, before/after changes, immutable retention controls. |

### ERD-Style Hierarchy

```text
User *--* Role *--* Permission

Paper 1--* PaperVersion
Paper 1--0..1 PaperMetadata
Paper 1--* PaperSimilarityCheck
PaperMetadata 1--* PaperSimilarityCheck

Paper 1--* PaperProcessingTracker
PaperVersion 1--1 PaperProcessingTracker
PaperProcessingTracker 1--* PaperProcessingEvent

OutboxMessage
AuditLog -> Paper?
AuditLog -> PaperVersion?
AuditLog -> User?
```

## Part 3 - Workflow Review

Current workflow:

```text
POST /api/papers/upload
  -> validate PDF
  -> upload PDF to S3
  -> create Paper, PaperVersion, PaperMetadata placeholder
  -> create tracker and audit logs
  -> save PaperUploadedIntegrationEvent to outbox
  -> Kafka publisher publishes ripqms.paper-uploaded.v1
  -> OCR consumer converts PDF to Markdown with Nougat and stores Markdown in S3
  -> Metadata consumer extracts GROBID metadata and Crossref enrichment
  -> MetadataQualityScoringService persists quality fields
  -> MetadataQualityScoredIntegrationEvent
  -> OpenAlex gate requests or skips
  -> OpenAlex worker persists similarity result
  -> tracker reaches COMPLETED or FAILED
```

Missing workflow stages: section extraction, AI publication quality review, integrity screening, human metadata review, authorship/COI/funding/ethics/data availability confirmation, reviewer assignment, review decision, author response, submission tracking, correction/retraction/investigation.

Broken or risky transitions:

- Metadata is one row per Paper, not per PaperVersion, so later versions can overwrite or conflict.
- OCR and metadata both consume `PaperUploaded` in parallel; metadata does not wait for Markdown. This is valid for GROBID-from-PDF but means Markdown is not used for quality/review stages.
- Outbox failed messages are retried forever without max attempts or DLQ.
- Consumers commit offsets after `ProcessMessageAsync`; processing exceptions are mostly swallowed inside services, so some failed business states still commit. That is acceptable only if the database records failure fully.
- Tracker docs describe granular step statuses, but code stores a simpler current-stage snapshot.

Missing events/APIs:

- Missing events: MarkdownGenerated, IntegrityScreeningRequested/Completed, QualityAssessmentRequested/Completed, ReviewAssigned, ReviewDecisionSubmitted, SubmissionCreated/Updated, CorrectionRequested, RetractionRequested, InvestigationOpened/Closed.
- Missing APIs: paper list/get/update/delete, paper version upload/list/get/compare/delete/restore, similarity result query, quality report query, integrity report query, review APIs, submission APIs, dashboards.

Workflow maturity: 6.5/10 for upload-to-OpenAlex processing, 2/10 for end-to-end RIPQMS governance.

## Part 4 - COPE Coverage Review

| COPE area | Status | Why |
|---|---|---|
| Authorship verification | Partial | Authors are extracted into JSON; no author entity, author confirmation, ORCID verification workflow, contribution taxonomy, or internal-user mapping. |
| Conflict of interest | Missing | No COI entity, DTO, API, workflow, or audit-specific declaration model. |
| Funding disclosure | Partial | Funding organizations are extracted and scored in metadata quality; no disclosure attestation/review workflow. |
| Ethics statement | Missing | No ethics fields, declarations, approval IDs, or APIs. |
| Data availability | Missing | No data availability statement extraction, validation, or declaration workflow. |
| Citation integrity | Partial | References are extracted; OpenAlex reference overlap contributes similarity. No citation manipulation checks or reviewer workflow. |
| Transparency | Partial | Audit logs, tracker, metadata quality details exist. Missing governance declarations and public decision trace. |
| Retraction workflow | Missing | No retraction entities/states/APIs. |
| Correction workflow | Missing | No correction entities/states/APIs. |
| Investigation workflow | Missing | No investigation case model, assignment, evidence, findings, or decision workflow. |

## Part 5 - Publication Quality Coverage

| Area | Status | Why |
|---|---|---|
| Structure assessment | Missing | No section model or structural scoring. |
| Literature review assessment | Partial | References are extracted; no review adequacy scoring. |
| Methodology assessment | Missing | No methodology extraction or evaluation. |
| Novelty assessment | Partial | OpenAlex similarity is a duplicate/similarity signal, not a novelty assessment. |
| Writing quality assessment | Missing | Nougat markdown exists, but no writing-quality worker/report. |
| Reference quality assessment | Partial | References exist and are used in similarity; no reference completeness/recency/venue quality scoring. |
| Publication readiness assessment | Missing | No composite readiness report or decision model. |

## Part 6 - Event-Driven Architecture Review

Kafka topics implemented:

- `ripqms.paper-uploaded.v1`
- `ripqms.metadata-quality-scored.v1`
- `ripqms.openalex-similarity-requested.v1`
- `ripqms.openalex-similarity-skipped.v1`

Event contracts implemented as DTOs: `PaperUploadedIntegrationEvent`, `MetadataQualityScoredIntegrationEvent`, `OpenAlexSimilarityCheckRequestedIntegrationEvent`, `OpenAlexSimilarityCheckSkippedIntegrationEvent`.

Quality assessment:

- Strong: durable outbox, producer idempotence, manual consumer commits, clear worker ownership, correlation id, processing event history, topic initializer.
- Partial: idempotency exists for OCR complete, metadata complete, and OpenAlex existing-check checks.
- Weak: no DLQ, no max retry policy, no exponential backoff, no event schema registry/version metadata beyond topic suffix, no consumer retry orchestration, no distributed lock/claim for outbox rows, no poison-message quarantine.

Risk: a failed outbox message remains eligible forever; concurrent API instances could publish the same pending outbox row unless row claiming or database locks are added.

## Part 7 - Clean Architecture Review

Layering is mostly correct:

- Api depends on Application/Infrastructure/Shared.
- Application depends on Domain/Shared.
- Infrastructure depends on Application/Domain/Shared.
- Domain depends on Shared.

Violations and concerns:

- Application service interfaces are implemented in Infrastructure because services use EF, AWS, Kafka, and external clients. This is acceptable during refactor but leaves business orchestration coupled to Infrastructure.
- Domain entities inherit `BaseEntity` from Shared, so Domain is not fully pure.
- Repository pattern is partial: User/Role/Permission repositories exist, but PaperService and workers use `ApplicationDbContext` directly.
- DTO design is better after refactor for User/Role, but many workflow responses are read-only only; command DTOs for paper/review/submission are missing.
- `BaseCrudController` policy hooks are not overridden by User/Role controllers, so inherited create/get/update/delete endpoints use fallback auth only unless explicitly overridden.

## Part 8 - API Maturity Review

Implemented APIs:

| Module | Endpoints | Status |
|---|---|---|
| Auth | register, login, refresh-token, logout, me | Implemented |
| Users | inherited create/get/update/delete | Partial |
| Roles | inherited CRUD plus list/user roles/assign/remove/replace | Partial |
| Papers | upload, metadata, audit logs, processing progress, tracker | Partial |
| Paper versions | audit logs, tracker | Partial |

Missing endpoints:

- Permission list/manage.
- Paper list/get/update/delete, owned/all queries.
- Paper version upload/list/get/compare/delete/restore.
- Similarity check result and retry endpoints.
- Quality reports and integrity reports.
- Author management/authorship verification.
- COI, funding disclosure review, ethics statement, data availability.
- Review assignment/comments/decision/author response.
- Venue, recommendation, submission tracking.
- Correction, retraction, investigation.
- Dashboard/analytics APIs.

Dashboard APIs missing:

- Processing queue status.
- Failed workflows and retry dashboard.
- Metadata quality distribution.
- OpenAlex risk distribution.
- Papers by owner/lab/status.
- COPE compliance checklist completion.

## Part 9 - Production Readiness Review

| Area | Score | Rationale |
|---|---:|---|
| Security | 5/10 | JWT/Cognito/RBAC exist; DB password shadowing, broad fallback-only endpoints, raw-ish error risk and missing ownership checks remain. |
| Scalability | 6/10 | Kafka/outbox/background workers help; outbox claiming and per-version metadata are weak. |
| Maintainability | 6/10 | Clean split and tests help; duplicate old in-memory workers and partial repository pattern add noise. |
| Reliability | 5/10 | Persisted workflow state exists; retry/DLQ/backoff/poison-message handling are incomplete. |
| Observability | 5/10 | Logs, audit logs, tracker exist; metrics/tracing/health dashboards are missing. |
| Monitoring | 3/10 | No Prometheus/OpenTelemetry/alerting found. |
| Logging | 6/10 | Structured logs are common; sensitive payload/log policy is not fully formalized. |
| Disaster recovery | 2/10 | No backup/restore, migration rollout, S3 recovery, or Kafka replay runbooks found. |

## Part 10 - Gap Analysis

| Capability | Classification | Evidence |
|---|---|---|
| Identity | Partial | Cognito/JWT/RBAC done; password stored in DB and ownership model incomplete. |
| Research Governance | Missing | No COPE declaration/case workflows. |
| Paper Management | Partial | Upload exists; list/get/version lifecycle missing. |
| Integrity Screening | Partial | OpenAlex similarity exists; no integrity report or screening workflow. |
| Quality Assessment | Partial | Metadata quality exists; no publication-quality report. |
| Review Workflow | Missing | Permissions only. |
| Submission Workflow | Missing | Permissions only. |
| Publication Workflow | Missing | No publication/correction/retraction entities. |
| Reporting | Partial | Audit/tracker reports only. |
| Analytics | Missing | Permission only. |

## Part 11 - Next Phase Roadmap

| Priority | Phase | Goal | Entities | APIs | Services/workers/events | DB changes | Complexity |
|---:|---|---|---|---|---|---|---|
| 1 | Secure core access | Fix authz, ownership, and Cognito shadow model. | User adjustments. | Add explicit policies on inherited CRUD; permission list. | CurrentUser ownership checks. | Remove/nullable Password, ownership columns. | Medium |
| 2 | Paper/version lifecycle | Make papers queryable and version-safe. | Paper owner/group, PaperVersion metadata relation. | Paper CRUD/list; version upload/list/get/delete/restore. | Version upload service; PaperVersionUploaded event. | Add owner, group, file hash/size; make metadata per version. | High |
| 3 | Workflow reliability | Productionize Kafka/outbox. | Outbox extensions. | Retry/replay admin APIs. | DLQ events, retry worker, poison message handling. | next_attempt_at, max_attempts, locked_by, locked_until, dlq table/topic. | Medium |
| 4 | Integrity report | Turn OpenAlex result into integrity workflow. | IntegrityReport, IntegrityFinding. | run/read/approve reports. | IntegrityScreeningRequested/Completed. | report/finding tables. | Medium |
| 5 | Publication quality report | Assess structure, writing, methodology, references, readiness. | QualityReport, QualityCriterionScore, PaperSection. | run/read quality report. | SectionExtraction, QualityAssessment workers/events. | section and report tables. | High |
| 6 | COPE declarations | Add governance checklists. | AuthorshipDeclaration, COIDisclosure, FundingDisclosure, EthicsStatement, DataAvailabilityStatement. | create/update/verify declarations. | DeclarationSubmitted/Verified events. | declaration tables and audit indexes. | High |
| 7 | Review workflow | Internal review assignment and decisions. | Review, ReviewAssignment, ReviewerComment, ReviewerResponse, ReviewDecision. | assign/read/comment/respond/decision. | ReviewAssigned/DecisionSubmitted. | review tables. | High |
| 8 | Submission/publication | Track venue and submission lifecycle. | Venue, VenueRecommendation, Submission. | venue/recommend/submit/track. | SubmissionCreated/Updated. | venue/submission tables. | Medium |
| 9 | Cases: correction/retraction/investigation | Support COPE post-publication governance. | InvestigationCase, CorrectionCase, RetractionCase, EvidenceItem. | open/assign/update/close cases. | CaseOpened/Closed. | case tables. | High |
| 10 | Dashboards/analytics | Operational and governance visibility. | Read models/materialized views. | dashboards for processing, quality, integrity, COPE. | Aggregation workers. | reporting views. | Medium |

## Part 12 - Final Architecture Score

| Dimension | Score |
|---|---:|
| Architecture | 72/100 |
| Domain Design | 50/100 |
| Clean Architecture | 68/100 |
| Event Driven Design | 66/100 |
| Scalability | 60/100 |
| Research Governance Support | 25/100 |
| COPE Compliance | 22/100 |
| Publication Quality Coverage | 30/100 |

Overall score: 52/100.

## What Exists, What Is Missing, What To Build, What To Refactor

Exists: Clean project split, Cognito/JWT/RBAC foundation, S3 upload, paper/version/metadata entities, audit logs, tracker/event history, Kafka/outbox, Nougat/GROBID/Crossref/OpenAlex integration, metadata quality scoring, OpenAlex quality gate, focused tests.

Missing: COPE governance domain, review/submission/publication domain, paper ownership and version-safe metadata, full paper/version APIs, integrity/quality reports, dashboards, DLQ/retry production controls, monitoring/DR.

Build next: secure ownership and policies; paper/version lifecycle; reliable outbox/DLQ; integrity report; publication quality report; COPE declarations; review workflow.

Refactor next: remove or stop persisting `User.Password`; align docs with current Clean Architecture; remove or clearly deprecate in-memory worker classes if Kafka is the path; normalize authors/declarations; move orchestration logic from Infrastructure toward Application use cases where feasible; make metadata version-scoped.
