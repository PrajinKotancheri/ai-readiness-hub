using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace AI_Readiness_Hub.Data;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? Environments.Development;
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: false)
            .AddUserSecrets(typeof(ApplicationDbContextFactory).Assembly, optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();

        var configuredProvider = configuration.GetValue<string>("DatabaseProvider");
        if (!string.IsNullOrWhiteSpace(configuredProvider) &&
            !configuredProvider.Equals("Postgres", StringComparison.OrdinalIgnoreCase) &&
            !configuredProvider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("AI Readiness Consultant Hub supports PostgreSQL only. Remove DatabaseProvider or set it to Postgres.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required for PostgreSQL.");
        }

        optionsBuilder.UseNpgsql(connectionString);
        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
