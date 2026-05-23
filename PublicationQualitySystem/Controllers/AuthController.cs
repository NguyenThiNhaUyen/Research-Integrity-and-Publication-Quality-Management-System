using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PublicationQualitySystem.Common;
using PublicationQualitySystem.DTOs;
using PublicationQualitySystem.Services;

namespace PublicationQualitySystem.Controllers;

[Route("api/auth")]
public class AuthController(IAuthService authService) : ApiBaseController
{
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<BaseResponse<AuthResponseDto>>> Register([FromBody] RegisterRequestDto request) =>
        CreatedResponse(await authService.RegisterAsync(request));

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<BaseResponse<AuthResponseDto>>> Login([FromBody] LoginRequestDto request) =>
        OkResponse(await authService.LoginAsync(request), "Login successfully");

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<ActionResult<BaseResponse<AuthResponseDto>>> RefreshToken([FromBody] RefreshTokenRequestDto request) =>
        OkResponse(await authService.RefreshTokenAsync(request), "Token refreshed successfully");

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<ActionResult<BaseResponse<object>>> Logout([FromBody] LogoutRequestDto request)
    {
        await authService.LogoutAsync(request);
        return OkResponse<object>(null, "Logout successfully");
    }

    [HttpGet("me")]
    [Authorize]
    public ActionResult<BaseResponse<CurrentUserDto>> Me()
    {
        var dto = new CurrentUserDto
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
