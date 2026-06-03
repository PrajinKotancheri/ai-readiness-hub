using AI_Readiness_Hub.Data;
using AI_Readiness_Hub.Models;
using Microsoft.AspNetCore.Mvc;

namespace AI_Readiness_Hub.Controllers;

[Route("Roadmap")]
public class RoadmapController(ApplicationDbContext context) : Controller
{
    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int clientId, AIRoadmapItem item)
    {
        item.ClientCompanyId = clientId;
        item.CreatedAt = DateTime.UtcNow;
        context.AIRoadmapItems.Add(item);
        await LogAsync(clientId, "Roadmap item added", $"Roadmap item added: {item.Title}.");
        await context.SaveChangesAsync();
        return RedirectToWorkspace(clientId);
    }

    [HttpPost("UpdateStatus")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, ApprovalStatus status)
    {
        var item = await context.AIRoadmapItems.FindAsync(id);
        if (item is null)
        {
            return NotFound();
        }

        item.Status = status;
        item.LastModifiedAt = DateTime.UtcNow;
        await LogAsync(item.ClientCompanyId, "Roadmap item updated", $"{item.Title} status changed to {status}.");
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
