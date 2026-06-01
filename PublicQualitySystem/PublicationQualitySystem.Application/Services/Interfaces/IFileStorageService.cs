namespace PublicationQualitySystem.Application.Services.Interfaces;

public interface IFileStorageService
{
    Task<string> UploadAsync(
        Stream content,
        string key,
        string contentType,
        CancellationToken cancellationToken);

    Task<Stream> DownloadAsync(
        string key,
        CancellationToken cancellationToken);
}
