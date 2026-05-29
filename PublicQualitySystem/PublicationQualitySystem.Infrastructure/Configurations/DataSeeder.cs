using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PublicationQualitySystem.Domain.Entities;
using PublicationQualitySystem.Domain.Enums;
using PublicationQualitySystem.Infrastructure.Options;
using PublicationQualitySystem.Application.Services.Interfaces;

namespace PublicationQualitySystem.Infrastructure.Configurations;

public class DataSeeder(ApplicationDbContext db, ICognitoGroupService cognitoGroups, ICognitoUserService cognitoUsers, IOptions<AdminOptions> adminOptions, ILogger<DataSeeder> logger)
{
    public async Task SeedAsync()
    {
        await SeedPermissionsAsync();
        await SeedRolesAsync();
        await SeedAdminAsync();
    }

    private async Task SeedPermissionsAsync()
    {
        foreach (var name in Enum.GetNames<PermissionName>())
        {
            if (!await db.Permissions.AnyAsync(p => p.Name == name))
            {
                db.Permissions.Add(new Permission
                {
                    Name = name,
                    Description = $"Permission for {name}",
                    CreatedBy = "SYSTEM",
                    UpdatedBy = "SYSTEM"
                });
            }
        }
        await db.SaveChangesAsync();
    }

    private async Task SeedRolesAsync()
    {
        foreach (var roleName in Enum.GetValues<RoleName>())
        {
            var name = roleName.ToString();
            var role = await db.Roles.Include(r => r.Permissions).FirstOrDefaultAsync(r => r.Name == name);
            if (role is null)
            {
                role = new Role { Name = name, Description = $"Role for {name}", CreatedBy = "SYSTEM", UpdatedBy = "SYSTEM" };
                db.Roles.Add(role);
            }

            var allowed = GetPermissionsForRole(roleName).Select(p => p.ToString()).ToHashSet();
            role.Permissions = await db.Permissions.Where(p => allowed.Contains(p.Name)).ToListAsync();
            await cognitoGroups.EnsureGroupExistsAsync(name);
        }
        await db.SaveChangesAsync();
    }

    private async Task SeedAdminAsync()
    {
        var fullName = Normalize(adminOptions.Value.FullName);
        var email = NormalizeEmail(adminOptions.Value.Email);
        var password = Normalize(adminOptions.Value.Password);
        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("Default admin account was not seeded because admin full name, email, or password is missing");
            return;
        }
        if (password.Length < 8)
        {
            logger.LogWarning("Default admin account was not seeded because admin password is too weak");
            return;
        }

