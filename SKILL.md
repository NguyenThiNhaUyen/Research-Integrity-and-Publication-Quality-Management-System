---
name: publication-quality-system-architecture
description: Architecture and refactoring guide for the Research Integrity and Publication Quality Management System. Use when modifying this repository, adding controllers/services/repositories/DTOs/entities, refactoring CRUD or error handling, working with AWS Cognito/S3/Textract, EF Core, authorization, response formats, or preserving project conventions.
---

# Project Overview

This repository is a .NET 8 ASP.NET Core Web API for a Research Integrity and Publication Quality Management System. It manages lab members, authentication, roles and permissions, research profiles, research groups, file uploads, paper versions, and OCR extraction.

Main business domains:

- Authentication and current user data through AWS Cognito.
- Role and permission management with policy-based authorization.
- Lab member users, research profiles, and research groups.
- Research group membership, status, roles, and leader assignment.
- Paper version upload, restore, download URL generation, and OCR support.
- S3-backed file storage with uploaded file metadata.

Technology stack:

- .NET 8, ASP.NET Core Web API, nullable enabled, implicit usings enabled.
- Entity Framework Core with PostgreSQL via Npgsql.
- AWS Cognito, S3, Textract, and STS SDKs.
- JWT bearer authentication and policy authorization.
- Swagger via Swashbuckle.
- BCrypt.Net for password hashing.

# Solution Structure

The solution uses five projects under `PublicQualitySystem`:

```text
PublicationQualitySystem.Api
PublicationQualitySystem.Application
PublicationQualitySystem.Domain
PublicationQualitySystem.Infrastructure
PublicationQualitySystem.Shared
```

## Api

Responsibilities:

- HTTP controllers.
- Controller base classes.
- Authentication and authorization configuration.
- Swagger and API service configuration.
- Middleware and request pipeline.

Main folders:

- `Common`: `ApiBaseController`, `BaseCrudController`.
- `Configurations`: authentication, authorization, Swagger, service setup.
- `Controllers`: endpoint classes.
- `Extensions`: app and service extension methods.
- `Middleware`: global exception handling.

## Application

Responsibilities:

- Request/response DTO contracts.
- Service interfaces.
- Repository interfaces.
- Manual mapping helpers.

Main folders:

- `DTOs`: request and response contracts grouped by domain.
- `Mappings`: static manual mapper classes.
- `Repositories/Interfaces`: repository contracts.
- `Services/Interfaces`: service contracts.

## Domain

Responsibilities:

- Entity classes and enums.
- Business model shape without infrastructure implementation.

Main folders:

- `Entities`: EF Core entity types.
- `Enums`: domain enumerations such as roles, permissions, statuses, ranks, and upload types.

## Infrastructure

Responsibilities:

- EF Core DbContext and migrations.
- Repository implementations.
- Service implementations.
- AWS client configuration and integrations.
- Security helpers, claims transformers, authorization handlers.
- Options classes and database seeders.

Main folders:

- `Configurations`: DbContext, database, AWS, repositories, seeders.
- `Migrations`: EF Core migrations.
- `Options`: strongly typed configuration options.
- `Repositories/Implementations`: EF Core query implementations.
- `Security`: current user, JWT events, permission policies, claims transforms.
- `Services/Implementations`: concrete application services.

## Shared

Responsibilities:

- Cross-cutting primitives and constants.
- Common response model.
- Error code and exception abstractions.
- Shared extensions.

Main folders:

- `Common`: `BaseEntity`, `BaseResponse<T>`, `IBaseCrudService`, `IErrorCode`, `ErrorCode`.
- `Constants`: app, AWS, claim, and policy constants.
- `Exceptions`: `AppException` and error-code holder classes.
- `Extensions`: shared extension helpers.

# Architecture Rules

Architecture style: Clean/Layered hybrid.

Dependency flow:

```text
Api -> Application + Infrastructure + Shared
Infrastructure -> Application + Domain + Shared
Application -> Domain + Shared
Domain -> Shared
Shared -> no project dependencies
```

Rules:

- Keep HTTP concerns in `Api`.
- Keep interfaces, DTO contracts, and mapping helpers in `Application`.
- Keep entities and enums in `Domain`.
- Keep EF Core, AWS, repositories, concrete services, auth infrastructure, and seeders in `Infrastructure`.
- Keep reusable cross-cutting primitives in `Shared`.
- Do not move infrastructure dependencies into `Domain` or `Shared`.
- Preserve project boundaries when adding new features.

