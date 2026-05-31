# PublicationQualitySystem Architecture Review

Ngay hien tai solution dang la mot ASP.NET Core layered monolith trong 1 project. Nen tang da du tot de tiep tuc lam Postman, Docker, database logical diagram va nghiep vu, nhung can chuan hoa authorization, config va module boundary truoc khi mo rong workflow Publication Quality Assurance.

## A. Tong Quan Solution

| Hang muc | Gia tri |
|---|---|
| Solution | `PublicationQualitySystem.sln` |
| So project | 1 |
| Project startup/API | `PublicationQualitySystem/PublicationQualitySystem.csproj` |
| Entry point | `PublicationQualitySystem/Program.cs` |
| Target framework | `.NET 8` |
| Kien truc hien tai | Layered monolith |
| Clean Architecture | Chua |
| Modular Monolith | Chua ro rang |
| Swagger/OpenAPI | Co, qua Swashbuckle |
| EF Core provider | PostgreSQL/Npgsql |
| Auth provider | AWS Cognito + JWT Bearer |

## B. Folder Structure Hien Tai

| Folder | Vai tro | Trang thai | Nhan xet |
|---|---|---|---|
| `Common` | Base response/entity/controller/service | Done mot phan | Co `BaseResponse`, `BaseEntity`, `ApiBaseController` |
| `Configurations` | DI, DbContext, Swagger, Auth, AWS, Seeder | Done mot phan | Dang gom nhieu infrastructure config |
| `Constants` | Hang so app/AWS/claim/policy | Done mot phan | Co fallback DB connection hard-code |
| `Controllers` | API endpoint layer | Done mot phan | Controller mong, goi service |
| `DTOs` | Request/response DTO | Done mot phan | Mot so DTO dung chung create/update/response |
| `Entities` | EF Core entities | Done mot phan | Co 10 entity, nhung API moi co cho mot phan |
| `Enums` | Role, permission, status, upload type | Done | Permission enum bao phu nhieu module tuong lai |
| `Exceptions` | Error code + AppException | Done mot phan | Co chuan hoa exception noi bo |
| `Extensions` | App/service/http/claims extensions | Done mot phan | Pipeline va helper da tach rieng |
| `Mappings` | Manual DTO mapper | Done mot phan | Don gian, de doc |
| `Middleware` | Global exception handling | Done mot phan | Co response format thong nhat, validation chua co `errors[]` |
| `Migrations` | EF migrations | Done | Da co migration ban dau va migration Cognito sub |
| `Options` | Strongly typed config | Done mot phan | Co AWS/Cognito/S3/Admin/JWT options |
| `Repositories` | Repository interface/implementation | Done mot phan | Chua nhat quan vi service van goi DbContext truc tiep |
| `Security` | JWT events, claims transformer, permission handler | Done mot phan | Co permission claim transform tu DB |
| `Services` | Business/application services | Done mot phan | Co logic nghiep vu chinh |

## C. Layer Review

| Layer | Dang co | Tot | Can sua/chuan hoa |
|---|---|---|---|
| Controllers | `Auth`, `User`, `Role`, `ResearchProfile`, `ResearchGroup`, `Upload`, `S3` | Mong, it business logic | CRUD endpoint can explicit policy; can them API versioning |
| DTOs | Auth/User/Role/Profile/Group/File DTOs | Response khong tra raw entity | Tach `Create`, `Update`, `Response` DTO cho module lon |
| Entities | 10 EF entities | Relationship core da co | Paper/Author chua gan owner/group; thieu entity review/report/submission |
| DbContext | `ApplicationDbContext` | Fluent config kha ro | Dang nam trong `Configurations`, nen dua ve `Infrastructure/Persistence` neu mo rong |
| Repositories | User/Role/Permission/ResearchGroupMember | Truy van Include tap trung | Chua co repository cho Paper/Profile/Group; service goi DbContext truc tiep |
| Services | Auth/User/Role/Profile/Group/File | Controller khong goi DbContext | Mot so service vua dung repo vua dung DbContext |
| Interfaces | Co service/repository interfaces | DI ro rang | Interface import thua nhieu namespace DTO |
| Middleware | `ExceptionHandlingMiddleware` | Co global error response | Validation response chua co `errors[]`; 500 tra exception message |
| Configurations | DB/Auth/Authz/AWS/Swagger/Seeder | Tach extension tot | Can chuan hoa env var va Docker config |
| Migrations | Co | Tu dong migrate luc startup neu co connection | Can can nhac production migration strategy |
| Helpers/Utils | Extensions, mappers, constants | Co | Nen gom theo Shared/Infrastructure khi module lon |

