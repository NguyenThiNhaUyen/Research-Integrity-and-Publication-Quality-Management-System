# PublicationQualitySystem API Inventory

Danh sach nay duoc lap tu controller hien tai trong `PublicationQualitySystem/Controllers`. Fallback authorization policy cua project yeu cau authenticated user cho moi endpoint tru cac endpoint co `[AllowAnonymous]`.

## Summary

| Controller | So endpoint | Module |
|---|---:|---|
| `AuthController` | 5 | Auth |
| `UserController` | 4 | Users |
| `RoleController` | 9 | Roles |
| `ResearchProfileController` | 5 | Research Profiles |
| `ResearchGroupController` | 12 | Research Groups |
| `PaperController` | 1 | Papers/Nougat OCR |

Tong endpoint hien tai: can duoc cap nhat lai sau dot reset Paper/OCR.

## AuthController

Base route: `/api/auth`

| Method | Route | Action | Request DTO | Response DTO | Authorization | Policy/Permission | JWT Required | Ghi chu |
|---|---|---|---|---|---|---|---|---|
| POST | `/api/auth/register` | `Register` | `RegisterRequestDto` | `BaseResponse<AuthResponseDto>` | `[AllowAnonymous]` | None | No | Dang tao Cognito user va DB user |
| POST | `/api/auth/login` | `Login` | `LoginRequestDto` | `BaseResponse<AuthResponseDto>` | `[AllowAnonymous]` | None | No | Tra access/refresh/id token |
| POST | `/api/auth/refresh-token` | `RefreshToken` | `RefreshTokenRequestDto` | `BaseResponse<AuthResponseDto>` | `[AllowAnonymous]` | None | No | Refresh token Cognito |
| POST | `/api/auth/logout` | `Logout` | `LogoutRequestDto` | `BaseResponse<object>` | `[AllowAnonymous]` | None | No | Body co access token |
| GET | `/api/auth/me` | `Me` | None | `BaseResponse<CurrentUserDto>` | `[Authorize]` | Authenticated user | Yes | Doc claim tu JWT |

## UserController

Base route: `/api/lab-members/users`

| Method | Route | Action | Request DTO | Response DTO | Authorization | Policy/Permission | JWT Required | Ghi chu |
|---|---|---|---|---|---|---|---|---|
| POST | `/api/lab-members/users` | `Create` | `UserDto` | `BaseResponse<UserDto>` | Fallback policy | Missing explicit policy | Yes | Nen dung `LAB_MEMBER_CREATE` hoac `USER_CREATE` |
| GET | `/api/lab-members/users/{id}` | `GetById` | None | `BaseResponse<UserDto>` | Fallback policy | Missing explicit policy | Yes | Nen dung `LAB_MEMBER_READ` hoac `USER_READ` |
| PUT | `/api/lab-members/users/{id}` | `Update` | `UserDto` | `BaseResponse<UserDto>` | Fallback policy | Missing explicit policy | Yes | Nen dung `LAB_MEMBER_UPDATE` hoac `USER_UPDATE` |
| DELETE | `/api/lab-members/users/{id}` | `Delete` | None | `BaseResponse<object>` | Fallback policy | Missing explicit policy | Yes | Nen dung `LAB_MEMBER_DELETE` hoac `USER_DELETE` |

## RoleController

Base route: `/api/roles`

