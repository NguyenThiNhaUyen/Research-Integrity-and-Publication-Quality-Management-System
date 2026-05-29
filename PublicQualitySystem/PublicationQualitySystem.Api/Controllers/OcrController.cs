using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PublicationQualitySystem.Application.DTOs.Ocr;
using PublicationQualitySystem.Application.Services.Interfaces;
using PublicationQualitySystem.Shared.Exceptions;

namespace PublicationQualitySystem.Api.Controllers;

[ApiController]
[Route("api/ocr")]
public class OcrController(
    IPdfOcrService pdfOcrService,
    IUploadService uploadService,
    ILogger<OcrController> logger) : ControllerBase
{
    [HttpPost("extract-text")]
    [Authorize]
    public async Task<ActionResult<OcrResponse>> ExtractText([FromBody] OcrRequest request)
    {
        try
        {
            if (request is null || request.FileId <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "FileId is required"
                });
            }

            var file = await uploadService.GetFileAsync(request.FileId);
            var objectKey = string.IsNullOrWhiteSpace(file.S3Key) ? file.Key : file.S3Key;
            var text = await pdfOcrService.ExtractTextAsync(objectKey);

            return Ok(new OcrResponse
            {
                Success = true,
                FileId = file.FileId,
                ObjectKey = objectKey,
                Text = text
            });
        }
        catch (AppException exception)
        {
            var statusCode = (int)exception.ErrorCode.StatusCode;
            return StatusCode(statusCode, new
            {
                success = false,
                code = statusCode,
                message = exception.Message
            });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "OCR extract text failed for file {FileId}", request?.FileId);
            return StatusCode(500, new
            {
                success = false,
                code = 500,
                message = exception.Message
            });
        }
    }
}
