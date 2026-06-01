namespace PublicationQualitySystem.Application.Services.Interfaces;

public interface INougatService
{
    Task<string> ConvertPdfToMarkdownAsync(
        Stream pdfStream,
        string fileName,
        CancellationToken cancellationToken);
}
