using AI_Readiness_Hub.Data;
using AI_Readiness_Hub.Models;
using AI_Readiness_Hub.Services;
using Microsoft.AspNetCore.Mvc;

namespace AI_Readiness_Hub.Controllers;

[Route("Analysis")]
public class AnalysisController(ApplicationDbContext context, IAIConsultingAnalysisService analysisService) : Controller
{
    [HttpPost("Generate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(int clientId, string operation)
    {
        var action = operation switch
        {
            "company-summary" => analysisService.GenerateCompanySummaryAsync(clientId),
            "gap-analysis" => analysisService.GenerateGapAnalysisAsync(clientId),
            "swot" => analysisService.GenerateSwotAnalysisAsync(clientId),
            "industry" => analysisService.GenerateIndustryAnalysisAsync(clientId),
            "competitors" => analysisService.GenerateCompetitorInsightsAsync(clientId),
            "use-cases" => analysisService.GenerateUseCasesAsync(clientId),
            "score-use-cases" => analysisService.ScoreUseCasesAsync(clientId),
            "readiness-score" => analysisService.GenerateReadinessScoreAsync(clientId),
            "roadmap" => analysisService.GenerateRoadmapAsync(clientId),
            "report" => analysisService.GenerateReportDraftAsync(clientId),
            _ => Task.CompletedTask
        };

        await action;
        return RedirectToWorkspace(clientId);
    }

    [HttpPost("EditOutput")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditOutput(int id, string outputContent, DraftStatus status)
    {
        var output = await context.AIAnalysisOutputs.FindAsync(id);
        if (output is null)
        {
            return NotFound();
        }

        output.OutputContent = outputContent;
        output.Status = status == DraftStatus.DraftGenerated ? DraftStatus.ConsultantEdited : status;
        output.LastModifiedAt = DateTime.UtcNow;
        await LogAsync(output.ClientCompanyId, "AI output edited", $"{output.Title} updated.");
        await context.SaveChangesAsync();
        return RedirectToWorkspace(output.ClientCompanyId);
    }

    [HttpPost("ApproveOutput")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveOutput(int id)
    {
        var output = await context.AIAnalysisOutputs.FindAsync(id);
        if (output is null)
        {
            return NotFound();
        }

        output.Status = DraftStatus.Approved;
        output.ApprovedAt = DateTime.UtcNow;
        output.ApprovedBy = "Consultant";
        output.LastModifiedAt = DateTime.UtcNow;
        await LogAsync(output.ClientCompanyId, "AI output approved", $"{output.Title} approved.");
        await context.SaveChangesAsync();
        return RedirectToWorkspace(output.ClientCompanyId);
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
