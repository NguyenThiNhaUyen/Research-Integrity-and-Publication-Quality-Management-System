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

[Route("api/lab-members/profiles")]
public class ResearchProfileController(IResearchProfileService profileService) : BaseCrudController<
    CreateResearchProfileRequest,
    UpdateResearchProfileRequest,
    ResearchProfileResponse,
    long>(profileService)
{
    [HttpGet("user/{userId}")]
    [Authorize(Policy = "RESEARCH_PROFILE_READ")]
    public async Task<ActionResult<BaseResponse<ResearchProfileResponse>>> GetByUserId(string userId) =>
        OkResponse(await profileService.GetByUserIdAsync(userId), "Profile retrieved successfully");
}
