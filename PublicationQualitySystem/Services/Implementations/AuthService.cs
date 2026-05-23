using System.Security.Cryptography;
using System.Text;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Microsoft.Extensions.Options;
using PublicationQualitySystem.Configurations;
using PublicationQualitySystem.DTOs.Auth;
using PublicationQualitySystem.DTOs.File;
using PublicationQualitySystem.DTOs.ResearchGroup;
using PublicationQualitySystem.DTOs.ResearchProfile;
using PublicationQualitySystem.DTOs.Role;
using PublicationQualitySystem.DTOs.User;
using PublicationQualitySystem.Entities;
using PublicationQualitySystem.Exceptions;
using PublicationQualitySystem.Options;
using PublicationQualitySystem.Repositories.Interfaces;
using PublicationQualitySystem.Services.Interfaces;

namespace PublicationQualitySystem.Services.Implementations;

public class AuthService(
    IAmazonCognitoIdentityProvider cognito,
    IOptions<CognitoOptions> cognitoOptions,
    ApplicationDbContext db,
    IUserRepository users) : IAuthService
{
    private string ClientId => cognitoOptions.Value.ClientId ?? string.Empty;
    private string ClientSecret => cognitoOptions.Value.ClientSecret ?? string.Empty;
    private string UserPoolId => cognitoOptions.Value.UserPoolId ?? string.Empty;

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request)
    {
        var email = NormalizeEmail(request.Email);
        var fullName = request.FullName.Trim();
        if (await users.ExistsByEmailAsync(email)) 
            throw new AppException(AuthErrorCode.UserAlreadyExists);

        try
        {
            var signUp = new SignUpRequest
            {
                ClientId = ClientId,
                Username = email,
                Password = request.Password,
                UserAttributes =
                [
                    new AttributeType { Name = "email", Value = email },
                    new AttributeType { Name = "name", Value = fullName }
                ]
            };
            if (HasClientSecret()) signUp.SecretHash = CalculateSecretHash(email);
            var response = await cognito.SignUpAsync(signUp);
            await ConfirmAndVerifyEmailIfNeeded(email, response);
            
            if(string.IsNullOrWhiteSpace(response.UserSub)) 
                throw new AppException(AuthErrorCode.CognitoSubMissing);

            db.Users.Add(new User
            {
                Id = response.UserSub,
                Email = email,
                FullName = fullName,
                Password = BCrypt.Net.BCrypt.HashPassword(request.Password)
            });
            await db.SaveChangesAsync();
            return await LoginAsync(new LoginRequestDto { Email = email, Password = request.Password });
        }
        catch (UsernameExistsException)
        {
            throw new AppException(AuthErrorCode.UserAlreadyExists);
        }
        catch (UserNotConfirmedException)
        {
            throw new AppException(AuthErrorCode.UserNotConfirmed);
        }
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
    {
        var email = NormalizeEmail(request.Email);
        try
        {
            var authParameters = new Dictionary<string, string>
            {
                ["USERNAME"] = email,
                ["PASSWORD"] = request.Password
            };
            PutSecretHash(authParameters, email);
            var response = await cognito.InitiateAuthAsync(new InitiateAuthRequest
            {
                ClientId = ClientId,
                AuthFlow = AuthFlowType.USER_PASSWORD_AUTH,
                AuthParameters = authParameters
            });
            var result = response.AuthenticationResult;
            var dto = new AuthResponseDto
            {
                AccessToken = result.AccessToken,
                RefreshToken = result.RefreshToken,
                IdToken = result.IdToken,
                TokenType = result.TokenType,
                ExpiresIn = result.ExpiresIn,
                Email = email
            };
            var user = await users.FindByEmailAsync(email);
            if (user is not null)
            {
                dto.Email = user.Email;
                dto.FullName = user.FullName;
            }
            return dto;
        }
        catch (UserNotConfirmedException)
        {
            throw new AppException(AuthErrorCode.UserNotConfirmed);
        }
        catch (NotAuthorizedException)
        {
            throw new AppException(AuthErrorCode.InvalidCredentials);
        }
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request)
    {
        try
        {
            var response = await cognito.InitiateAuthAsync(new InitiateAuthRequest
            {
                ClientId = ClientId,
                AuthFlow = AuthFlowType.REFRESH_TOKEN_AUTH,
                AuthParameters = new Dictionary<string, string> { ["REFRESH_TOKEN"] = request.RefreshToken }
            });
            var result = response.AuthenticationResult;
            return new AuthResponseDto
            {
                AccessToken = result.AccessToken,
                RefreshToken = result.RefreshToken ?? request.RefreshToken,
                IdToken = result.IdToken,
                TokenType = result.TokenType,
                ExpiresIn = result.ExpiresIn
            };
        }
        catch (NotAuthorizedException)
        {
            throw new AppException(AuthErrorCode.TokenRefreshFailed);
        }
    }

    public Task LogoutAsync(LogoutRequestDto request) =>
        cognito.GlobalSignOutAsync(new GlobalSignOutRequest { AccessToken = request.AccessToken });

    private async Task ConfirmAndVerifyEmailIfNeeded(string email, SignUpResponse response)
    {
        if (response.UserConfirmed != true)
        {
            await cognito.AdminConfirmSignUpAsync(new AdminConfirmSignUpRequest { UserPoolId = UserPoolId, Username = email });
        }
        await cognito.AdminUpdateUserAttributesAsync(new AdminUpdateUserAttributesRequest
        {
            UserPoolId = UserPoolId,
            Username = email,
            UserAttributes = [new AttributeType { Name = "email_verified", Value = "true" }]
        });
    }

    private void PutSecretHash(IDictionary<string, string> authParameters, string username)
    {
        if (HasClientSecret()) authParameters["SECRET_HASH"] = CalculateSecretHash(username);
    }

    private string CalculateSecretHash(string username)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(ClientSecret));
        return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(username + ClientId)));
    }

    private bool HasClientSecret() => !string.IsNullOrWhiteSpace(ClientSecret);
    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
