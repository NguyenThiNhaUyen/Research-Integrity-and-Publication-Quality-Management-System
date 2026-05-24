using Microsoft.EntityFrameworkCore;
using PublicationQualitySystem.Configurations;
using PublicationQualitySystem.DTOs.Auth;
using PublicationQualitySystem.DTOs.File;
using PublicationQualitySystem.DTOs.ResearchGroup;
using PublicationQualitySystem.DTOs.ResearchProfile;
using PublicationQualitySystem.DTOs.Role;
using PublicationQualitySystem.DTOs.User;
using PublicationQualitySystem.Entities;
using PublicationQualitySystem.Exceptions;
using PublicationQualitySystem.Mappings;
using PublicationQualitySystem.Repositories.Interfaces;
using PublicationQualitySystem.Services.Interfaces;

namespace PublicationQualitySystem.Services.Implementations;

public class ResearchProfileService(ApplicationDbContext db, IUserRepository users) : IResearchProfileService
{
    public async Task<ResearchProfileDto> CreateAsync(ResearchProfileDto dto)
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
            AcademicRank = dto.AcademicRank ?? Enums.AcademicRank.RESEARCHER,
            Status = dto.Status ?? Enums.MemberStatus.ACTIVE
        };
        db.ResearchProfiles.Add(profile);
        await db.SaveChangesAsync();
        return ResearchProfileMapper.ToDto(profile);
    }

    public async Task<ResearchProfileDto> GetByIdAsync(long id)
    {
        var profile = await db.ResearchProfiles.FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new AppException(ResearchProfileErrorCode.ProfileNotFound);
        return ResearchProfileMapper.ToDto(profile);
    }

    public async Task<ResearchProfileDto> GetByUserIdAsync(string userId)
    {
        var profile = await db.ResearchProfiles.FirstOrDefaultAsync(p => p.UserId == userId)
            ?? throw new AppException(ResearchProfileErrorCode.ProfileNotFound);
        return ResearchProfileMapper.ToDto(profile);
    }

    public async Task<ResearchProfileDto> UpdateAsync(long id, ResearchProfileDto dto)
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

    public async Task<List<ResearchProfileDto>> GetAllAsync(int page, int size)
    {
        return await db.ResearchProfiles.Where(p => !p.Deleted)
            .Skip(Math.Max(0, page) * Math.Max(1, size))
            .Take(Math.Max(1, size))
            .Select(p => ResearchProfileMapper.ToDto(p))
            .ToListAsync();
    }
}
