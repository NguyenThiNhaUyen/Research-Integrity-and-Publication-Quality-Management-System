using PublicationQualitySystem.Application.DTOs.Grobid;
using PublicationQualitySystem.Application.DTOs.References;

namespace PublicationQualitySystem.Application.Services.Interfaces;

public interface IReferenceQualityService
{
    ReferenceQualityResult Evaluate(ReferenceDto reference);

    int CalculateReferenceQualityScore(
        IReadOnlyList<ReferenceQualityResult> references,
        int validDoiCount,
        int invalidDoiCount,
        int titleMismatchCount);
}
