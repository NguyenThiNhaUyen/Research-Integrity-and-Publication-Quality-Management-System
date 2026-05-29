using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PublicationQualitySystem.Api.Common;
using PublicationQualitySystem.Shared.Common;
using PublicationQualitySystem.Application.DTOs.Auth;
using PublicationQualitySystem.Application.DTOs.File;
using PublicationQualitySystem.Application.DTOs.ResearchGroup;
using PublicationQualitySystem.Application.DTOs.ResearchProfile;
using PublicationQualitySystem.Application.DTOs.Role;
using PublicationQualitySystem.Application.DTOs.User;
using PublicationQualitySystem.Application.Services.Interfaces;

namespace PublicationQualitySystem.Api.Controllers;

[Route("api/auth")]
public class AuthController(IAuthService authService) : ApiBaseController
{
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<BaseResponse<AuthResponse>>> Register([FromBody] RegisterRequest request) =>
        CreatedResponse(await authService.RegisterAsync(request));

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<BaseResponse<AuthResponse>>> Login([FromBody] LoginRequest request) =>
        OkResponse(await authService.LoginAsync(request), "Login successfully");

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<ActionResult<BaseResponse<AuthResponse>>> RefreshToken([FromBody] RefreshTokenRequest request) =>
        OkResponse(await authService.RefreshTokenAsync(request), "Token refreshed successfully");

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<ActionResult<BaseResponse<object>>> Logout([FromBody] LogoutRequest request)
    {
        await authService.LogoutAsync(request);
        return OkResponse<object>(null, "Logout successfully");
    }

    [HttpGet("me")]
    [Authorize]
    public ActionResult<BaseResponse<CurrentUserResponse>> Me()
    {
        var dto = new CurrentUserResponse
        {
            FullName = User.FindFirst("full_name")?.Value,
            Email = User.FindFirst("email")?.Value ?? User.Identity?.Name,
            Sub = User.FindFirst("sub")?.Value,
            Authorities = User.Claims.Where(c => c.Type is "permission" or "role" or "scope")
                .Select(c => c.Value)
                .Distinct()
                .ToList()
        };
        return OkResponse(dto, "Current user retrieved successfully");
    }
}
