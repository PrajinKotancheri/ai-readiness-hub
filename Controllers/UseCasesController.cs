using AI_Readiness_Hub.Data;
using AI_Readiness_Hub.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AI_Readiness_Hub.Controllers;

[Route("UseCases")]
public class UseCasesController(ApplicationDbContext context) : Controller
{
    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int clientId, AIUseCase useCase)
    {
        useCase.ClientCompanyId = clientId;
        useCase.CreatedAt = DateTime.UtcNow;
        context.AIUseCases.Add(useCase);
        await LogAsync(clientId, "Use case added", $"Use case added: {useCase.Title}.");
        await context.SaveChangesAsync();
        return RedirectToWorkspace(clientId);
    }

    [HttpPost("Score")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Score(int useCaseId, AIUseCaseScore score)
    {
        var useCase = await context.AIUseCases
            .Include(item => item.Score)
            .FirstOrDefaultAsync(item => item.Id == useCaseId);
        if (useCase is null)
        {
            return NotFound();
        }

        var target = useCase.Score ?? new AIUseCaseScore { AIUseCaseId = useCaseId, CreatedAt = DateTime.UtcNow };
        target.RoiScore = score.RoiScore;
        target.FeasibilityScore = score.FeasibilityScore;
        target.RiskSafetyScore = score.RiskSafetyScore;
        target.StrategicFitScore = score.StrategicFitScore;
        target.DataReadinessScore = score.DataReadinessScore;
        target.ScoringComment = score.ScoringComment;
        target.LastModifiedAt = DateTime.UtcNow;
        target.RecalculatePriority();

        if (useCase.Score is null)
        {
            context.AIUseCaseScores.Add(target);
        }

        await LogAsync(useCase.ClientCompanyId, "Use case scored", $"Use case scored: {useCase.Title}.");
        await context.SaveChangesAsync();
        return RedirectToWorkspace(useCase.ClientCompanyId);
    }

    [HttpPost("ShortlistTop3")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ShortlistTop3(int clientId)
    {
        var useCases = await context.AIUseCases
            .Include(item => item.Score)
            .Where(item => item.ClientCompanyId == clientId)
            .ToListAsync();

        foreach (var useCase in useCases)
        {
            useCase.Status = UseCaseStatus.Suggested;
            useCase.LastModifiedAt = DateTime.UtcNow;
        }

        foreach (var useCase in useCases.OrderByDescending(item => item.Score?.PriorityScore ?? 0).Take(3))
        {
            useCase.Status = UseCaseStatus.Shortlisted;
        }

        await LogAsync(clientId, "Use cases shortlisted", "Top three use cases shortlisted by priority score.");
        await context.SaveChangesAsync();
        return RedirectToWorkspace(clientId);
    }

    [HttpPost("UpdateStatus")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, UseCaseStatus status)
    {
        var useCase = await context.AIUseCases.FindAsync(id);
        if (useCase is null)
        {
            return NotFound();
        }

        useCase.Status = status;
        useCase.LastModifiedAt = DateTime.UtcNow;
        await LogAsync(useCase.ClientCompanyId, "Use case updated", $"{useCase.Title} status changed to {status}.");
        await context.SaveChangesAsync();
        return RedirectToWorkspace(useCase.ClientCompanyId);
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
