using Microsoft.EntityFrameworkCore;
using PublicationQualitySystem.Infrastructure.Configurations;
using PublicationQualitySystem.Application.DTOs.ResearchGroup.Requests;
using PublicationQualitySystem.Application.DTOs.ResearchGroup.Responses;
using PublicationQualitySystem.Domain.Entities;
using PublicationQualitySystem.Domain.Enums;
using PublicationQualitySystem.Shared.Exceptions;
using PublicationQualitySystem.Application.Mappings;
using PublicationQualitySystem.Application.Repositories.Interfaces;
using PublicationQualitySystem.Application.Services.Interfaces;

namespace PublicationQualitySystem.Infrastructure.Services.Implementations;

public class ResearchGroupService(
    ApplicationDbContext db,
    IUserRepository users,
    IResearchGroupMemberRepository members) : IResearchGroupService
{
    public async Task<ResearchGroupResponseDto> CreateAsync(CreateResearchGroupRequestDto dto)
    {
        if (await db.ResearchGroups.AnyAsync(g => g.Name == dto.Name)) throw new AppException(ResearchGroupErrorCode.ResearchGroupNameAlreadyExists);
        var group = new ResearchGroup();
        ResearchGroupMapper.UpdateEntity(group, dto);
        db.ResearchGroups.Add(group);
        await db.SaveChangesAsync();
        return await ToDtoAsync(group, true);
    }

    public async Task<ResearchGroupResponseDto> GetByIdAsync(long id)
    {
        var group = await db.ResearchGroups.FirstOrDefaultAsync(g => g.Id == id)
            ?? throw new AppException(ResearchGroupErrorCode.ResearchGroupNotFound);
        return await ToDtoAsync(group, true);
    }

    public async Task<ResearchGroupResponseDto> UpdateAsync(long id, UpdateResearchGroupRequestDto dto)
    {
        var group = await db.ResearchGroups.FirstOrDefaultAsync(g => g.Id == id)
            ?? throw new AppException(ResearchGroupErrorCode.ResearchGroupNotFound);
        if (!string.IsNullOrWhiteSpace(dto.Name) && dto.Name != group.Name && await db.ResearchGroups.AnyAsync(g => g.Name == dto.Name))
        {
            throw new AppException(ResearchGroupErrorCode.ResearchGroupNameAlreadyExists);
        }
        ResearchGroupMapper.UpdateEntity(group, dto);
        await db.SaveChangesAsync();
        return await ToDtoAsync(group, true);
    }

    public async Task DeleteAsync(long id)
    {
        var group = await db.ResearchGroups.FirstOrDefaultAsync(g => g.Id == id)
            ?? throw new AppException(ResearchGroupErrorCode.ResearchGroupNotFound);
        group.Deleted = true;
        await db.SaveChangesAsync();
    }

    public async Task<List<ResearchGroupResponseDto>> GetAllAsync(int page, int size)
    {
        var groups = await db.ResearchGroups.Where(g => !g.Deleted)
            .Skip(Math.Max(0, page) * Math.Max(1, size))
            .Take(Math.Max(1, size))
            .ToListAsync();
        var result = new List<ResearchGroupResponseDto>();
        foreach (var group in groups) result.Add(await ToDtoAsync(group, false));
        return result;
    }

    public async Task<ResearchGroupMemberResponseDto> AddMemberAsync(long groupId, AddResearchGroupMemberRequestDto dto)
    {
        var group = await db.ResearchGroups.FirstOrDefaultAsync(g => g.Id == groupId)
            ?? throw new AppException(ResearchGroupErrorCode.ResearchGroupNotFound);
        var userId = dto.UserId?.Trim();
        if (string.IsNullOrWhiteSpace(userId)) throw new AppException(ResearchGroupErrorCode.ValidationError);
        var user = await users.FindByIdAsync(userId) ?? throw new AppException(UserErrorCode.UserNotFound);
        var member = await members.FindByGroupAndUserAsync(groupId, userId);

        if (member is not null && member.Status == MemberStatus.ACTIVE) throw new AppException(ResearchGroupErrorCode.GroupMemberAlreadyExists);

        var role = dto.Role ?? MemberRoleInGroup.MEMBER;
        if (role == MemberRoleInGroup.LEADER) return await AssignGroupLeaderAsync(groupId, userId);

        if (member is null)
        {
            member = new ResearchGroupMember
            {
                ResearchGroup = group,
                User = user,
                ResearchGroupId = group.Id,
                UserId = user.Id,
                JoinedAt = DateOnly.FromDateTime(DateTime.UtcNow),
                Status = MemberStatus.ACTIVE,
                Role = role,
                Responsibilities = dto.Responsibilities
            };
            db.ResearchGroupMembers.Add(member);
        }
        else
        {
            member.Role = role;
            member.Status = MemberStatus.ACTIVE;
            member.LeftAt = null;
            member.Responsibilities = dto.Responsibilities;
        }

        await db.SaveChangesAsync();
        return ResearchGroupMapper.ToMemberDto(member);
    }

    public async Task RemoveMemberAsync(long groupId, string userId)
    {
        var member = await members.FindByGroupAndUserAsync(groupId, userId)
            ?? throw new AppException(ResearchGroupErrorCode.GroupMemberNotFound);
        if (member.Role == MemberRoleInGroup.LEADER) throw new AppException(ResearchGroupErrorCode.CannotRemoveOnlyLeader);
        member.Status = MemberStatus.INACTIVE;
        member.LeftAt = DateOnly.FromDateTime(DateTime.UtcNow);
        await db.SaveChangesAsync();
    }

    public async Task<ResearchGroupMemberResponseDto> ChangeMemberRoleAsync(long groupId, string userId, ChangeMemberRoleRequestDto dto)
    {
        if (dto.Role is null) throw new AppException(ResearchGroupErrorCode.ValidationError);
        if (dto.Role == MemberRoleInGroup.LEADER) return await AssignGroupLeaderAsync(groupId, userId);

        var member = await members.FindByGroupAndUserAsync(groupId, userId)
            ?? throw new AppException(ResearchGroupErrorCode.GroupMemberNotFound);
        if (member.Role == MemberRoleInGroup.LEADER) throw new AppException(ResearchGroupErrorCode.GroupLeaderRequired);

        member.Role = dto.Role.Value;
        await db.SaveChangesAsync();
        return ResearchGroupMapper.ToMemberDto(member);
    }

    public async Task<ResearchGroupMemberResponseDto> UpdateMemberStatusAsync(long groupId, string userId, UpdateMemberStatusRequestDto dto)
    {
        if (dto.Status is null) throw new AppException(ResearchGroupErrorCode.ValidationError);
        var member = await members.FindByGroupAndUserAsync(groupId, userId)
            ?? throw new AppException(ResearchGroupErrorCode.GroupMemberNotFound);
        if (member.Role == MemberRoleInGroup.LEADER && dto.Status != MemberStatus.ACTIVE)
        {
            throw new AppException(ResearchGroupErrorCode.CannotRemoveOnlyLeader);
        }

        member.Status = dto.Status.Value;
        member.LeftAt = dto.Status == MemberStatus.ACTIVE ? null : DateOnly.FromDateTime(DateTime.UtcNow);
        await db.SaveChangesAsync();
        return ResearchGroupMapper.ToMemberDto(member);
    }

    public async Task<ResearchGroupMemberResponseDto> AssignGroupLeaderAsync(long groupId, string userId)
    {
        var group = await db.ResearchGroups.FirstOrDefaultAsync(g => g.Id == groupId)
            ?? throw new AppException(ResearchGroupErrorCode.ResearchGroupNotFound);
        var user = await users.FindByIdAsync(userId) ?? throw new AppException(UserErrorCode.UserNotFound);

        var oldLeader = await members.FindLeaderAsync(groupId);
        if (oldLeader is not null && oldLeader.UserId != userId) oldLeader.Role = MemberRoleInGroup.MEMBER;

        var member = await members.FindByGroupAndUserAsync(groupId, userId);
        if (member is null)
        {
            member = new ResearchGroupMember
            {
                ResearchGroup = group,
                User = user,
                ResearchGroupId = group.Id,
                UserId = user.Id,
                JoinedAt = DateOnly.FromDateTime(DateTime.UtcNow)
            };
            db.ResearchGroupMembers.Add(member);
        }

        member.Role = MemberRoleInGroup.LEADER;
        member.Status = MemberStatus.ACTIVE;
        member.LeftAt = null;
        await db.SaveChangesAsync();
        return ResearchGroupMapper.ToMemberDto(member);
    }

    public async Task<List<ResearchGroupMemberResponseDto>> GetGroupMembersAsync(long groupId)
    {
        if (!await db.ResearchGroups.AnyAsync(g => g.Id == groupId)) throw new AppException(ResearchGroupErrorCode.ResearchGroupNotFound);
        return (await members.FindByGroupAsync(groupId)).Select(ResearchGroupMapper.ToMemberDto).ToList();
    }

    public async Task<List<ResearchGroupResponseDto>> GetGroupsByUserAsync(string userId)
    {
        if (!await users.ExistsByIdAsync(userId)) throw new AppException(UserErrorCode.UserNotFound);
        var memberships = await members.FindByUserAsync(userId);
        var result = new List<ResearchGroupResponseDto>();
        foreach (var group in memberships.Select(m => m.ResearchGroup).DistinctBy(g => g.Id))
        {
            result.Add(await ToDtoAsync(group, false));
        }
        return result;
    }

    private async Task<ResearchGroupResponseDto> ToDtoAsync(ResearchGroup group, bool includeMembers)
    {
        var memberDtos = (await members.FindByGroupAsync(group.Id)).Select(ResearchGroupMapper.ToMemberDto).ToList();
        var dto = ResearchGroupMapper.ToDto(group, includeMembers ? memberDtos : null);
        dto.MemberCount = memberDtos.Count;
        dto.Leader = memberDtos.FirstOrDefault(m => m.Role == MemberRoleInGroup.LEADER);
        return dto;
    }
}
