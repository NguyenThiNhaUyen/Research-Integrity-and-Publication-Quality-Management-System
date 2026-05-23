using PublicationQualitySystem.Common;
using PublicationQualitySystem.DTOs.Auth;
using PublicationQualitySystem.DTOs.File;
using PublicationQualitySystem.DTOs.ResearchGroup;
using PublicationQualitySystem.DTOs.ResearchProfile;
using PublicationQualitySystem.DTOs.Role;
using PublicationQualitySystem.DTOs.User;

namespace PublicationQualitySystem.Services.Interfaces;

public interface IResearchProfileService : IBaseCrudService<ResearchProfileDto>
{
    Task<ResearchProfileDto> GetByUserIdAsync(string userId);
}
