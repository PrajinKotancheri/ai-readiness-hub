using AI_Readiness_Hub.Data;
using AI_Readiness_Hub.Services;
using Microsoft.EntityFrameworkCore;

var seedOnly = args.Any(arg => arg.Equals("--seed-only", StringComparison.OrdinalIgnoreCase));
var filteredArgs = args
    .Where(arg => !arg.Equals("--seed-only", StringComparison.OrdinalIgnoreCase))
    .ToArray();
var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions
{
    ApplicationName = typeof(AI_Readiness_Hub.Controllers.DashboardController).Assembly.GetName().Name,
    Args = filteredArgs,
    ContentRootPath = Directory.GetCurrentDirectory(),
    WebRootPath = "wwwroot"
});
builder.WebHost.UseKestrel();
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: false)
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
        options.UseNpgsql(builder.Configuration.GetConnectionString("PostgresConnection"));
        return;
    }

    options.UseSqlite(builder.Configuration.GetConnectionString("SqliteConnection") ?? "Data Source=ai-readiness-hub.db");
});
builder.Services.AddScoped<IAIConsultingAnalysisService, MockAIConsultingAnalysisService>();
builder.Services.AddScoped<IClientDocumentSummaryService, MockClientDocumentSummaryService>();

var app = builder.Build();

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
