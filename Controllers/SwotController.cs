using AI_Readiness_Hub.Data;
using AI_Readiness_Hub.Models;
using Microsoft.AspNetCore.Mvc;

namespace AI_Readiness_Hub.Controllers;

[Route("Swot")]
public class SwotController(ApplicationDbContext context) : Controller
{
    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int clientId, SwotAnalysisItem item)
    {
        item.ClientCompanyId = clientId;
        item.CreatedAt = DateTime.UtcNow;
        context.SwotAnalysisItems.Add(item);
        await LogAsync(clientId, "SWOT item added", $"SWOT item added: {item.Category}.");
        await context.SaveChangesAsync();
        return RedirectToWorkspace(clientId);
    }

    [HttpPost("UpdateStatus")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, ItemReviewStatus status, string? consultantComment)
    {
        var item = await context.SwotAnalysisItems.FindAsync(id);
        if (item is null)
        {
            return NotFound();
        }

        item.Status = status;
        item.ConsultantComment = consultantComment;
        item.LastModifiedAt = DateTime.UtcNow;
        await LogAsync(item.ClientCompanyId, "SWOT item updated", $"SWOT item {status}.");
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
        return RedirectToAction("Workspace", "Clients", this.ToWorkspaceRouteValues(clientId));
    }
}
