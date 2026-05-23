namespace PublicationQualitySystem.Extensions;

public static class ConfigurationValueResolver
{
    public static string? Resolve(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;
        if (!value.StartsWith("${", StringComparison.Ordinal) || !value.EndsWith('}')) return value;

        var key = value[2..^1].Split(':', 2)[0];
        return Environment.GetEnvironmentVariable(key);
    }
}
