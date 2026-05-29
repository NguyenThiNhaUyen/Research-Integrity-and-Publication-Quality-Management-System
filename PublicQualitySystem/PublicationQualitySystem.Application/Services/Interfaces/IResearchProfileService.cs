using PublicationQualitySystem.Shared.Common;
using PublicationQualitySystem.Application.DTOs.Auth;
using PublicationQualitySystem.Application.DTOs.File;
using PublicationQualitySystem.Application.DTOs.ResearchGroup;
using PublicationQualitySystem.Application.DTOs.ResearchProfile;
using PublicationQualitySystem.Application.DTOs.Role;
using PublicationQualitySystem.Application.DTOs.User;

namespace PublicationQualitySystem.Application.Services.Interfaces;

public interface IResearchProfileService : IBaseCrudService<CreateResearchProfileRequest, UpdateResearchProfileRequest, ResearchProfileResponse, long>
{
    Task<ResearchProfileResponse> GetByUserIdAsync(string userId);
}
