using AI_Readiness_Hub.Data;
using AI_Readiness_Hub.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AI_Readiness_Hub.Controllers;

[Route("Settings")]
public class SettingsController(ApplicationDbContext context) : Controller
{
    [HttpGet("ReadinessForm")]
    public async Task<IActionResult> ReadinessForm()
    {
        var settings = await GetActiveSettingsAsync() ?? new ReadinessFormSettings();
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
            : await GetActiveSettingsAsync();

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

    private async Task<ReadinessFormSettings?> GetActiveSettingsAsync()
    {
        return await context.ReadinessFormSettings
            .Where(settings => settings.IsActive)
            .OrderByDescending(settings => settings.LastModifiedAt ?? settings.CreatedAt)
            .FirstOrDefaultAsync();
    }
}
