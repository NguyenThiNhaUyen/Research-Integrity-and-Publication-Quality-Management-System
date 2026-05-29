using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PublicationQualitySystem.Api.Common;
using PublicationQualitySystem.Application.DTOs.PaperReviewProfile;
using PublicationQualitySystem.Application.Services.Interfaces;
using PublicationQualitySystem.Shared.Common;

namespace PublicationQualitySystem.Api.Controllers;

[Route("api/papers/{paperId:long}/review-profile")]
[Authorize]
public class PaperReviewProfileController(IPaperReviewProfileService reviewProfileService) : ApiBaseController
{
    [HttpPost]
    public async Task<ActionResult<BaseResponse<PaperReviewProfileResponse>>> CreateOrUpdate(
        long paperId,
        [FromBody] CreatePaperReviewProfileRequest request) =>
        OkResponse(await reviewProfileService.CreateOrUpdateAsync(paperId, request), "Save paper review profile successfully");

    [HttpGet]
    public async Task<ActionResult<BaseResponse<PaperReviewProfileResponse>>> Get(long paperId) =>
        OkResponse(await reviewProfileService.GetByPaperIdAsync(paperId), "Get paper review profile successfully");
}
