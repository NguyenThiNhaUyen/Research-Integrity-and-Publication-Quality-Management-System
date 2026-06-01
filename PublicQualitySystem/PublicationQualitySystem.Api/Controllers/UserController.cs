using Microsoft.AspNetCore.Mvc;
using PublicationQualitySystem.Api.Common;
using PublicationQualitySystem.Shared.Common;

using PublicationQualitySystem.Application.DTOs.Role;
using PublicationQualitySystem.Application.DTOs.User;
using PublicationQualitySystem.Application.Services.Interfaces;

namespace PublicationQualitySystem.Api.Controllers;

[Route("api/lab-members/users")]
public class UserController(IUserService service) : BaseCrudController<
    CreateUserRequest,
    UpdateUserRequest,
    UserResponse,
    string>(service)
{
}
