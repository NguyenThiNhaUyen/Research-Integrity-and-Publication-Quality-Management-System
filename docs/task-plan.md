# PublicationQualitySystem Task Plan

Task plan nay dung de theo doi nhung viec da hoan thanh, viec can sua truoc QA/Docker, va viec can trien khai tiep cho workflow Publication Quality Assurance.

## Legend

| Status | Y nghia |
|---|---|
| Done | Da co trong code hien tai |
| Partial | Da co mot phan, can hoan thien |
| Todo | Chua co |
| Blocked | Can quyet dinh/thong tin truoc |

## Phase 0 - Architecture Baseline

| ID | Task | Status | Uu tien | Ghi chu |
|---|---|---|---|---|
| A0-01 | Xac dinh project startup/API | Done | P0 | `PublicationQualitySystem.csproj` |
| A0-02 | Xac dinh architecture hien tai | Done | P0 | Layered monolith |
| A0-03 | Lap danh sach folder/layer | Done | P0 | Xem `architecture-review.md` |
| A0-04 | Lap API inventory | Done | P0 | Xem `api-inventory.md` |
| A0-05 | Lap module completion matrix | Done | P0 | Xem `architecture-review.md` |

## Phase 1 - Fix Truoc QA

| ID | Task | Status | Uu tien | Ghi chu |
|---|---|---|---|---|
| QA-01 | Quyet dinh loai bo `User.Password` khoi DB nghiep vu | Todo | P0 | Dang dung AWS Cognito, khong nen duplicate password |
| QA-02 | Sua auth/register de khong luu password hash trong app DB | Todo | P0 | Neu chap nhan QA-01 |
| QA-03 | Gan policy cho `UserController` CRUD | Todo | P0 | `USER_*` hoac `LAB_MEMBER_*` |
| QA-04 | Gan policy cho `RoleController` create/get/update/delete by id | Todo | P0 | `ROLE_CREATE`, `ROLE_READ`, `ROLE_UPDATE`, `ROLE_DELETE` |
| QA-05 | Gan policy cho `ResearchProfileController` CRUD | Todo | P0 | `RESEARCH_PROFILE_*` |
| QA-06 | Gan policy cho `ResearchGroupController` CRUD | Todo | P0 | `RESEARCH_GROUP_*` |
| QA-07 | Chuan hoa validation error co `errors[]` | Todo | P1 | Dang tra message dau tien |
| QA-08 | Khong tra exception message raw o 500 | Todo | P1 | Middleware hien tra `exception.Message` |
| QA-09 | Them Swagger response annotations | Todo | P2 | `ProducesResponseType`, summary, examples |
| QA-10 | Them API versioning `/api/v1` | Todo | P1 | Nen lam truoc khi mo rong API |

## Phase 2 - Config Va Docker Readiness

| ID | Task | Status | Uu tien | Ghi chu |
|---|---|---|---|---|
| DKR-01 | Chuan hoa env var DB | Todo | P0 | `DB_URL` hoac `ConnectionStrings__DefaultConnection` |
| DKR-02 | Sua typo admin env `${Emaill}` | Todo | P0 | Nen thanh `${ADMIN_EMAIL}` hoac `${Email}` |
| DKR-03 | Tao `appsettings.Development.json` | Todo | P1 | Tach config local/dev |
| DKR-04 | Loai bo hoac giam phu thuoc fallback DB hard-code | Todo | P1 | `AppConstants.DefaultConnectionString` |
| DKR-05 | Tao Dockerfile multi-stage | Todo | P1 | .NET 8 publish/runtime |
| DKR-06 | Tao `docker-compose.yml` cho API + PostgreSQL | Todo | P1 | Co volume va env |
| DKR-07 | Chuan hoa port container | Todo | P1 | De xuat `8080` |
| DKR-08 | Kiem tra HTTPS redirect trong container | Todo | P2 | Can cau hinh reverse proxy/Kestrel |
| DKR-09 | Dua AWS credentials vao secret/env | Todo | P1 | Khong commit secret |

## Phase 3 - API Contract Va DTO Standardization

