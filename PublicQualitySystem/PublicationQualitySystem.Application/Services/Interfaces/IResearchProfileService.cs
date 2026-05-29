using PublicationQualitySystem.Shared.Common;
using PublicationQualitySystem.Application.DTOs.ResearchProfile.Requests;
using PublicationQualitySystem.Application.DTOs.ResearchProfile.Responses;

namespace PublicationQualitySystem.Application.Services.Interfaces;

public interface IResearchProfileService : IBaseCrudService<ResearchProfileResponseDto, CreateResearchProfileRequestDto, UpdateResearchProfileRequestDto>
{
    Task<ResearchProfileResponseDto> GetByUserIdAsync(string userId);
}