## D. Dependency Direction

| Kiem tra | Ket qua | Nhan xet |
|---|---|---|
| Controller co goi truc tiep DbContext khong? | Khong | Controller goi service/interface |
| Controller co business logic khong? | Rat it | `AuthController.Me()` doc claim va tao DTO, chap nhan duoc |
| Service co phu thuoc Controller khong? | Khong | Khong thay dependency nguoc ve controller |
| Repository co phu thuoc Service/Controller khong? | Khong | Repository chi phu thuoc DbContext/entity |
| Service co goi DbContext truc tiep khong? | Co | `AuthService`, `UserService`, `RoleService`, `ResearchProfileService`, `ResearchGroupService` |
| Entity co bi lo truc tiep ra response khong? | Khong ro rang | API tra DTO trong `BaseResponse<T>` |
| DTO co duoc dung dung khong? | Co, nhung chua toi uu | Dang co DTO chung cho create/update/response |
| Auth/security co phu thuoc DB khong? | Co | `PermissionClaimsTransformer` load role/permission tu DB theo email |

## E. Database Architecture

| Hang muc | Gia tri |
|---|---|
| DbContext | `ApplicationDbContext` |
| Tong DbSet | 10 |
| Migration | Co |
| Migration files | `20260523094024_InitialCreate`, `20260523163554_UseCognitoSubForUserId` |
| Connection string source | `ConnectionStrings:DefaultConnection` -> `${DB_URL}` |
| Fallback connection hard-code | Co, trong `AppConstants.DefaultConnectionString` |
| Auto migration on startup | Co, trong `SeedDatabaseAsync()` neu co DefaultConnection |

### Entities

| Entity | DbSet | Relationship | Trang thai |
|---|---:|---|---|
| `User` | Co | Many-to-many Role, one-to-one ResearchProfile, one-to-many ResearchGroupMember | Active |
| `Role` | Co | Many-to-many User, many-to-many Permission | Active |
| `Permission` | Co | Many-to-many Role | Active |
| `ResearchProfile` | Co | One-to-one User | Active |
| `ResearchGroup` | Co | One-to-many ResearchGroupMember | Active |
| `ResearchGroupMember` | Co | Many-to-one ResearchGroup/User | Active |
| `Author` | Co | One-to-many PaperAuthor | DB skeleton |
| `Paper` | Co | One-to-many PaperAuthor/PaperVersion | DB skeleton |
| `PaperAuthor` | Co | Join Paper/Author | DB skeleton |
| `PaperVersion` | Co | Many-to-one Paper | DB skeleton |

### Database Gaps

| Gap | Anh huong |
|---|---|
| `User.Password` dang luu hash password | Khong nen neu Cognito la auth source chinh |
| Paper chua co owner/user/group relationship | Kho quan ly paper theo researcher/lab |
| Author chua lien ket User/ResearchProfile | Kho mapping author noi bo voi tac gia ngoai |
| Chua co QualityReport/IntegrityReport/InternalReview/Submission/Venue/Notification/AuditLog entity | Chua du workflow Publication QA |
| ResearchGroup leader suy ra tu member role | Chap nhan duoc, nhung can constraint nghiep vu 1 leader active/group |

## F. Authentication/Authorization

