using Microsoft.Extensions.Configuration;

namespace AI_Readiness_Hub.Services;

public sealed record EmailConfiguration(
    string? ApiKey,
    string? FromEmail,
    string? FromName,
    EmailKeySources ApiKeySources,
    EmailKeySources FromEmailSources,
    EmailKeySources FromNameSources)
{
    public IReadOnlyList<string> MissingRequiredKeys
    {
        get
        {
            var missing = new List<string>();
            if (string.IsNullOrWhiteSpace(ApiKey))
            {
                missing.Add("SMTP_PASSWORD");
            }

            if (string.IsNullOrWhiteSpace(FromEmail))
            {
                missing.Add("Smtp:FromEmail (environment variable: Smtp__FromEmail)");
            }

            return missing;
        }
    }

    public bool IsComplete => MissingRequiredKeys.Count == 0;

    public string CheckedSourcesSummary =>
        "Checked environment variables, user secrets, and appsettings. Expected keys: SMTP_PASSWORD; Smtp:FromEmail (environment variable: Smtp__FromEmail); optional Smtp:FromName (environment variable: Smtp__FromName).";

    public static EmailConfiguration From(IConfiguration configuration)
    {
        return new EmailConfiguration(
            configuration["SMTP_PASSWORD"]?.Trim(),
            configuration["Smtp:FromEmail"]?.Trim(),
            configuration["Smtp:FromName"]?.Trim(),
            GetSources(configuration, "SMTP_PASSWORD"),
            GetSources(configuration, "Smtp:FromEmail"),
            GetSources(configuration, "Smtp:FromName"));
    }

    private static EmailKeySources GetSources(IConfiguration configuration, string key)
    {
        if (configuration is not IConfigurationRoot root)
        {
            return new EmailKeySources(false, false, false);
        }

        var providers = root.Providers.ToList();
        return new EmailKeySources(
            providers.Any(provider => IsEnvironmentProvider(provider) && HasValue(provider, key)),
            providers.Any(provider => IsUserSecretsProvider(provider) && HasValue(provider, key)),
            providers.Any(provider => IsAppSettingsProvider(provider) && HasValue(provider, key)));
    }

    private static bool HasValue(IConfigurationProvider provider, string key)
    {
        return provider.TryGet(key, out var value) && !string.IsNullOrWhiteSpace(value);
    }

    private static bool IsEnvironmentProvider(IConfigurationProvider provider)
    {
        return provider.GetType().FullName?.Contains("EnvironmentVariables", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static bool IsUserSecretsProvider(IConfigurationProvider provider)
    {
        return provider.ToString()?.Contains("secrets.json", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static bool IsAppSettingsProvider(IConfigurationProvider provider)
    {
        return provider.ToString()?.Contains("appsettings", StringComparison.OrdinalIgnoreCase) == true;
    }
}

public sealed record EmailKeySources(bool EnvironmentVariables, bool UserSecrets, bool AppSettings);
