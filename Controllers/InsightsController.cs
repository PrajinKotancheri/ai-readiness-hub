using AI_Readiness_Hub.Data;
using AI_Readiness_Hub.Models;
using Microsoft.AspNetCore.Mvc;

namespace AI_Readiness_Hub.Controllers;

[Route("Insights")]
public class InsightsController(ApplicationDbContext context) : Controller
{
    [HttpPost("Industry")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Industry(int clientId, IndustryInsight insight)
    {
        insight.ClientCompanyId = clientId;
        insight.CreatedAt = DateTime.UtcNow;
        context.IndustryInsights.Add(insight);
        await LogAsync(clientId, "Industry insight added", $"Industry insight added: {insight.Topic}.");
        await context.SaveChangesAsync();
        return RedirectToWorkspace(clientId);
    }

    [HttpPost("Competitor")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Competitor(int clientId, CompetitorInsight insight)
    {
        insight.ClientCompanyId = clientId;
        insight.CreatedAt = DateTime.UtcNow;
        context.CompetitorInsights.Add(insight);
        await LogAsync(clientId, "Competitor insight added", $"Competitor insight added: {insight.CompetitorName}.");
        await context.SaveChangesAsync();
        return RedirectToWorkspace(clientId);
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