| Hang muc | Trang thai | Nhan xet |
|---|---|---|
| Mechanism | AWS Cognito + JWT Bearer | `JwtBearer` validate issuer Cognito |
| Cookie auth | Khong | API thuan JWT |
| Access token validation | Co | Yeu cau `token_use == access` va `client_id` dung |
| Role/permission | Co | Role/permission luu DB, permission add vao claims |
| Fallback policy | Co | Tat ca endpoint can authenticated user tru `[AllowAnonymous]` |
| Explicit policy | Co mot phan | File, role assign/read, research read/member manage |
| Password trong DB | Co | Can canh bao: khong nen luu password DB nghiep vu neu dung Cognito |
| Admin seed | Co | Seed Cognito admin + DB user/role |

### Authorization Gaps

| Endpoint group | Van de |
|---|---|
| Users CRUD | Chi co fallback JWT, chua co `USER_*`/`LAB_MEMBER_*` policy |
| Roles CRUD by id | Create/get/update/delete chua co `ROLE_CREATE/READ/UPDATE/DELETE` policy |
| Research Profiles CRUD | Chua co `RESEARCH_PROFILE_CREATE/UPDATE/DELETE` policy |
| Research Groups CRUD | Chua co `RESEARCH_GROUP_CREATE/UPDATE/DELETE` policy |
| Auth logout | Dang `[AllowAnonymous]`, nhung yeu cau access token trong body |

## G. API Architecture

| Hang muc | Trang thai |
|---|---|
| RESTful route | Tuong doi |
| API versioning | Chua co |
| Swagger/OpenAPI | Co |
| Response format | Co `BaseResponse<T>` |
| Error handling | Co global middleware |
| Validation | Co DataAnnotations va custom invalid model response |
| Standard validation errors array | Chua |
| Raw entity response | Khong thay |

### API Issues

| Van de | Muc do | Ghi chu |
|---|---|---|
| Chua co API versioning | Trung binh | Nen them truoc khi mo rong nhieu module |
| Swagger thieu annotation chi tiet | Thap | Nen them `ProducesResponseType`, summary, examples |
| File endpoint da reset | Thap | Workflow moi di qua `POST /api/papers/upload` va Nougat service |
| Role assignment route hoi kho doc | Thap | Co the doi ve `/api/users/{userId}/roles/{roleId}` |
| Validation response chua co `errors[]` | Trung binh | Chua khop Postman standard mong muon |

## H. File/S3 Architecture

| Hang muc | Trang thai |
|---|---|
| Upload service | Co `UploadService` |
| Storage abstraction | Co `IFileStorageService` |
| S3 implementation | Co `S3FileService` |
| DTO response | Co `S3FileResponseDto` |
| Local file storage | Khong thay |
| AWS key hard-code | Khong hard-code truc tiep |
| AWS config source | `appsettings.json` placeholders + env resolver |
| Bucket config | `Aws:S3:Bucket` |

### File/S3 Gaps

| Gap | Nhan xet |
|---|---|
| Chua co file metadata entity | Neu can audit file upload/delete thi nen them |
| Delete file qua `/api/s3?fileKey=` | Nen chuan hoa route |
| Upload file chua gioi han size ro trong config | Nen them size limit va policy theo type |

## I. Module Completion Matrix

