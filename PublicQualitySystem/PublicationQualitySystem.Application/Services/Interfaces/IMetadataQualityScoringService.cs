using PublicationQualitySystem.Application.DTOs.Metadata;
using PublicationQualitySystem.Domain.Entities;

namespace PublicationQualitySystem.Application.Services.Interfaces;

public interface IMetadataQualityScoringService
{
    MetadataQualityScoreResponse Calculate(PaperMetadata metadata);
}
