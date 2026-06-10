using AI_Readiness_Hub.Data;
using AI_Readiness_Hub.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Text;

namespace AI_Readiness_Hub.Controllers;

[Route("Settings")]
public class SettingsController(
    ApplicationDbContext context,
    ILogger<SettingsController> logger) : Controller
{
    [HttpGet("ReadinessForm")]
    public async Task<IActionResult> ReadinessForm()
    {
        var stopwatch = Stopwatch.StartNew();
        var settings = await GetActiveSettingsAsync(asNoTracking: true) ?? new ReadinessFormSettings();
        logger.LogInformation(
            "Readiness form settings loaded. SettingsConfigured: {SettingsConfigured}; ElapsedMs: {ElapsedMs}; RequestId: {RequestId}",
            settings.Id > 0,
            stopwatch.ElapsedMilliseconds,
            HttpContext.TraceIdentifier);
        return View(settings);
    }

    [HttpPost("ReadinessForm")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReadinessForm(ReadinessFormSettings posted)
    {
        if (!ModelState.IsValid)
        {
            return View(posted);
        }

        var settings = posted.Id > 0
            ? await context.ReadinessFormSettings.FindAsync(posted.Id)
            : await GetActiveSettingsAsync(asNoTracking: false);

        if (settings is null)
        {
            settings = new ReadinessFormSettings
            {
                CreatedAt = DateTime.UtcNow
            };
            context.ReadinessFormSettings.Add(settings);
        }

        settings.DefaultFormUrl = posted.DefaultFormUrl?.Trim();
        settings.ClientReferenceEntryId = posted.ClientReferenceEntryId?.Trim();
        settings.EmailSubjectTemplate = posted.EmailSubjectTemplate.Trim();
        settings.EmailBodyTemplate = posted.EmailBodyTemplate.Trim();
        settings.WebhookSecret = posted.WebhookSecret?.Trim();
        settings.IsActive = true;
        settings.LastModifiedAt = DateTime.UtcNow;

        var otherSettings = await context.ReadinessFormSettings
            .Where(item => item.Id != settings.Id && item.IsActive)
            .ToListAsync();
        foreach (var item in otherSettings)
        {
            item.IsActive = false;
            item.LastModifiedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Readiness form settings saved.";
        return RedirectToAction(nameof(ReadinessForm));
    }

    [HttpGet("PromptInventory")]
    public async Task<IActionResult> PromptInventory()
    {
        var prompts = await context.PromptDefinitions
            .AsNoTracking()
            .OrderBy(prompt => prompt.PlatformLocation)
            .ThenBy(prompt => prompt.PromptName)
            .ThenBy(prompt => prompt.VersionNumber)
            .ToListAsync();

        return View(prompts);
    }

    [HttpPost("PromptInventory/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePrompt(
        string promptName,
        string? goal,
        string? inputs,
        string? outputs,
        string? platformLocation,
        string promptText,
        string? notes,
        PromptStatus status,
        int versionNumber)
    {
        if (string.IsNullOrWhiteSpace(promptName))
        {
            TempData["ErrorMessage"] = "Prompt name is required.";
            return RedirectToAction(nameof(PromptInventory));
        }

        context.PromptDefinitions.Add(new PromptDefinition
        {
            PromptName = promptName.Trim(),
            Goal = goal?.Trim(),
            Inputs = inputs?.Trim(),
            Outputs = outputs?.Trim(),
            PlatformLocation = platformLocation?.Trim(),
            PromptText = string.IsNullOrWhiteSpace(promptText) ? "Stakeholder to provide actual prompt." : promptText.Trim(),
            Notes = notes?.Trim(),
            Status = status,
            VersionNumber = Math.Max(versionNumber, 1),
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Prompt definition added.";
        return RedirectToAction(nameof(PromptInventory));
    }

    [HttpPost("PromptInventory/Update")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePrompt(
        int id,
        string promptName,
        string? goal,
        string? inputs,
        string? outputs,
        string? platformLocation,
        string promptText,
        string? notes,
        PromptStatus status,
        int versionNumber)
    {
        var prompt = await context.PromptDefinitions.FindAsync(id);
        if (prompt is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(promptName))
        {
            TempData["ErrorMessage"] = "Prompt name is required.";
            return RedirectToAction(nameof(PromptInventory));
        }

        prompt.PromptName = promptName.Trim();
        prompt.Goal = goal?.Trim();
        prompt.Inputs = inputs?.Trim();
        prompt.Outputs = outputs?.Trim();
        prompt.PlatformLocation = platformLocation?.Trim();
        prompt.PromptText = string.IsNullOrWhiteSpace(promptText) ? "Stakeholder to provide actual prompt." : promptText.Trim();
        prompt.Notes = notes?.Trim();
        prompt.Status = status;
        prompt.VersionNumber = Math.Max(versionNumber, 1);
        prompt.LastModifiedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Prompt definition saved.";
        return RedirectToAction(nameof(PromptInventory));
    }

    [HttpGet("PromptInventory.csv")]
    public async Task<IActionResult> PromptInventoryCsv()
    {
        var prompts = await context.PromptDefinitions
            .AsNoTracking()
            .OrderBy(prompt => prompt.PlatformLocation)
            .ThenBy(prompt => prompt.PromptName)
            .ThenBy(prompt => prompt.VersionNumber)
            .ToListAsync();

        var builder = new StringBuilder();
        builder.AppendLine("PromptName,Version,Status,Goal,Inputs,Outputs,PlatformLocation,Notes");
        foreach (var prompt in prompts)
        {
            builder.AppendLine(string.Join(",", new[]
            {
                Csv(prompt.PromptName),
                Csv(prompt.VersionNumber.ToString()),
                Csv(prompt.Status.ToString()),
                Csv(prompt.Goal),
                Csv(prompt.Inputs),
                Csv(prompt.Outputs),
                Csv(prompt.PlatformLocation),
                Csv(prompt.Notes)
            }));
        }

        return File(Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", "prompt-inventory.csv");
    }

    private async Task<ReadinessFormSettings?> GetActiveSettingsAsync(bool asNoTracking)
    {
        var query = context.ReadinessFormSettings.AsQueryable();
        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query
            .Where(settings => settings.IsActive)
            .OrderByDescending(settings => settings.LastModifiedAt ?? settings.CreatedAt)
            .FirstOrDefaultAsync();
    }

    private static string Csv(string? value)
    {
        var text = value ?? string.Empty;
        return $"\"{text.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }
}