| ID | Task | Status | Uu tien | Ghi chu |
|---|---|---|---|---|
| API-01 | Tach `UserCreateRequest`, `UserUpdateRequest`, `UserResponse` | Todo | P1 | Dang dung `UserDto` chung |
| API-02 | Tach `RoleCreateRequest`, `RoleUpdateRequest`, `RoleResponse` | Todo | P1 | Dang co `UpdateRoleDto` nhung controller chua dung |
| API-03 | Tach `ResearchProfileCreate/Update/Response` | Todo | P1 | Entity nhieu field hon DTO |
| API-04 | Tach `ResearchGroupCreate/Update/Response` | Todo | P1 | Tach member commands rieng |
| API-05 | Chuan hoa `BaseResponse` theo success/message/data/errors | Todo | P1 | Hien co `code`, chua co `errors` |
| API-06 | Chuan hoa route file upload/delete | Todo | P2 | `api/s3` nen doi domain-friendly |
| API-07 | Tao endpoint list users | Todo | P2 | Service co `GetAllAsync`, controller chua co |
| API-08 | Tao endpoint list permissions | Todo | P2 | Permission da seed nhung chua public API |

## Phase 4 - Core Publication Workflow

| ID | Task | Status | Uu tien | Ghi chu |
|---|---|---|---|---|
| PUB-01 | Tao Paper DTO/service/repository/controller | Todo | P0 | Entity da co |
| PUB-02 | Them owner/group relationship cho Paper | Todo | P0 | Can de query theo researcher/lab |
| PUB-03 | Tao Author API | Todo | P1 | Entity da co |
| PUB-04 | Link Author voi User/ResearchProfile neu la internal author | Todo | P1 | Can cho workflow lab |
| PUB-05 | Tao PaperVersion upload/list/get/delete | Todo | P0 | Entity da co |
| PUB-06 | Tao PaperVersion compare endpoint | Todo | P2 | Permission da co |
| PUB-07 | Tao submit internal review endpoint | Todo | P1 | Permission da co |

## Phase 5 - Quality Assurance Workflow

| ID | Task | Status | Uu tien | Ghi chu |
|---|---|---|---|---|
| QAFL-01 | Tao QualityReport entity/API/service | Todo | P0 | Can cho publication quality |
| QAFL-02 | Tao IntegrityReport entity/API/service | Todo | P0 | Can cho integrity workflow |
| QAFL-03 | Tao InternalReview entity/API/service | Todo | P0 | Can cho review assignment |
| QAFL-04 | Tao ReviewerComment entity/API/service | Todo | P1 | Comment theo review/paper version |
| QAFL-05 | Tao ReviewerResponse entity/API/service | Todo | P1 | Response cua author |
| QAFL-06 | Tao ReviewDecision endpoint | Todo | P1 | Submit decision |
| QAFL-07 | Tao AuditLog entity/API/service | Todo | P1 | Can trace QA actions |

## Phase 6 - Submission, Venue, Notification, Dashboard

| ID | Task | Status | Uu tien | Ghi chu |
|---|---|---|---|---|
| EXT-01 | Tao Venue entity/API/service | Todo | P1 | Cho recommendation/submission |
| EXT-02 | Tao VenueRecommendation API | Todo | P1 | Permission da co |
| EXT-03 | Tao Submission entity/API/service | Todo | P1 | `SubmissionStatus` enum da co |
| EXT-04 | Tao Submission tracking endpoint | Todo | P1 | Permission da co |
| EXT-05 | Tao Notification entity/API/service | Todo | P2 | Permission da co |
| EXT-06 | Tao Dashboard/Analytics endpoint | Todo | P2 | Permission analytics da co |

## Phase 7 - Testing And Documentation

| ID | Task | Status | Uu tien | Ghi chu |
|---|---|---|---|---|
| DOC-01 | Import Postman collection va environment | Done | P1 | Da co file o root |
| DOC-02 | Bo sung Postman tests cho happy path auth | Todo | P1 | Login -> save token |
| DOC-03 | Bo sung Postman workflow Paper -> Review -> Report | Todo | P1 | Sau khi API co |
| DOC-04 | Generate DB logical diagram | Todo | P1 | Sau khi entity core on dinh |
| DOC-05 | Viet API workflow documentation | Todo | P2 | Sequence theo module |
| DOC-06 | Them unit/integration tests | Todo | P1 | Nen co cho service/auth/policy |

## Suggested Execution Order

| Thu tu | Viec nen lam |
|---:|---|
| 1 | Fix password storage decision va auth data model |
| 2 | Gan explicit policy cho CRUD endpoint |
| 3 | Chuan hoa validation/error response |
| 4 | Chuan hoa env vars va appsettings development |
| 5 | Them API versioning |
| 6 | Dockerfile + docker-compose PostgreSQL |
| 7 | Hoan thien Paper/Author/PaperVersion |
| 8 | Hoan thien Review/Quality/Integrity workflow |
| 9 | Hoan thien Submission/Venue/Notification/Audit |
| 10 | Cap nhat Postman workflow va DB logical diagram |

