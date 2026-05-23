using PublicationQualitySystem.Common;
using PublicationQualitySystem.DTOs;

namespace PublicationQualitySystem.Services;

public interface IResearchProfileService : IBaseCrudService<ResearchProfileDto>
{
    Task<ResearchProfileDto> GetByUserIdAsync(string userId);
}