        var adminRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == RoleName.ADMIN.ToString());
        if (adminRole is null)
        {
            logger.LogError("Default admin account was not seeded because ADMIN role does not exist");
            return;
        }

        if (await db.Users.AnyAsync(u => u.Email == email))
        {
            return;
        }

        var cognitoSub = await cognitoUsers.EnsureDefaultAdminUserAsync(email, password);
        if (string.IsNullOrWhiteSpace(cognitoSub))
        {
            logger.LogWarning("Default admin Cognito sync failed");
            return;
        }

        db.Users.Add(new User
        {
            Id = cognitoSub,
            FullName = fullName,
            Email = email,
            Password = BCrypt.Net.BCrypt.HashPassword(password),
            Roles = new HashSet<Role> { adminRole },
            CreatedBy = "SYSTEM",
            UpdatedBy = "SYSTEM"
        });
        await db.SaveChangesAsync();
    }

    private static ISet<PermissionName> GetPermissionsForRole(RoleName roleName)
    {
        if (roleName == RoleName.ADMIN) return Enum.GetValues<PermissionName>().ToHashSet();

        var permissions = new HashSet<PermissionName>();
        if (roleName == RoleName.LAB_LEADER)
        {
            permissions.UnionWith(new[]
            {
                PermissionName.USER_READ, PermissionName.ROLE_ASSIGN, PermissionName.ROLE_READ,
                PermissionName.FILE_UPLOAD, PermissionName.FILE_DELETE,
                PermissionName.LAB_MEMBER_CREATE, PermissionName.LAB_MEMBER_READ, PermissionName.LAB_MEMBER_UPDATE, PermissionName.LAB_MEMBER_DELETE,
                PermissionName.RESEARCH_GROUP_CREATE, PermissionName.RESEARCH_GROUP_READ, PermissionName.RESEARCH_GROUP_UPDATE, PermissionName.RESEARCH_GROUP_DELETE,
                PermissionName.RESEARCH_GROUP_MEMBER_MANAGE,
                PermissionName.RESEARCH_PROFILE_CREATE, PermissionName.RESEARCH_PROFILE_READ, PermissionName.RESEARCH_PROFILE_UPDATE, PermissionName.RESEARCH_PROFILE_DELETE,
                PermissionName.PAPER_CREATE, PermissionName.PAPER_READ_OWN, PermissionName.PAPER_READ_ALL, PermissionName.PAPER_UPDATE_OWN, PermissionName.PAPER_UPDATE_ALL,
                PermissionName.PAPER_DELETE_OWN, PermissionName.PAPER_DELETE_ALL, PermissionName.PAPER_SUBMIT_INTERNAL_REVIEW,
                PermissionName.PAPER_VERSION_UPLOAD, PermissionName.PAPER_VERSION_READ, PermissionName.PAPER_VERSION_UPDATE,
                PermissionName.PAPER_VERSION_COMPARE, PermissionName.PAPER_VERSION_DELETE, PermissionName.PAPER_VERSION_RESTORE,
                PermissionName.QUALITY_CHECK_RUN, PermissionName.QUALITY_REPORT_READ,
                PermissionName.INTEGRITY_CHECK_RUN, PermissionName.INTEGRITY_REPORT_READ_OWN, PermissionName.INTEGRITY_REPORT_READ_ALL, PermissionName.INTEGRITY_REPORT_APPROVE,
                PermissionName.REVIEW_CREATE, PermissionName.REVIEW_READ_ASSIGNED, PermissionName.REVIEW_READ_ALL, PermissionName.REVIEW_ASSIGN, PermissionName.REVIEW_DECISION_SUBMIT,
                PermissionName.VENUE_RECOMMEND,
                PermissionName.SUBMISSION_CREATE, PermissionName.SUBMISSION_UPDATE, PermissionName.SUBMISSION_TRACK, PermissionName.SUBMISSION_APPROVE,
                PermissionName.REVIEWER_RESPONSE_CREATE, PermissionName.REVIEWER_RESPONSE_READ,
                PermissionName.NOTIFICATION_READ, PermissionName.NOTIFICATION_MANAGE,
                PermissionName.ANALYTICS_READ_OWN, PermissionName.ANALYTICS_READ_LAB,
                PermissionName.KNOWLEDGE_BASE_CREATE, PermissionName.KNOWLEDGE_BASE_READ, PermissionName.KNOWLEDGE_BASE_UPDATE
            });
        }
        else if (roleName == RoleName.SENIOR_RESEARCHER)
        {
            permissions.UnionWith(new[]
            {
                PermissionName.FILE_UPLOAD, PermissionName.LAB_MEMBER_READ,
                PermissionName.RESEARCH_GROUP_READ, PermissionName.RESEARCH_PROFILE_READ,
                PermissionName.PAPER_CREATE, PermissionName.PAPER_READ_OWN, PermissionName.PAPER_UPDATE_OWN, PermissionName.PAPER_DELETE_OWN,
                PermissionName.PAPER_SUBMIT_INTERNAL_REVIEW, PermissionName.PAPER_VERSION_UPLOAD, PermissionName.PAPER_VERSION_READ,
                PermissionName.PAPER_VERSION_UPDATE, PermissionName.PAPER_VERSION_COMPARE, PermissionName.PAPER_VERSION_DELETE,
                PermissionName.PAPER_VERSION_RESTORE, PermissionName.QUALITY_CHECK_RUN, PermissionName.QUALITY_REPORT_READ,
                PermissionName.INTEGRITY_CHECK_RUN, PermissionName.INTEGRITY_REPORT_READ_OWN, PermissionName.INTEGRITY_REPORT_APPROVE,
                PermissionName.REVIEW_CREATE, PermissionName.REVIEW_READ_ASSIGNED, PermissionName.REVIEW_DECISION_SUBMIT,
                PermissionName.VENUE_RECOMMEND, PermissionName.SUBMISSION_CREATE, PermissionName.SUBMISSION_UPDATE, PermissionName.SUBMISSION_TRACK, PermissionName.SUBMISSION_APPROVE,
                PermissionName.REVIEWER_RESPONSE_CREATE, PermissionName.REVIEWER_RESPONSE_READ,
                PermissionName.NOTIFICATION_READ, PermissionName.ANALYTICS_READ_OWN,
                PermissionName.KNOWLEDGE_BASE_CREATE, PermissionName.KNOWLEDGE_BASE_READ, PermissionName.KNOWLEDGE_BASE_UPDATE
            });
        }
        else if (roleName == RoleName.RESEARCHER)
        {
            permissions.UnionWith(new[]
            {
                PermissionName.FILE_UPLOAD, PermissionName.LAB_MEMBER_READ,
                PermissionName.RESEARCH_GROUP_READ, PermissionName.RESEARCH_PROFILE_READ,
                PermissionName.PAPER_CREATE, PermissionName.PAPER_READ_OWN, PermissionName.PAPER_UPDATE_OWN, PermissionName.PAPER_DELETE_OWN,
                PermissionName.PAPER_SUBMIT_INTERNAL_REVIEW, PermissionName.PAPER_VERSION_UPLOAD, PermissionName.PAPER_VERSION_READ,
                PermissionName.PAPER_VERSION_UPDATE, PermissionName.PAPER_VERSION_COMPARE, PermissionName.PAPER_VERSION_DELETE,
                PermissionName.PAPER_VERSION_RESTORE, PermissionName.QUALITY_CHECK_RUN, PermissionName.QUALITY_REPORT_READ,
                PermissionName.INTEGRITY_CHECK_RUN, PermissionName.INTEGRITY_REPORT_READ_OWN,
                PermissionName.VENUE_RECOMMEND, PermissionName.SUBMISSION_CREATE, PermissionName.SUBMISSION_UPDATE, PermissionName.SUBMISSION_TRACK,
                PermissionName.REVIEWER_RESPONSE_CREATE, PermissionName.REVIEWER_RESPONSE_READ,
                PermissionName.NOTIFICATION_READ, PermissionName.ANALYTICS_READ_OWN, PermissionName.KNOWLEDGE_BASE_READ
            });
        }
        else if (roleName == RoleName.AI_QUALITY_ASSISTANT)
        {
            permissions.UnionWith(new[]
            {
                PermissionName.PAPER_READ_ALL, PermissionName.PAPER_VERSION_READ, PermissionName.PAPER_VERSION_COMPARE,
                PermissionName.QUALITY_CHECK_RUN, PermissionName.QUALITY_REPORT_READ,
                PermissionName.INTEGRITY_CHECK_RUN, PermissionName.INTEGRITY_REPORT_READ_ALL,
                PermissionName.VENUE_RECOMMEND, PermissionName.SUBMISSION_TRACK,
                PermissionName.NOTIFICATION_READ, PermissionName.KNOWLEDGE_BASE_READ
            });
        }
        return permissions;
    }

    private static string? Normalize(string? value) => value?.Trim();
    private static string? NormalizeEmail(string? value) => value?.Trim().ToLowerInvariant();
}
