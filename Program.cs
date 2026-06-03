using AI_Readiness_Hub.Data;
using AI_Readiness_Hub.Services;
using Microsoft.EntityFrameworkCore;

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
builder.Services.AddCors(options =>
{
    options.AddPolicy("GoogleFormsWebhook", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.AddControllersWithViews()
    .AddApplicationPart(typeof(AI_Readiness_Hub.Controllers.DashboardController).Assembly);
ValidateDatabaseConfiguration(builder.Configuration, builder.Environment);
var postgresConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(postgresConnectionString);
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
app.UseCors();

app.UseAuthorization();

app.MapGet("/", context =>
{
    context.Response.Redirect("/Dashboard");
    return Task.CompletedTask;
});

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
    await InitializeDatabaseAsync(app);
    return;
}

if (filteredArgs.All(arg => !arg.Equals("--ef-design-time", StringComparison.OrdinalIgnoreCase)))
{
    await InitializeDatabaseAsync(app);
}

await app.RunAsync();

static void ValidateDatabaseConfiguration(IConfiguration configuration, IWebHostEnvironment environment)
{
    var configuredProvider = configuration.GetValue<string>("DatabaseProvider");
    if (!string.IsNullOrWhiteSpace(configuredProvider) &&
        !configuredProvider.Equals("Postgres", StringComparison.OrdinalIgnoreCase) &&
        !configuredProvider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException("AI Readiness Consultant Hub supports PostgreSQL only. Remove DatabaseProvider or set it to Postgres.");
    }

    var connectionString = configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required for PostgreSQL. Configure it with user secrets, an environment variable, or local-only appsettings.");
    }

    if (environment.IsProduction() && IsLocalPostgresConnection(connectionString))
    {
        throw new InvalidOperationException("Production PostgreSQL configuration points to localhost. Configure ConnectionStrings__DefaultConnection for the production database.");
    }
}

static async Task InitializeDatabaseAsync(WebApplication app)
{
    var logger = app.Services
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("DatabaseStartup");
    var connectionStringExists = !string.IsNullOrWhiteSpace(app.Configuration.GetConnectionString("DefaultConnection"));
    var runMigrationsOnStartup = app.Configuration.GetValue("RunMigrationsOnStartup", true);

    logger.LogInformation("Database provider: PostgreSQL.");
    logger.LogInformation("ConnectionStrings:DefaultConnection configured: {ConnectionStringConfigured}.", connectionStringExists);
    logger.LogInformation("RunMigrationsOnStartup: {RunMigrationsOnStartup}.", runMigrationsOnStartup);

    if (!runMigrationsOnStartup)
    {
        logger.LogWarning("RunMigrationsOnStartup is false. Skipping database migrations and seed data.");
        return;
    }

    try
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        logger.LogInformation("Applying database migrations...");
        await dbContext.Database.MigrateAsync();
        logger.LogInformation("Database migrations completed.");

        logger.LogInformation("Applying seed data...");
        await SeedData.InitializeAsync(scope.ServiceProvider);
        logger.LogInformation("Seed data completed.");
    }
    catch (Exception ex)
    {
        logger.LogError(
            ex,
            "Database startup failed. Message: {Message}. Inner exception: {InnerException}",
            ex.Message,
            ex.InnerException?.ToString() ?? "(none)");
        throw;
    }
}

static bool IsLocalPostgresConnection(string connectionString)
{
    var normalized = connectionString.Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase);
    return normalized.Contains("Host=localhost", StringComparison.OrdinalIgnoreCase) ||
           normalized.Contains("Host=127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
           normalized.Contains("Server=localhost", StringComparison.OrdinalIgnoreCase) ||
           normalized.Contains("Server=127.0.0.1", StringComparison.OrdinalIgnoreCase);
}

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
