using AI_Readiness_Hub.Data;
using AI_Readiness_Hub.Models;
using AI_Readiness_Hub.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AI_Readiness_Hub.Controllers;

[Route("Analysis")]
public class AnalysisController(
    ApplicationDbContext context,
    IAIConsultingAnalysisService analysisService,
    ILogger<AnalysisController> logger) : Controller
{
    [HttpPost("Generate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(int clientId, string operation)
    {
        var operationName = operation switch
        {
            "company-summary" => "company summary",
            "gap-analysis" => "gap analysis",
            "swot" => "SWOT",
            "industry" => "industry analysis",
            "competitors" => "competitor insights",
            "use-cases" => "AI use cases",
            "score-use-cases" => "use case scoring",
            "readiness-score" => "readiness score",
            "calculate-readiness-score" => "readiness score",
            "roadmap" => "roadmap",
            "report" => "strategic report draft",
            _ => null
        };

        if (operationName is null)
        {
            TempData["ErrorMessage"] = "Unknown analysis operation.";
            return RedirectToWorkspace(clientId);
        }

        var succeeded = await RunAnalysisOperationAsync(clientId, operation, operationName);
        if (succeeded)
        {
            TempData["SuccessMessage"] = $"{operationName} generated.";
        }

        return RedirectToWorkspace(clientId);
    }

    [HttpPost("CalculateReadinessScore")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CalculateReadinessScore(int clientId)
    {
        var succeeded = await RunAnalysisOperationAsync(clientId, "calculate-readiness-score", "readiness score");
        if (succeeded)
        {
            TempData["SuccessMessage"] = "Readiness score calculated.";
        }

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
        return RedirectToAction("Workspace", "Clients", this.ToWorkspaceRouteValues(clientId));
    }

    private async Task<bool> RunAnalysisOperationAsync(int clientId, string operation, string operationName)
    {
        try
        {
            var warning = await BuildDependencyWarningAsync(clientId, operation);
            if (!string.IsNullOrWhiteSpace(warning))
            {
                TempData["WarningMessage"] = warning;
            }

            var action = operation switch
            {
                "company-summary" => analysisService.GenerateCompanySummaryAsync(clientId),
                "gap-analysis" => analysisService.GenerateGapAnalysisAsync(clientId),
                "swot" => analysisService.GenerateSwotAnalysisAsync(clientId),
                "industry" => analysisService.GenerateIndustryAnalysisAsync(clientId),
                "competitors" => analysisService.GenerateCompetitorInsightsAsync(clientId),
                "use-cases" => analysisService.GenerateUseCasesAsync(clientId),
                "score-use-cases" => analysisService.ScoreUseCasesAsync(clientId),
                "readiness-score" or "calculate-readiness-score" => analysisService.GenerateReadinessScoreAsync(clientId),
                "roadmap" => analysisService.GenerateRoadmapAsync(clientId),
                "report" => analysisService.GenerateReportDraftAsync(clientId),
                _ => Task.CompletedTask
            };

            await action;
            return true;
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(
                ex,
                "Analysis operation could not run. ClientCompanyId: {ClientCompanyId}; Operation: {Operation}",
                clientId,
                operation);
            TempData["ErrorMessage"] = ex.Message;
            return false;
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(
                ex,
                "Analysis operation database update failed. ClientCompanyId: {ClientCompanyId}; Operation: {Operation}",
                clientId,
                operation);
            TempData["ErrorMessage"] = $"Could not save the {operationName}. Please try again.";
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Analysis operation failed. ClientCompanyId: {ClientCompanyId}; Operation: {Operation}",
                clientId,
                operation);
            TempData["ErrorMessage"] = $"Could not generate the {operationName}. Please try again.";
            return false;
        }
    }

    private async Task<string?> BuildDependencyWarningAsync(int clientId, string operation)
    {
        return operation switch
        {
            "company-summary" when !await HasApprovedKnowledgeGapAsync(clientId) =>
                "Company summary was generated before any knowledge gap item was approved.",
            "industry" when !await HasApprovedAnalysisOutputAsync(clientId, AnalysisType.CompanySummary) =>
                "Industry analysis was generated before the company summary was approved.",
            "competitors" when !await HasApprovedAnalysisOutputAsync(clientId, AnalysisType.IndustryAnalysis) =>
                "Competitor insights were generated before industry analysis was approved.",
            "swot" when !await HasApprovedAnalysisOutputAsync(clientId, AnalysisType.CompetitorInsights) =>
                "SWOT was generated before competitor insights were approved.",
            "use-cases" when !await HasApprovedAnalysisOutputAsync(clientId, AnalysisType.Swot) =>
                "Use cases were generated before SWOT was approved.",
            "score-use-cases" when !await context.AIUseCases.AsNoTracking().AnyAsync(item => item.ClientCompanyId == clientId && item.Status == UseCaseStatus.Approved) =>
                "Use cases were scored before any use case was approved.",
            "roadmap" when !await context.AIUseCaseScores.AsNoTracking().AnyAsync(score => score.AIUseCase!.ClientCompanyId == clientId) =>
                "Roadmap was generated before use cases were scored.",
            "report" when !await context.AIRoadmapItems.AsNoTracking().AnyAsync(item => item.ClientCompanyId == clientId && item.Status == ApprovalStatus.Approved) =>
                "Strategic report was generated before any roadmap item was approved.",
            _ => null
        };
    }

    private async Task<bool> HasApprovedKnowledgeGapAsync(int clientId)
    {
        return await context.KnowledgeGapItems
            .AsNoTracking()
            .AnyAsync(item => item.ClientCompanyId == clientId && item.Status == KnowledgeGapStatus.Approved);
    }

    private async Task<bool> HasApprovedAnalysisOutputAsync(int clientId, AnalysisType analysisType)
    {
        return await context.AIAnalysisOutputs
            .AsNoTracking()
            .AnyAsync(output =>
                output.ClientCompanyId == clientId &&
                output.AnalysisType == analysisType &&
                output.Status == DraftStatus.Approved);
    }
}
