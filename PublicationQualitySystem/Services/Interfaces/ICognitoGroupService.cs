namespace PublicationQualitySystem.Services.Interfaces;

public interface ICognitoGroupService
{
    Task EnsureGroupExistsAsync(string groupName);
    Task CreateGroupAsync(string groupName, string? description);
    Task UpdateGroupAsync(string oldGroupName, string newGroupName, string? description);
    Task DeleteGroupAsync(string groupName);
    Task AddUserToGroupAsync(string username, string groupName);
    Task RemoveUserFromGroupAsync(string username, string groupName);
    Task<ISet<string>> GetUserGroupsAsync(string username);
}
