using AI_Readiness_Hub.Data;
using AI_Readiness_Hub.Models;
using Microsoft.AspNetCore.Mvc;

namespace AI_Readiness_Hub.Controllers;

[Route("GapAnalysis")]
public class GapAnalysisController(ApplicationDbContext context) : Controller
{
    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int clientId, GapAnalysisItem item)
    {
        item.ClientCompanyId = clientId;
        item.CreatedAt = DateTime.UtcNow;
        context.GapAnalysisItems.Add(item);
        await LogAsync(clientId, "Gap item added", $"Gap item added: {item.GapArea}.");
        await context.SaveChangesAsync();
        return RedirectToWorkspace(clientId);
    }

    [HttpPost("Update")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, GapStatus status, Severity severity, string? consultantAction)
    {
        var item = await context.GapAnalysisItems.FindAsync(id);
        if (item is null)
        {
            return NotFound();
        }

        item.Status = status;
        item.Severity = severity;
        if (!string.IsNullOrWhiteSpace(consultantAction))
        {
            item.SuggestedAction = consultantAction;
        }
        item.LastModifiedAt = DateTime.UtcNow;
        await LogAsync(item.ClientCompanyId, "Gap item updated", $"Gap item updated: {item.GapArea}.");
        await context.SaveChangesAsync();
        return RedirectToWorkspace(item.ClientCompanyId);
    }

    private Task LogAsync(int clientId, string activityType, string description)
    {
        context.ClientActivityLogs.Add(new ClientActivityLog
        {
            ClientCompanyId = clientId,
            ActivityType = activityType,
            Description = description,
            CreatedBy = "Consultant",
            CreatedAt = DateTime.UtcNow
        });
        return Task.CompletedTask;
    }

    private RedirectToActionResult RedirectToWorkspace(int clientId)
    {
        return RedirectToAction("Workspace", "Clients", new { id = clientId });
    }
}