# DTO Rules

DTO files remain under `PublicationQualitySystem.Application.DTOs.*`.

Naming convention:

- Create input: `CreateXRequest`.
- Update input: `UpdateXRequest`.
- Output: `XResponse`.
- Specialized input/output: `LoginRequest`, `AuthResponse`, `UploadFileResponse`, `PaperVersionResponse`, etc.
- Do not use `Dto` or `DTO` suffix in class names or file names.

CRUD resources should have separate request and response contracts:

```text
CreateResearchGroupRequest
UpdateResearchGroupRequest
ResearchGroupResponse
```

For nested or specialized workflows, use explicit names:

```text
CreatePaperVersionRequest
UpdatePaperVersionRequest
PaperVersionResponse
ResearchGroupMemberRequest
ResearchGroupMemberResponse
```

Request DTOs may use DataAnnotations for validation. Keep output-only fields out of request types unless existing behavior requires them.

# Controller Rules

Use `ApiBaseController` for standard response helpers:

- `CreatedResponse(data)`
- `OkResponse(data, message)`

Use `BaseCrudController<TCreateRequest, TUpdateRequest, TResponse, Id>` for controllers with full CRUD.

Inherited CRUD actions:

- `POST` -> `CreateAsync`
- `PUT "{id}"` -> `UpdateAsync`
- `GET "{id}"` -> `GetByIdAsync`
- `DELETE "{id}"` -> `DeleteAsync`

Do not duplicate inherited CRUD actions in concrete controllers. Concrete controllers should only contain business endpoints that are not handled by the base controller.

Current CRUD controllers:

- `UserController`
- `RoleController`
- `ResearchGroupController`
- `ResearchProfileController`
- `PaperVersionController`

Business endpoints to preserve:

- `ResearchGroupController`: list, members, role/status updates, leader assignment, groups by user.
- `RoleController`: list, user roles, assign/remove/replace roles.
- `ResearchProfileController`: get profile by user id.
- `PaperVersionController`: list versions and restore version.

`BaseCrudController` has optional policy hooks:

- `CreatePolicy`
- `UpdatePolicy`
- `GetByIdPolicy`
- `DeletePolicy`

Use protected entity hooks for special CRUD behavior:

- `CreateEntityAsync`
- `UpdateEntityAsync`
- `GetEntityByIdAsync`
- `DeleteEntityAsync`

`PaperVersionController` uses these hooks because version CRUD is nested under `api/papers/{paperId:long}/versions` and needs `paperId`.

# Service Rules

Service interfaces live in `Application/Services/Interfaces`.

Service implementations live in `Infrastructure/Services/Implementations`.

Naming:

- Interface: `IXService`.
- Implementation: `XService`.
- Async methods end with `Async`.

For CRUD services, implement:

```csharp
IBaseCrudService<TCreateRequest, TUpdateRequest, TResponse, Id>
```

Current CRUD service interfaces:

- `IUserService : IBaseCrudService<CreateUserRequest, UpdateUserRequest, UserResponse, string>`
- `IRoleService : IBaseCrudService<CreateRoleRequest, UpdateRoleRequest, RoleResponse, long>`
- `IResearchGroupService : IBaseCrudService<CreateResearchGroupRequest, UpdateResearchGroupRequest, ResearchGroupResponse, long>`
- `IResearchProfileService : IBaseCrudService<CreateResearchProfileRequest, UpdateResearchProfileRequest, ResearchProfileResponse, long>`

Use `AppException` with a specific `IErrorCode` for expected business errors. Preserve existing messages and authorization checks when refactoring.

# Repository Rules

Repository interfaces live in `Application/Repositories/Interfaces`.

Repository implementations live in `Infrastructure/Repositories/Implementations`.

Naming:

- Interface: `IXRepository`.
- Implementation: `XRepository`.

Patterns:

- Use EF Core queries directly in repository implementations.
- Use `FindBy...Async` for nullable entity lookup.
- Use `Exists...Async` for existence checks.
- Use `FindAllAsync(skip, take)` or domain-specific list methods for paged queries.
- Use `Include`/`ThenInclude` where services require related data.
- Filter soft-deleted entities with `!x.Deleted` unless intentionally including deleted records.
- Keep `SaveChangesAsync` in repositories only where that pattern already exists for aggregate-specific repositories; otherwise services may call DbContext directly as current code does.

