using System.Diagnostics;
using PublicationQualitySystem.Application.DTOs.Grobid;
using PublicationQualitySystem.Application.Services.Interfaces;
using PublicationQualitySystem.Shared.Exceptions;

namespace PublicationQualitySystem.Infrastructure.Services.Implementations;

public class GrobidService(HttpClient httpClient, ILogger<GrobidService> logger) : IGrobidService
{
    public async Task<GrobidMetadataResponse> ExtractMetadataAsync(
        Stream pdfStream,
        string fileName,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        logger.LogInformation(
            "GROBID request sent. BaseAddress={BaseAddress}, FileName={FileName}, CanSeek={CanSeek}, Length={Length}",
            httpClient.BaseAddress,
            fileName,
            pdfStream.CanSeek,
            pdfStream.CanSeek ? pdfStream.Length : (long?)null);

        using var content = new MultipartFormDataContent();
        using var fileContent = new StreamContent(pdfStream);
        fileContent.Headers.ContentType = new("application/pdf");
        content.Add(fileContent, "input", fileName);

        HttpResponseMessage response;
        try
        {
            response = await httpClient.PostAsync("/api/processFulltextDocument", content, cancellationToken);
            logger.LogInformation(
                "GROBID response received. StatusCode={StatusCode}, ReasonPhrase={ReasonPhrase}, ElapsedMs={ElapsedMs}",
                (int)response.StatusCode,
                response.ReasonPhrase,
                stopwatch.ElapsedMilliseconds);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(
                ex,
                "GROBID HTTP request failed. BaseAddress={BaseAddress}, FileName={FileName}, ElapsedMs={ElapsedMs}",
                httpClient.BaseAddress,
                fileName,
                stopwatch.ElapsedMilliseconds);
            throw new AppException(GrobidErrorCode.ServiceUnavailable, ex.Message);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(
                ex,
                "GROBID HTTP request timed out. BaseAddress={BaseAddress}, FileName={FileName}, Timeout={Timeout}, ElapsedMs={ElapsedMs}",
                httpClient.BaseAddress,
                fileName,
                httpClient.Timeout,
                stopwatch.ElapsedMilliseconds);
            throw new AppException(GrobidErrorCode.ServiceUnavailable, ex.Message);
        }

        var xml = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "GROBID returned non-success HTTP status. StatusCode={StatusCode}, FileName={FileName}, BodyLength={BodyLength}, ElapsedMs={ElapsedMs}",
                (int)response.StatusCode,
                fileName,
                xml.Length,
                stopwatch.ElapsedMilliseconds);
            throw new AppException(GrobidErrorCode.ServiceUnavailable);
        }

        if (string.IsNullOrWhiteSpace(xml))
        {
            logger.LogWarning(
                "GROBID returned an empty response body. FileName={FileName}, ElapsedMs={ElapsedMs}",
                fileName,
                stopwatch.ElapsedMilliseconds);
            throw new AppException(GrobidErrorCode.InvalidResponse);
        }

        try
        {
            var metadata = GrobidTeiParser.Parse(xml, logger);
            metadata.RawGrobidXml = xml;

            logger.LogInformation(
                "GROBID metadata extraction parsed. FileName={FileName}, TitlePresent={TitlePresent}, Authors={Authors}, References={References}, ElapsedMs={ElapsedMs}",
                fileName,
                !string.IsNullOrWhiteSpace(metadata.Title),
                metadata.Authors.Count,
                metadata.References.Count,
                stopwatch.ElapsedMilliseconds);

            return metadata;
        }
        catch (Exception ex) when (ex is not AppException)
        {
            logger.LogError(
                ex,
                "GROBID response parsing failed. FileName={FileName}, ElapsedMs={ElapsedMs}",
                fileName,
                stopwatch.ElapsedMilliseconds);
            throw new AppException(GrobidErrorCode.InvalidResponse, ex.Message);
        }
    }
}
