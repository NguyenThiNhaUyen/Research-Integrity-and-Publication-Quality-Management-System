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
public class ResearchProfileController(IResearchProfileService profileService) : ApiBaseController
{
    [HttpPost]
    public async Task<ActionResult<BaseResponse<ResearchProfileDto>>> Create([FromBody] ResearchProfileDto dto) => CreatedResponse(await profileService.CreateAsync(dto));

    [HttpGet("{id:long}")]
    public async Task<ActionResult<BaseResponse<ResearchProfileDto>>> GetById(long id) => OkResponse(await profileService.GetByIdAsync(id), "Get by id successfully");

    [HttpPut("{id:long}")]
    public async Task<ActionResult<BaseResponse<ResearchProfileDto>>> Update(long id, [FromBody] ResearchProfileDto dto) => OkResponse(await profileService.UpdateAsync(id, dto), "Update successfully");

    [HttpDelete("{id:long}")]
    public async Task<ActionResult<BaseResponse<object>>> Delete(long id)
    {
        await profileService.DeleteAsync(id);
        return OkResponse<object>(null, "Delete successfully");
    }

    [HttpGet("user/{userId}")]
    [Authorize(Policy = "RESEARCH_PROFILE_READ")]
    public async Task<ActionResult<BaseResponse<ResearchProfileDto>>> GetByUserId(string userId) =>
        OkResponse(await profileService.GetByUserIdAsync(userId), "Profile retrieved successfully");
}