# Entity Rules

Entities live in `Domain/Entities`.

Most entities inherit `BaseEntity`, which uses `long` keys. `User` inherits `BaseEntity<string>` because the user id is the Cognito subject.

`BaseEntity<TKey>` provides:

- `Id`
- `CreatedAt`
- `UpdatedAt`
- `Deleted`
- `CreatedBy`
- `UpdatedBy`

Entity conventions:

- Use navigation properties for relationships.
- Initialize collection navigations with `HashSet<T>` or `List<T>`.
- Store soft deletion in `Deleted`.
- Configure persistence details in `ApplicationDbContext`, not in entities.
- Use enum properties in entities and configure string conversions in DbContext.

Important domains:

- User, Role, Permission.
- ResearchProfile, ResearchGroup, ResearchGroupMember.
- Paper, Author, PaperAuthor, PaperVersion.
- UploadedFile.

# Validation Rules

Current validation approach:

- Request DTOs use DataAnnotations such as `[Required]`, `[EmailAddress]`, and `[MaxLength]`.
- API invalid model state is handled in API service configuration using `BaseResponse<object>.Error(400, message)`.
- No FluentValidation validators or `AbstractValidator<T>` classes are currently present.

When adding validation:

- Preserve existing DataAnnotations unless intentionally migrating a complete validation path.
- If adding FluentValidation later, document the new registration and naming convention.
- Keep validation messages stable unless the task asks to change them.

# Mapper Rules

Mapping is manual.

No AutoMapper or Mapster package/pattern is present.

Manual mapper classes live in `Application/Mappings` and are static:

- `UserMapper`
- `RoleMapper`
- `ResearchGroupMapper`
- `ResearchProfileMapper`
- `PaperVersionMapper`

Preferred method names:

- `ToResponse(entity)`
- `ToMemberResponse(entity)` for member-specific mappings.
- `UpdateEntity(entity, request)` for applying request data to existing entities.

Some mapping is still inline in services, such as `UploadService.ToResponse`; prefer a mapper class for new repeated domain mapping.

# Error Handling Rules

Error handling lives in `Shared/Exceptions` and `Api/Middleware`.

Standard error primitive:

```csharp
public interface IErrorCode
{
    string Message { get; }
    int StatusCode { get; }
}
```

`ErrorCode` must only contain `Message`, `StatusCode`, and a constructor accepting `(string message, int statusCode)`.

Rules:

- Do not use `HttpStatusCode`, `System.Net`, or enum HTTP status values in error-code definitions.
- Use numeric status codes, e.g. `404`, `400`, `409`, `401`, `403`, `500`, `502`.
- Error-code holder classes such as `AuthErrorCode` and `OcrErrorCode` are `static class` containers and must not implement `IErrorCode`.
- Throw expected business errors with `new AppException(SomeErrorCode.SomeCase)` or `new AppException(code, customMessage)`.
- `ExceptionHandlingMiddleware` converts `AppException` into `BaseResponse<object>.Error(statusCode, message)`.

# Response Format

Standard response wrapper:

```csharp
BaseResponse<T>
```

Fields:

- `Success`
- `Code`
- `Message`
- `Data`

Success helpers:

- `BaseResponse<T>.Created(data)` returns code `201`.
- `BaseResponse<T>.SuccessResponse(data, message)` returns code `200`.
- `ApiBaseController.CreatedResponse(data)` wraps created responses.
- `ApiBaseController.OkResponse(data, message)` wraps success responses.

Error helper:

- `BaseResponse<T>.Error(code, message)` returns `Success = false`.

Preserve response shape and messages when refactoring endpoints unless explicitly requested.

# Authentication Rules

Authentication uses AWS Cognito JWT bearer tokens.

Rules:

- JWT authority is built from Cognito region and user pool id.
- Tokens must be Cognito access tokens.
- `JwtAuthenticationEvents` validates `token_use == "access"` and matching `client_id`.
- `CognitoClaimsTransformer` maps Cognito groups to role claims with `ROLE_` prefix.
- Scope claims are normalized with `SCOPE_` prefix.
- `PermissionClaimsTransformer` loads user roles and permissions from the database and adds permission claims.
- `PermissionAuthorizationHandler` checks permission policies.
- Authorization policies are generated from `PermissionName` enum names.
- Default fallback policy requires authenticated users.

