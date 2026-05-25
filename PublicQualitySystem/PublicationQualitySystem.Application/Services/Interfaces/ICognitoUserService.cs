namespace PublicationQualitySystem.Application.Services.Interfaces;

public interface ICognitoUserService
{
    Task<bool> ExistsByEmailAsync(string email);
    Task<string?> EnsureDefaultAdminUserAsync(string email, string password);
}