| Method | Route | Action | Request DTO | Response DTO | Authorization | Policy/Permission | JWT Required | Ghi chu |
|---|---|---|---|---|---|---|---|---|
| POST | `/api/roles` | `Create` | `RoleDto` | `BaseResponse<RoleDto>` | Fallback policy | Missing `ROLE_CREATE` | Yes | Tao DB role va Cognito group |
| GET | `/api/roles/{id}` | `GetById` | None | `BaseResponse<RoleDto>` | Fallback policy | Missing explicit `ROLE_READ` | Yes | |
| PUT | `/api/roles/{id}` | `Update` | `RoleDto` | `BaseResponse<RoleDto>` | Fallback policy | Missing `ROLE_UPDATE` | Yes | `UpdateRoleDto` ton tai nhung chua dung |
| DELETE | `/api/roles/{id}` | `Delete` | None | `BaseResponse<object>` | Fallback policy | Missing `ROLE_DELETE` | Yes | Soft delete role |
| GET | `/api/roles?page=0&size=20` | `GetAll` | Query: `page`, `size` | `BaseResponse<List<RoleDto>>` | `[Authorize]` | `ROLE_READ` | Yes | |
| GET | `/api/roles/users/{userId}` | `GetUserRoles` | None | `BaseResponse<List<UserRoleDto>>` | `[Authorize]` | `ROLE_READ` | Yes | |
| POST | `/api/roles/users/{userId}/{roleId}` | `AssignRole` | None | `BaseResponse<List<UserRoleDto>>` | `[Authorize]` | `ROLE_ASSIGN` | Yes | Route co the chuan hoa lai |
| DELETE | `/api/roles/users/{userId}/{roleId}` | `RemoveRole` | None | `BaseResponse<List<UserRoleDto>>` | `[Authorize]` | `ROLE_ASSIGN` | Yes | |
| PUT | `/api/roles/users/{userId}` | `ReplaceRoles` | `UpdateUserRolesDto` | `BaseResponse<List<UserRoleDto>>` | `[Authorize]` | `ROLE_ASSIGN` | Yes | |

## ResearchProfileController

Base route: `/api/lab-members/profiles`

| Method | Route | Action | Request DTO | Response DTO | Authorization | Policy/Permission | JWT Required | Ghi chu |
|---|---|---|---|---|---|---|---|---|
| POST | `/api/lab-members/profiles` | `Create` | `ResearchProfileDto` | `BaseResponse<ResearchProfileDto>` | Fallback policy | Missing `RESEARCH_PROFILE_CREATE` | Yes | |
| GET | `/api/lab-members/profiles/{id}` | `GetById` | None | `BaseResponse<ResearchProfileDto>` | Fallback policy | Missing explicit `RESEARCH_PROFILE_READ` | Yes | |
| PUT | `/api/lab-members/profiles/{id}` | `Update` | `ResearchProfileDto` | `BaseResponse<ResearchProfileDto>` | Fallback policy | Missing `RESEARCH_PROFILE_UPDATE` | Yes | |
| DELETE | `/api/lab-members/profiles/{id}` | `Delete` | None | `BaseResponse<object>` | Fallback policy | Missing `RESEARCH_PROFILE_DELETE` | Yes | |
| GET | `/api/lab-members/profiles/user/{userId}` | `GetByUserId` | None | `BaseResponse<ResearchProfileDto>` | `[Authorize]` | `RESEARCH_PROFILE_READ` | Yes | |

## ResearchGroupController

Base route: `/api/lab-members/research-groups`

