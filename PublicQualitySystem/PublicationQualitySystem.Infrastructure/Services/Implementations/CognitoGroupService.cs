using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Microsoft.Extensions.Options;
using PublicationQualitySystem.Shared.Exceptions;
using PublicationQualitySystem.Infrastructure.Options;
using PublicationQualitySystem.Application.Services.Interfaces;

namespace PublicationQualitySystem.Infrastructure.Services.Implementations;

public class CognitoGroupService(IAmazonCognitoIdentityProvider cognito, IOptions<CognitoOptions> options, ILogger<CognitoGroupService> logger) : ICognitoGroupService
{
    private string UserPoolId => options.Value.UserPoolId ?? string.Empty;

    public async Task EnsureGroupExistsAsync(string groupName)
    {
        if (!await GroupExistsAsync(groupName)) await CreateGroupAsync(groupName, $"Auto-created group for role {groupName}");
    }

    public async Task CreateGroupAsync(string groupName, string? description)
    {
        if (IsBlank(groupName) || IsBlank(UserPoolId) || await GroupExistsAsync(groupName)) return;
        try
        {
            await cognito.CreateGroupAsync(new CreateGroupRequest { UserPoolId = UserPoolId, GroupName = groupName, Description = description });
        }
        catch (AmazonCognitoIdentityProviderException exception)
        {
            throw ToSyncException(exception);
        }
    }

    public async Task UpdateGroupAsync(string oldGroupName, string newGroupName, string? description)
    {
        if (oldGroupName == newGroupName) await EnsureGroupExistsAsync(newGroupName);
        else
        {
            await CreateGroupAsync(newGroupName, description);
            await DeleteGroupAsync(oldGroupName);
        }
    }

    public async Task DeleteGroupAsync(string groupName)
    {
        if (IsBlank(groupName) || IsBlank(UserPoolId)) return;
        try
        {
            await cognito.DeleteGroupAsync(new DeleteGroupRequest { UserPoolId = UserPoolId, GroupName = groupName });
        }
        catch (ResourceNotFoundException)
        {
            logger.LogWarning("Cognito group does not exist: {GroupName}", groupName);
        }
        catch (AmazonCognitoIdentityProviderException exception)
        {
            throw ToSyncException(exception);
        }
    }

    public async Task AddUserToGroupAsync(string username, string groupName)
    {
        if (IsBlank(username) || IsBlank(groupName) || IsBlank(UserPoolId)) return;
        await cognito.AdminAddUserToGroupAsync(new AdminAddUserToGroupRequest { UserPoolId = UserPoolId, Username = username, GroupName = groupName });
    }

    public async Task RemoveUserFromGroupAsync(string username, string groupName)
    {
        if (IsBlank(username) || IsBlank(groupName) || IsBlank(UserPoolId)) return;
        try
        {
            await cognito.AdminRemoveUserFromGroupAsync(new AdminRemoveUserFromGroupRequest { UserPoolId = UserPoolId, Username = username, GroupName = groupName });
        }
        catch (Exception exception) when (exception is ResourceNotFoundException or UserNotFoundException)
        {
            logger.LogWarning("Cognito user or group not found while removing {Username} from {GroupName}", username, groupName);
        }
    }

    public async Task<ISet<string>> GetUserGroupsAsync(string username)
    {
        if (IsBlank(username) || IsBlank(UserPoolId)) return new HashSet<string>();
        var response = await cognito.AdminListGroupsForUserAsync(new AdminListGroupsForUserRequest { UserPoolId = UserPoolId, Username = username });
        return response.Groups.Select(g => g.GroupName).ToHashSet();
    }

    private async Task<bool> GroupExistsAsync(string groupName)
    {
        try
        {
            await cognito.GetGroupAsync(new GetGroupRequest { UserPoolId = UserPoolId, GroupName = groupName });
            return true;
        }
        catch (ResourceNotFoundException)
        {
            return false;
        }
    }

    private AppException ToSyncException(AmazonCognitoIdentityProviderException exception)
    {
        logger.LogError(exception, "Cognito group sync failed");
        return new AppException(RoleErrorCode.CognitoGroupSyncFailed, $"Cognito group sync failed [{exception.ErrorCode ?? "UNKNOWN"}]: {exception.Message}");
    }

    private static bool IsBlank(string? value) => string.IsNullOrWhiteSpace(value);
}