Use `[Authorize(Policy = "...")]` for protected business endpoints. Preserve existing policies when refactoring controllers.

# File Storage Rules

File storage uses AWS S3 and metadata in the `uploaded_files` table.

Key services:

- `IFileStorageService` / `S3FileService`
- `IUploadService` / `UploadService`
- `IPdfOcrService` / `TextractOcrService`

Upload rules:

- Validate file presence, size, extension, and content type.
- Use `UploadType` to decide allowed extensions, content types, and S3 folder.
- Store metadata in `UploadedFile`.
- Build S3 URL from bucket, region, and key.
- Track uploader through `ICurrentUserProvider.Subject`.

Paper version rules:

- Paper versions use uploaded files.
- Access checks allow admins, owners, and active research group members.
- Download URLs are generated with expiration from manuscript upload options.

OCR rules:

- Textract OCR receives normalized S3 object keys.
- Preserve AWS error handling and identity/bucket-region checks.

# Database Rules

Database stack:

- EF Core.
- PostgreSQL via Npgsql.
- Migrations live in `Infrastructure/Migrations`.
- DbContext is `ApplicationDbContext`.

DbContext rules:

- Configure tables and relationships in `ApplicationDbContext.OnModelCreating`.
- Use snake_case table names and selected column names.
- Configure many-to-many join tables for user roles and role permissions.
- Configure enum conversions to strings.
- Configure DateTime values as UTC with `timestamp with time zone`.
- Convert nullable `DateOnly` properties for research group membership dates.
- Apply audit timestamps in `SaveChanges` and `SaveChangesAsync`.

Default connection string is in `AppConstants.DefaultConnectionString`; runtime configuration can resolve environment-style placeholders through `ConfigurationValueResolver`.

# Naming Conventions

Controllers:

- `XController`
- Route attributes stay stable and must not be changed casually.
- CRUD controllers inherit `BaseCrudController`.

Services:

- `IXService` in Application.
- `XService` in Infrastructure.
- Async method names end with `Async`.

Repositories:

- `IXRepository` in Application.
- `XRepository` in Infrastructure.
- Query names describe intent, e.g. `FindByIdAsync`, `FindByEmailAsync`, `FindByPaperAndIdAsync`.

DTOs:

- `CreateXRequest`
- `UpdateXRequest`
- `XResponse`
- No `Dto` or `DTO` suffix.

Mappers:

- `XMapper`
- `ToResponse`
- `UpdateEntity`

Options:

- `XOptions` in Infrastructure options folder.

Error codes:

- `XErrorCode` static holder class.
- Each member is `public static readonly IErrorCode`.

# Refactoring Rules

Follow these rules for all edits:

- Preserve business logic.
- Preserve routes unless the task explicitly requests route changes.
- Preserve authorization policies.
- Preserve validation behavior and messages.
- Preserve response shape and response messages.
- Preserve architecture boundaries and dependency flow.
- Do not introduce `Dto/DTO` suffixes.
- Do not introduce `HttpStatusCode` into error code handling.
- Prefer manual mappers matching existing `Application/Mappings` patterns.
- Use `BaseCrudController` for full CRUD controllers.
- Do not duplicate CRUD actions already inherited from `BaseCrudController`.
- Keep custom business endpoints in concrete controllers.
- Run targeted `rg` checks after naming/error refactors.
- Run `dotnet build PublicationQualitySystem.sln --no-restore` when possible.

# Technical Debt

- No FluentValidation validators currently exist despite validation needs.
- `Application.DependencyInjection` is currently empty.
- Some response mapping is inline in services, especially upload response mapping, while most domain mapping uses static mapper classes.
- `OcrController` inherits `ControllerBase` directly instead of `ApiBaseController`.
- Build can fail when `PublicationQualitySystem.Api.exe` is already running and locking DLLs; stop the running API before a full solution build.
- Ensure static error-code holder classes never implement `IErrorCode`.
- There are no explicit pagination response models; list endpoints currently return `List<T>` inside `BaseResponse<T>`.

