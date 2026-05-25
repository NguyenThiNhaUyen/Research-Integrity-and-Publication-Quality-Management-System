using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Microsoft.Extensions.Options;
using PublicationQualitySystem.Infrastructure.Options;
using PublicationQualitySystem.Application.Services.Interfaces;

namespace PublicationQualitySystem.Infrastructure.Services.Implementations;

public class CognitoUserService(IAmazonCognitoIdentityProvider cognito, IOptions<CognitoOptions> options, ILogger<CognitoUserService> logger) : ICognitoUserService
{
    private string UserPoolId => options.Value.UserPoolId ?? string.Empty;
    private string AdminGroupName => options.Value.AdminGroupName;

    public async Task<bool> ExistsByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(UserPoolId)) return false;
        try
        {
            await cognito.AdminGetUserAsync(new AdminGetUserRequest { UserPoolId = UserPoolId, Username = email.Trim().ToLowerInvariant() });
            return true;
        }
        catch (UserNotFoundException)
        {
            return false;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Default admin Cognito user existence check failed");
            return false;
        }
    }

    public async Task<string?> EnsureDefaultAdminUserAsync(string email, string password)
    {
        email = email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(UserPoolId)) return null;
        try
        {
            if (!await ExistsByEmailAsync(email))
            {
                await cognito.AdminCreateUserAsync(new AdminCreateUserRequest
                {
                    UserPoolId = UserPoolId,
                    Username = email,
                    MessageAction = MessageActionType.SUPPRESS,
                    UserAttributes =
                    [
                        new AttributeType { Name = "email", Value = email },
                        new AttributeType { Name = "email_verified", Value = "true" }
                    ]
                });
            }
            await cognito.AdminSetUserPasswordAsync(new AdminSetUserPasswordRequest { UserPoolId = UserPoolId, Username = email, Password = password, Permanent = true });
            if (!string.IsNullOrWhiteSpace(AdminGroupName))
            {
                try { await cognito.AdminAddUserToGroupAsync(new AdminAddUserToGroupRequest { UserPoolId = UserPoolId, Username = email, GroupName = AdminGroupName }); }
                catch (ResourceNotFoundException) { logger.LogWarning("Default admin group does not exist: {AdminGroupName}", AdminGroupName); }
            }
            return await GetUserSubAsync(email);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Default admin Cognito sync failed");
            return null;
        }
    }

    private async Task<string?> GetUserSubAsync(string email)
    {
        var user = await cognito.AdminGetUserAsync(new AdminGetUserRequest { UserPoolId = UserPoolId, Username = email });
        return user.UserAttributes.FirstOrDefault(attribute => attribute.Name == "sub")?.Value;
    }
}
