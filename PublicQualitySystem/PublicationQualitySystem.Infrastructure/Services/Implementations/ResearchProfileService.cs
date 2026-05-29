using Microsoft.EntityFrameworkCore;
using PublicationQualitySystem.Infrastructure.Configurations;
using PublicationQualitySystem.Application.DTOs.ResearchProfile.Requests;
using PublicationQualitySystem.Application.DTOs.ResearchProfile.Responses;
using PublicationQualitySystem.Domain.Entities;
using PublicationQualitySystem.Domain.Enums;
using PublicationQualitySystem.Shared.Exceptions;
using PublicationQualitySystem.Application.Mappings;
using PublicationQualitySystem.Application.Repositories.Interfaces;
using PublicationQualitySystem.Application.Services.Interfaces;

namespace PublicationQualitySystem.Infrastructure.Services.Implementations;

public class ResearchProfileService(ApplicationDbContext db, IUserRepository users) : IResearchProfileService
{
    public async Task<ResearchProfileResponseDto> CreateAsync(CreateResearchProfileRequestDto dto)
    {
        var user = await users.FindByIdAsync(dto.UserId) ?? throw new AppException(UserErrorCode.UserNotFound);
        var profile = new ResearchProfile
        {
            User = user,
            UserId = user.Id,
            AvatarUrl = dto.AvatarUrl,
            Institution = dto.Institution ?? string.Empty,
            Specialization = dto.Specialization,
            Orcid = dto.Orcid,
            ResearchInterests = dto.ResearchInterests,
            AcademicRank = dto.AcademicRank ?? AcademicRank.RESEARCHER,
            Status = dto.Status ?? MemberStatus.ACTIVE
        };
        db.ResearchProfiles.Add(profile);
        await db.SaveChangesAsync();
        return ResearchProfileMapper.ToDto(profile);
    }

    public async Task<ResearchProfileResponseDto> GetByIdAsync(long id)
    {
        var profile = await db.ResearchProfiles.FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new AppException(ResearchProfileErrorCode.ProfileNotFound);
        return ResearchProfileMapper.ToDto(profile);
    }

    public async Task<ResearchProfileResponseDto> GetByUserIdAsync(string userId)
    {
        var profile = await db.ResearchProfiles.FirstOrDefaultAsync(p => p.UserId == userId)
            ?? throw new AppException(ResearchProfileErrorCode.ProfileNotFound);
        return ResearchProfileMapper.ToDto(profile);
    }

    public async Task<ResearchProfileResponseDto> UpdateAsync(long id, UpdateResearchProfileRequestDto dto)
    {
        var profile = await db.ResearchProfiles.FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new AppException(ResearchProfileErrorCode.ProfileNotFound);
        ResearchProfileMapper.UpdateEntity(profile, dto);
        await db.SaveChangesAsync();
        return ResearchProfileMapper.ToDto(profile);
    }

    public async Task DeleteAsync(long id)
    {
        var profile = await db.ResearchProfiles.FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new AppException(ResearchProfileErrorCode.ProfileNotFound);
        profile.Deleted = true;
        await db.SaveChangesAsync();
    }

    public async Task<List<ResearchProfileResponseDto>> GetAllAsync(int page, int size)
    {
        return await db.ResearchProfiles.Where(p => !p.Deleted)
            .Skip(Math.Max(0, page) * Math.Max(1, size))
            .Take(Math.Max(1, size))
            .Select(p => ResearchProfileMapper.ToDto(p))
            .ToListAsync();
    }
}
