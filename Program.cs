using AI_Readiness_Hub.Data;
using AI_Readiness_Hub.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

var seedOnly = args.Any(arg => arg.Equals("--seed-only", StringComparison.OrdinalIgnoreCase));
var filteredArgs = args
    .Where(arg => !arg.Equals("--seed-only", StringComparison.OrdinalIgnoreCase))
    .ToArray();
var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
    ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
    ?? Environments.Production;
var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions
{
    ApplicationName = typeof(AI_Readiness_Hub.Controllers.DashboardController).Assembly.GetName().Name,
    Args = filteredArgs,
    ContentRootPath = Directory.GetCurrentDirectory(),
    EnvironmentName = environmentName,
    WebRootPath = "wwwroot"
});
builder.WebHost.UseKestrel();
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: false);
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets(typeof(AI_Readiness_Hub.Controllers.DashboardController).Assembly, optional: true, reloadOnChange: false);
}

builder.Configuration
    .AddEnvironmentVariables()
    .AddCommandLine(filteredArgs);
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
builder.Logging.AddConsole();

builder.Services.AddRouting();
builder.Services.AddControllersWithViews()
    .AddApplicationPart(typeof(AI_Readiness_Hub.Controllers.DashboardController).Assembly);
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var provider = builder.Configuration.GetValue<string>("DatabaseProvider") ?? "Sqlite";
    if (provider.Equals("Postgres", StringComparison.OrdinalIgnoreCase) ||
        provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
    {
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
            ?? builder.Configuration.GetConnectionString("PostgresConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:DefaultConnection or ConnectionStrings:PostgresConnection is required when DatabaseProvider=Postgres.");
        }

        options.UseNpgsql(connectionString);
        return;
    }

    options.UseSqlite(builder.Configuration.GetConnectionString("SqliteConnection") ?? "Data Source=ai-readiness-hub.db");
    options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
});
builder.Services.AddScoped<IAIConsultingAnalysisService, MockAIConsultingAnalysisService>();
builder.Services.AddScoped<IClientDocumentSummaryService, MockClientDocumentSummaryService>();
builder.Services.AddScoped<IEmailService, SendGridEmailService>();
builder.Services.AddScoped<IReadinessFormService, ReadinessFormService>();

var app = builder.Build();

ValidateEmailConfiguration(app);

var configuredUrls = app.Configuration["urls"] ?? app.Configuration["ASPNETCORE_URLS"];
if (!string.IsNullOrWhiteSpace(configuredUrls))
{
    foreach (var url in configuredUrls.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    {
        app.Urls.Add(url);
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();

app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapStaticAssets();
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Dashboard}/{action=Index}/{id?}")
        .WithStaticAssets();
    endpoints.MapControllers();
});

if (seedOnly)
{
    using var seedScope = app.Services.CreateScope();
    Console.WriteLine("Applying migrations and seed data...");
    await SeedData.InitializeAsync(seedScope.ServiceProvider);
    Console.WriteLine("Seed data is ready.");
    return;
}

if (filteredArgs.All(arg => !arg.Equals("--ef-design-time", StringComparison.OrdinalIgnoreCase)))
{
    using var scope = app.Services.CreateScope();
    Console.WriteLine("Applying migrations and seed data...");
    await SeedData.InitializeAsync(scope.ServiceProvider);
    Console.WriteLine("Seed data is ready.");
}

await app.RunAsync();

static void ValidateEmailConfiguration(WebApplication app)
{
    var logger = app.Services
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("SendGridConfiguration");

    var emailConfiguration = EmailConfiguration.From(app.Configuration);

    if (app.Environment.IsDevelopment())
    {
        logger.LogInformation(
            "Email configuration sources. SMTP_PASSWORD env: {ApiKeyEnv}; user-secrets: {ApiKeySecrets}; appsettings: {ApiKeyAppSettings}. Smtp:FromEmail env: {FromEmailEnv}; user-secrets: {FromEmailSecrets}; appsettings: {FromEmailAppSettings}. Smtp:FromName env: {FromNameEnv}; user-secrets: {FromNameSecrets}; appsettings: {FromNameAppSettings}.",
            emailConfiguration.ApiKeySources.EnvironmentVariables,
            emailConfiguration.ApiKeySources.UserSecrets,
            emailConfiguration.ApiKeySources.AppSettings,
            emailConfiguration.FromEmailSources.EnvironmentVariables,
            emailConfiguration.FromEmailSources.UserSecrets,
            emailConfiguration.FromEmailSources.AppSettings,
            emailConfiguration.FromNameSources.EnvironmentVariables,
            emailConfiguration.FromNameSources.UserSecrets,
            emailConfiguration.FromNameSources.AppSettings);
    }

    if (!emailConfiguration.IsComplete)
    {
        logger.LogWarning(
            "SendGrid email configuration missing. Email sending disabled. Missing keys: {MissingKeys}",
            string.Join(", ", emailConfiguration.MissingRequiredKeys));
        return;
    }

    logger.LogInformation("SendGrid email configuration loaded. Email sending enabled.");
}