| Method | Route | Action | Request DTO | Response DTO | Authorization | Policy/Permission | JWT Required | Ghi chu |
|---|---|---|---|---|---|---|---|---|
| POST | `/api/lab-members/research-groups` | `Create` | `ResearchGroupDto` | `BaseResponse<ResearchGroupDto>` | Fallback policy | Missing `RESEARCH_GROUP_CREATE` | Yes | |
| GET | `/api/lab-members/research-groups/{id}` | `GetById` | None | `BaseResponse<ResearchGroupDto>` | Fallback policy | Missing explicit `RESEARCH_GROUP_READ` | Yes | |
| PUT | `/api/lab-members/research-groups/{id}` | `Update` | `ResearchGroupDto` | `BaseResponse<ResearchGroupDto>` | Fallback policy | Missing `RESEARCH_GROUP_UPDATE` | Yes | |
| DELETE | `/api/lab-members/research-groups/{id}` | `Delete` | None | `BaseResponse<object>` | Fallback policy | Missing `RESEARCH_GROUP_DELETE` | Yes | |
| GET | `/api/lab-members/research-groups?page=0&size=20` | `GetAll` | Query: `page`, `size` | `BaseResponse<List<ResearchGroupDto>>` | `[Authorize]` | `RESEARCH_GROUP_READ` | Yes | |
| POST | `/api/lab-members/research-groups/{groupId}/members` | `AddMember` | `ResearchGroupMemberDto` | `BaseResponse<ResearchGroupMemberDto>` | `[Authorize]` | `RESEARCH_GROUP_MEMBER_MANAGE` | Yes | |
| GET | `/api/lab-members/research-groups/{groupId}/members` | `GetMembers` | None | `BaseResponse<List<ResearchGroupMemberDto>>` | `[Authorize]` | `RESEARCH_GROUP_READ` | Yes | |
| DELETE | `/api/lab-members/research-groups/{groupId}/members/{userId}` | `RemoveMember` | None | `BaseResponse<object>` | `[Authorize]` | `RESEARCH_GROUP_MEMBER_MANAGE` | Yes | |
| PATCH | `/api/lab-members/research-groups/{groupId}/members/{userId}/role` | `ChangeMemberRole` | `ResearchGroupMemberDto` | `BaseResponse<ResearchGroupMemberDto>` | `[Authorize]` | `RESEARCH_GROUP_MEMBER_MANAGE` | Yes | |
| PATCH | `/api/lab-members/research-groups/{groupId}/members/{userId}/status` | `UpdateMemberStatus` | `ResearchGroupMemberDto` | `BaseResponse<ResearchGroupMemberDto>` | `[Authorize]` | `RESEARCH_GROUP_MEMBER_MANAGE` | Yes | |
| PATCH | `/api/lab-members/research-groups/{groupId}/leader/{userId}` | `AssignLeader` | None | `BaseResponse<ResearchGroupMemberDto>` | `[Authorize]` | `RESEARCH_GROUP_MEMBER_MANAGE` | Yes | |
| GET | `/api/lab-members/research-groups/users/{userId}` | `GetGroupsByUser` | None | `BaseResponse<List<ResearchGroupDto>>` | `[Authorize]` | `RESEARCH_GROUP_READ` | Yes | |

## PaperController

Base route: `/api/papers`

| Method | Route | Action | Request DTO | Response DTO | Authorization | Policy/Permission | JWT Required | Ghi chu |
|---|---|---|---|---|---|---|---|---|
| POST | `/api/papers/upload` | `Upload` | `multipart/form-data`: `file`, `title?` | `BaseResponse<PaperVersionResponse>` | Fallback policy | Authenticated user | Yes | Upload PDF len S3, tao Paper/PaperVersion, goi Nougat, upload Markdown len S3 |

## Missing API By Workflow

| Module | API status | Ghi chu |
|---|---|---|
| Permissions | Missing public API | Entity/repository/enum co |
| Papers | Initial upload API done | `POST /api/papers/upload` |
| Paper Versions | Created by upload workflow | Chua co list/get endpoint |
| Authors | Missing API | Entity co |
| Quality Reports | Missing API | Permission enum co |
| Integrity Reports | Missing API | Permission enum co |
| Internal Reviews | Missing API | Permission enum co |
| Reviewer Comments | Missing API | Chua thay entity |
| Reviewer Responses | Missing API | Permission enum co |
| Venues | Missing API | Permission enum co |
| Venue Recommendations | Missing API | Permission enum co |
| Submissions | Missing API | `SubmissionStatus` enum co |
| Notifications | Missing API | Permission enum co |
| Dashboard/Analytics | Missing API | Permission enum co |
| Audit Logs | Missing API | Permission enum co |

## API Consistency Checklist

| Kiem tra | Ket qua | Can sua |
|---|---|---|
| Duplicate endpoint | Khong thay | None |
| RESTful naming | Tuong doi | `api/s3` va role-user route nen chuan hoa |
| API versioning | Chua co | Them `/api/v1` |
| JWT requirement | Co fallback policy | Can document ro trong Swagger/Postman |
| Permission requirement | Co mot phan | Gan policy cho CRUD endpoint |
| Response wrapper | Co | `BaseResponse<T>` |
| Validation response | Co mot phan | Them `errors[]` neu muon standard QA |
| Raw entity response | Khong thay | Dang dung DTO |
| Swagger/OpenAPI | Co | Can annotations/examples |
