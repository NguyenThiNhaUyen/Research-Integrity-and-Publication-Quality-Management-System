using System.Net.Http.Json;
using System.Diagnostics;
using System.Text.Json.Serialization;
using PublicationQualitySystem.Application.Services.Interfaces;
using PublicationQualitySystem.Shared.Exceptions;

namespace PublicationQualitySystem.Infrastructure.Services.Implementations;

public class NougatService(HttpClient httpClient, ILogger<NougatService> logger) : INougatService
{
    public async Task<string> ConvertPdfToMarkdownAsync(
        Stream pdfStream,
        string fileName,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        logger.LogInformation(
            "Nougat request started. BaseAddress={BaseAddress}, FileName={FileName}, CanSeek={CanSeek}, Length={Length}",
            httpClient.BaseAddress,
            fileName,
            pdfStream.CanSeek,
            pdfStream.CanSeek ? pdfStream.Length : (long?)null);

        using var content = new MultipartFormDataContent();
        using var fileContent = new StreamContent(pdfStream);
        fileContent.Headers.ContentType = new("application/pdf");
        content.Add(fileContent, "file", fileName);

        HttpResponseMessage response;
        try
        {
            response = await httpClient.PostAsync("/api/nougat/convert", content, cancellationToken);
            logger.LogInformation(
                "Nougat HTTP response received. StatusCode={StatusCode}, ReasonPhrase={ReasonPhrase}, ElapsedMs={ElapsedMs}",
                (int)response.StatusCode,
                response.ReasonPhrase,
                stopwatch.ElapsedMilliseconds);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(
                ex,
                "Nougat HTTP request failed. BaseAddress={BaseAddress}, FileName={FileName}, ElapsedMs={ElapsedMs}",
                httpClient.BaseAddress,
                fileName,
                stopwatch.ElapsedMilliseconds);
            throw new AppException(NougatErrorCode.ServiceUnavailable, ex.Message);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(
                ex,
                "Nougat HTTP request timed out. BaseAddress={BaseAddress}, FileName={FileName}, Timeout={Timeout}, ElapsedMs={ElapsedMs}",
                httpClient.BaseAddress,
                fileName,
                httpClient.Timeout,
                stopwatch.ElapsedMilliseconds);
            throw new AppException(NougatErrorCode.ServiceUnavailable, ex.Message);
        }

        var result = await response.Content.ReadFromJsonAsync<NougatConvertResponse>(cancellationToken: cancellationToken);
        if (result is null)
        {
            logger.LogWarning(
                "Nougat response body could not be deserialized. StatusCode={StatusCode}, FileName={FileName}, ElapsedMs={ElapsedMs}",
                (int)response.StatusCode,
                fileName,
                stopwatch.ElapsedMilliseconds);
            throw new AppException(NougatErrorCode.InvalidResponse);
        }

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "Nougat returned non-success HTTP status. StatusCode={StatusCode}, Success={Success}, Error={Error}, FileName={FileName}, ElapsedMs={ElapsedMs}",
                (int)response.StatusCode,
                result.Success,
                result.Error,
                fileName,
                stopwatch.ElapsedMilliseconds);
            throw new AppException(NougatErrorCode.ServiceUnavailable);
        }

        if (!result.Success)
        {
            logger.LogWarning(
                "Nougat returned success=false. Error={Error}, FileName={FileName}, ElapsedMs={ElapsedMs}",
                result.Error,
                fileName,
                stopwatch.ElapsedMilliseconds);
            throw new AppException(NougatErrorCode.InvalidResponse);
        }

        logger.LogInformation(
            "Nougat request completed. FileName={FileName}, MarkdownLength={MarkdownLength}, ElapsedMs={ElapsedMs}",
            fileName,
            result.Markdown?.Length ?? 0,
            stopwatch.ElapsedMilliseconds);

        return result.Markdown ?? string.Empty;
    }

    private sealed class NougatConvertResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("markdown")]
        public string? Markdown { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }
}
