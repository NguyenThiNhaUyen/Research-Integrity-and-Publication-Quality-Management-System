using Amazon;
using Amazon.CognitoIdentityProvider;
using Amazon.Runtime;
using Amazon.S3;
using Microsoft.Extensions.Options;
using PublicationQualitySystem.Shared.Constants;
using PublicationQualitySystem.Shared.Extensions;
using PublicationQualitySystem.Infrastructure.Options;

namespace PublicationQualitySystem.Infrastructure.Configurations;

public static class AwsConfiguration
{
    public static IServiceCollection AddAwsConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AwsOptions>(options =>
        {
            options.AccessKey = ConfigurationValueResolver.Resolve(configuration["Aws:AccessKey"]);
            options.SecretKey = ConfigurationValueResolver.Resolve(configuration["Aws:SecretKey"]);
            options.Region = ConfigurationValueResolver.Resolve(configuration["Aws:Region"]);
        });
        services.Configure<CognitoOptions>(options =>
        {
            options.Region = ConfigurationValueResolver.Resolve(configuration["Aws:Cognito:Region"]);
            options.UserPoolId = ConfigurationValueResolver.Resolve(configuration["Aws:Cognito:UserPoolId"]);
            options.ClientId = ConfigurationValueResolver.Resolve(configuration["Aws:Cognito:ClientId"]);
            options.ClientSecret = ConfigurationValueResolver.Resolve(configuration["Aws:Cognito:ClientSecret"]);
            options.AdminGroupName = ConfigurationValueResolver.Resolve(configuration["Aws:Cognito:AdminGroupName"]) ?? "ADMIN";
        });
        services.Configure<S3Options>(options =>
        {
            options.Bucket = ConfigurationValueResolver.Resolve(configuration["Aws:S3:Bucket"])
                ?? Environment.GetEnvironmentVariable("AWS_S3_BUCKET");
        });
        services.Configure<ManuscriptUploadOptions>(configuration.GetSection("Upload:Manuscript"));

        services.AddSingleton<IAmazonCognitoIdentityProvider>(provider =>
        {
            var awsOptions = provider.GetRequiredService<IOptions<AwsOptions>>().Value;
            var cognitoOptions = provider.GetRequiredService<IOptions<CognitoOptions>>().Value;
            var region = RegionEndpoint.GetBySystemName(cognitoOptions.Region ?? AwsConstants.DefaultRegion);
            var credentials = CreateAwsCredentials(awsOptions);
            return credentials is null
                ? new AmazonCognitoIdentityProviderClient(region)
                : new AmazonCognitoIdentityProviderClient(credentials, region);
        });

        services.AddSingleton<IAmazonS3>(provider =>
        {
            var awsOptions = provider.GetRequiredService<IOptions<AwsOptions>>().Value;
            var region = RegionEndpoint.GetBySystemName(awsOptions.Region ?? AwsConstants.DefaultRegion);
            var credentials = CreateAwsCredentials(awsOptions);
            return credentials is null ? new AmazonS3Client(region) : new AmazonS3Client(credentials, region);
        });

        return services;
    }

    private static AWSCredentials? CreateAwsCredentials(AwsOptions options) =>
        string.IsNullOrWhiteSpace(options.AccessKey) || string.IsNullOrWhiteSpace(options.SecretKey)
            ? null
            : new BasicAWSCredentials(options.AccessKey, options.SecretKey);
}
