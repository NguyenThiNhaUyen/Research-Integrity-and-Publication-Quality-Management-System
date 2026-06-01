using PublicationQualitySystem.Application.DTOs.Crossref;

namespace PublicationQualitySystem.Application.Services.Interfaces;

public interface ICrossrefService
{
    Task<CrossrefMetadataResponse?> GetWorkByDoiAsync(
        string doi,
        CancellationToken cancellationToken);
}