| Module | Da co chua | Folder/Class lien quan | Controller | Service | Entity | Muc do hoan thanh | Thieu gi |
|---|---|---|---|---|---|---|---|
| Auth | Co | `DTOs/Auth`, `AuthService` | Co | Co | `User` | 75% | Bo password DB, refresh/logout policy decision |
| User | Co | `UserDto`, `UserService` | Co | Co | `User` | 60% | List API, explicit policy, DTO tach rieng |
| Role | Co | `RoleDto`, `RoleService` | Co | Co | `Role`, `Permission` | 70% | Permission API, CRUD policies |
| Permission | Mot phan | `PermissionName`, `PermissionRepository` | Chua | Chua public | `Permission` | 40% | List/manage API |
| Research Profile | Co | Profile DTO/service/mapper | Co | Co | `ResearchProfile` | 60% | Policy CRUD, DTO day du hon |
| Research Group | Co | Group/member DTO/service/mapper | Co | Co | `ResearchGroup`, `ResearchGroupMember` | 70% | CRUD policies, leader constraint |
| Paper | Mot phan | `Paper` entity | Chua | Chua | `Paper` | 20% | Full API/service/DTO/repository |
| Paper Version | Mot phan | `PaperVersion` entity | Chua | Chua | `PaperVersion` | 20% | Version upload/read/compare/delete |
| Author | Mot phan | `Author`, `PaperAuthor` | Chua | Chua | Co | 20% | API + relationship voi user/profile |
| Quality Report | Chua | Permission enum | Chua | Chua | Chua | 0% | Entity/API/service |
| Integrity Report | Chua | Permission enum | Chua | Chua | Chua | 0% | Entity/API/service |
| Internal Review | Chua | Permission enum | Chua | Chua | Chua | 0% | Entity/API/service |
| Reviewer Comment | Chua | Khong thay | Chua | Chua | Chua | 0% | Entity/API/service |
| Reviewer Response | Chua | Permission enum | Chua | Chua | Chua | 0% | Entity/API/service |
| Venue Recommendation | Chua | Permission enum | Chua | Chua | Chua | 0% | Venue + recommendation model |
| Submission | Chua | `SubmissionStatus` enum | Chua | Chua | Chua | 0% | Entity/API/service |
| Notification | Chua | Permission enum | Chua | Chua | Chua | 0% | Entity/API/service |
| Audit Log | Chua | Permission enum | Chua | Chua | Chua | 0% | Entity/event trail/API |

## J. Docker Readiness

| Kiem tra | Trang thai | Can lam |
|---|---|---|
| Dockerfile | Chua co | Them Dockerfile multi-stage cho .NET 8 |
| docker-compose.yml | Chua co | Them API + PostgreSQL |
| API port local | `https://localhost:7210`, `http://localhost:5245` | Container nen expose `8080` hoac `5000` |
| `appsettings.Development.json` | Chua co | Nen them de tach local/dev |
| DB config env | Co `${DB_URL}` | Chuan hoa env name cho compose |
| AWS config env | Co `${AWS_*}` | Dua vao env/secrets |
| Admin seed env | Co, nhung typo `${Emaill}` | Sua truoc Docker |
| HTTPS redirect | Co | Can cau hinh reverse proxy/container phu hop |

## K. Cac Van De Can Sua Truoc Khi Mo Rong

| Uu tien | Van de | Ly do |
|---:|---|---|
| P0 | Khong nen luu password hash trong DB nghiep vu khi dung Cognito | Giam rui ro bao mat va tranh duplicate auth source |
| P0 | Gan explicit authorization policy cho CRUD endpoint | QA/security can permission ro rang |
| P1 | Chuan hoa env/config truoc Docker | Docker compose se phu thuoc config sach |
| P1 | Tach DTO theo use case | Giam loi validation va response contract |
| P1 | Them API versioning | Can truoc khi public/mo rong API |
| P2 | Hoan thien Paper/Author/PaperVersion workflow | Day la core domain |
| P2 | Them entities/API cho review/report/submission | Hoan thien Publication QA |
| P2 | Swagger annotations/examples | Giup QA va Postman sync |
| P3 | Chuan hoa repository pattern hoac bo bot repository neu khong can | Giam inconsistency trong data access |

## L. De Xuat Folder Chuan Neu Tiep Tuc 1 Project

```text
PublicationQualitySystem/
  Modules/
    Auth/
    Users/
    Roles/
    ResearchProfiles/
    ResearchGroups/
    Papers/
    Reviews/
    Reports/
    Submissions/
    Notifications/
  Infrastructure/
    Persistence/
    Aws/
    Auth/
    Storage/
  Shared/
    Common/
    Exceptions/
    Middleware/
    Security/
  Configurations/
```

## M. De Xuat Neu Chuyen Sang Clean Architecture Sau Nay

```text
PublicationQualitySystem.Api
PublicationQualitySystem.Application
PublicationQualitySystem.Domain
PublicationQualitySystem.Infrastructure
PublicationQualitySystem.Tests
```
