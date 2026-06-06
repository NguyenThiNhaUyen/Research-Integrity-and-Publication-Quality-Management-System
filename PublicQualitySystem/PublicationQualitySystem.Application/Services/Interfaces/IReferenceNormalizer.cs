using PublicationQualitySystem.Application.DTOs.Grobid;
using PublicationQualitySystem.Application.DTOs.References;

namespace PublicationQualitySystem.Application.Services.Interfaces;

public interface IReferenceNormalizer
{
    ReferenceDto Normalize(ReferenceDto reference);

    ReferenceNormalizationResult NormalizeDetailed(ReferenceDto reference);

    string? NormalizeDoi(string? doi);

    bool IsValidDoiFormat(string? doi);
}
