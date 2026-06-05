using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Options;

namespace AI_Readiness_Hub.Services;

public sealed class DatabaseDataProtectionKeyManagementOptionsSetup(
    DatabaseDataProtectionXmlRepository repository,
    ILogger<DatabaseDataProtectionKeyManagementOptionsSetup> logger) : IConfigureOptions<KeyManagementOptions>
{
    public void Configure(KeyManagementOptions options)
    {
        options.XmlRepository = repository;
        logger.LogInformation("ASP.NET Core Data Protection keys are configured to persist in PostgreSQL.");
    }
}
