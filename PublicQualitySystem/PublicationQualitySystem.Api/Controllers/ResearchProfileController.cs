using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PublicationQualitySystem.Api.Common;
using PublicationQualitySystem.Shared.Common;
using PublicationQualitySystem.Application.DTOs.ResearchProfile.Requests;
using PublicationQualitySystem.Application.DTOs.ResearchProfile.Responses;
using PublicationQualitySystem.Application.Services.Interfaces;

namespace PublicationQualitySystem.Api.Controllers;

[Route("api/lab-members/profiles")]
public class ResearchProfileController(IResearchProfileService profileService) : ApiBaseController
{
    [HttpPost]
    public async Task<ActionResult<BaseResponse<ResearchProfileResponseDto>>> Create([FromBody] CreateResearchProfileRequestDto dto) => CreatedResponse(await profileService.CreateAsync(dto));

    [HttpGet("{id:long}")]
    public async Task<ActionResult<BaseResponse<ResearchProfileResponseDto>>> GetById(long id) => OkResponse(await profileService.GetByIdAsync(id), "Get by id successfully");

    [HttpPut("{id:long}")]
    public async Task<ActionResult<BaseResponse<ResearchProfileResponseDto>>> Update(long id, [FromBody] UpdateResearchProfileRequestDto dto) => OkResponse(await profileService.UpdateAsync(id, dto), "Update successfully");

    [HttpDelete("{id:long}")]
    public async Task<ActionResult<BaseResponse<object>>> Delete(long id)
    {
        await profileService.DeleteAsync(id);
        return OkResponse<object>(null, "Delete successfully");
    }

    [HttpGet("user/{userId}")]
    [Authorize(Policy = "RESEARCH_PROFILE_READ")]
    public async Task<ActionResult<BaseResponse<ResearchProfileResponseDto>>> GetByUserId(string userId) =>
        OkResponse(await profileService.GetByUserIdAsync(userId), "Profile retrieved successfully");
}
