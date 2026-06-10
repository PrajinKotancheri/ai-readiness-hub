using AI_Readiness_Hub.Data;
using AI_Readiness_Hub.Models;
using AI_Readiness_Hub.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AI_Readiness_Hub.Controllers;

[Route("KnowledgeGaps")]
public class KnowledgeGapsController(
    ApplicationDbContext context,
    IKnowledgeGapAnalysisService knowledgeGapService) : Controller
{
    [HttpPost("Generate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(int clientId)
    {
        try
        {
            var created = await knowledgeGapService.GenerateAsync(clientId);
            TempData["SuccessMessage"] = created == 0
                ? "Knowledge gap analysis refreshed. No new missing-understanding items were found."
                : $"Knowledge gap analysis generated {created} item{(created == 1 ? string.Empty : "s")}.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToWorkspace(clientId);
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        int clientId,
        KnowledgeGapArea gapArea,
        string missingInformation,
        string? whyItMatters,
        string? followUpQuestion,
        string? suggestedEvidence,
        KnowledgeGapPriority priority)
    {
        if (string.IsNullOrWhiteSpace(missingInformation))
        {
            TempData["ErrorMessage"] = "Missing information is required.";
            return RedirectToWorkspace(clientId);
        }

        context.KnowledgeGapItems.Add(new KnowledgeGapItem
        {
            ClientCompanyId = clientId,
            GapArea = gapArea,
            MissingInformation = missingInformation.Trim(),
            WhyItMatters = whyItMatters?.Trim(),
            FollowUpQuestion = followUpQuestion?.Trim(),
            SuggestedEvidence = suggestedEvidence?.Trim(),
            Priority = priority,
            Status = KnowledgeGapStatus.Open,
            CreatedAt = DateTime.UtcNow
        });
        await LogAsync(clientId, "Knowledge Gap item edited", "Knowledge gap item added manually.");
        await context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Knowledge gap item added.";
        return RedirectToWorkspace(clientId);
    }

    [HttpPost("Update")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(
        int id,
        KnowledgeGapArea gapArea,
        string missingInformation,
        string? whyItMatters,
        string? followUpQuestion,
        string? suggestedEvidence,
        KnowledgeGapPriority priority,
        KnowledgeGapStatus status)
    {
        var item = await context.KnowledgeGapItems.FindAsync(id);
        if (item is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(missingInformation))
        {
            TempData["ErrorMessage"] = "Missing information is required.";
            return RedirectToWorkspace(item.ClientCompanyId);
        }

        item.GapArea = gapArea;
        item.MissingInformation = missingInformation.Trim();
        item.WhyItMatters = whyItMatters?.Trim();
        item.FollowUpQuestion = followUpQuestion?.Trim();
        item.SuggestedEvidence = suggestedEvidence?.Trim();
        item.Priority = priority;
        item.Status = status;
        item.LastModifiedAt = DateTime.UtcNow;
        await LogAsync(item.ClientCompanyId, "Knowledge Gap item edited", $"Knowledge gap item {item.Id} updated.");
        await context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Knowledge gap item saved.";
        return RedirectToWorkspace(item.ClientCompanyId);
    }

    [HttpPost("MarkAnswered")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAnswered(int id)
    {
        var item = await context.KnowledgeGapItems.FindAsync(id);
        if (item is null)
        {
            return NotFound();
        }

        item.Status = KnowledgeGapStatus.Answered;
        item.LastModifiedAt = DateTime.UtcNow;
        await LogAsync(item.ClientCompanyId, "Knowledge Gap item edited", $"Knowledge gap item {item.Id} marked answered.");
        await context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Knowledge gap item marked answered.";
        return RedirectToWorkspace(item.ClientCompanyId);
    }

    [HttpPost("Approve")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var item = await context.KnowledgeGapItems.FindAsync(id);
        if (item is null)
        {
            return NotFound();
        }

        item.Status = KnowledgeGapStatus.Approved;
        item.ApprovedAt = DateTime.UtcNow;
        item.ApprovedBy = "Consultant";
        item.LastModifiedAt = DateTime.UtcNow;
        await LogAsync(item.ClientCompanyId, "Knowledge Gap item approved", $"Knowledge gap item {item.Id} approved.");
        await context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Knowledge gap item approved.";
        return RedirectToWorkspace(item.ClientCompanyId);
    }

    [HttpPost("AddSource")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddSource(
        int clientId,
        int? outputId,
        AIOutputSourceType sourceType,
        AIOutputSourceCategory sourceCategory,
        string sourceLabel,
        string? sourceReference,
        string? sourceUrl,
        string? evidenceText)
    {
        if (string.IsNullOrWhiteSpace(sourceLabel))
        {
            TempData["ErrorMessage"] = "Source label is required.";
            return RedirectToWorkspace(clientId);
        }

        context.AIOutputSources.Add(new AIOutputSource
        {
            ClientCompanyId = clientId,
            OutputType = AIOutputType.KnowledgeGap,
            OutputId = outputId,
            SourceType = sourceType,
            SourceCategory = sourceCategory,
            SourceLabel = sourceLabel.Trim(),
            SourceReference = sourceReference?.Trim(),
            SourceUrl = sourceUrl?.Trim(),
            EvidenceText = evidenceText?.Trim(),
            CreatedAt = DateTime.UtcNow
        });
        await LogAsync(clientId, "Source attribution added", $"Knowledge gap source added: {sourceLabel.Trim()}.");
        await context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Source attribution added.";
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
        return RedirectToAction("Workspace", "Clients", this.ToWorkspaceRouteValues(clientId));
    }
}
