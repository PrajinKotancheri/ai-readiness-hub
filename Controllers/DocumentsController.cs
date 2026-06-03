using AI_Readiness_Hub.Data;
using AI_Readiness_Hub.Models;
using AI_Readiness_Hub.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AI_Readiness_Hub.Controllers;

[Route("Documents")]
public class DocumentsController(ApplicationDbContext context, IClientDocumentSummaryService summaryService) : Controller
{
    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int clientId, ClientDocument document, IFormFile? upload)
    {
        var client = await context.ClientCompanies.FindAsync(clientId);
        if (client is null)
        {
            return NotFound();
        }

        document.ClientCompanyId = clientId;
        document.UploadedAt = DateTime.UtcNow;
        document.CreatedAt = DateTime.UtcNow;

        if (upload is not null && upload.Length > 0)
        {
            var folder = Path.Combine("wwwroot", "uploads", $"client-{clientId}");
            Directory.CreateDirectory(folder);
            var fileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}-{Path.GetFileName(upload.FileName)}";
            var path = Path.Combine(folder, fileName);
            await using var stream = System.IO.File.Create(path);
            await upload.CopyToAsync(stream);
            document.FileName = string.IsNullOrWhiteSpace(document.FileName) ? upload.FileName : document.FileName;
            document.FilePath = $"/uploads/client-{clientId}/{fileName}";
        }

        if (string.IsNullOrWhiteSpace(document.AiSummary))
        {
            document.AiSummary = await summaryService.GeneratePlaceholderSummaryAsync(document);
        }

        context.ClientDocuments.Add(document);
        client.CurrentStage = ClientStage.DocumentsUploaded;
        client.NextAction = "Review documents and generate analysis";
        client.LastModifiedAt = DateTime.UtcNow;

        await MarkWorkflowAsync(clientId, "Documents Uploaded", WorkflowStepStatus.Completed);
        await LogAsync(clientId, "Document uploaded", $"Document registered: {document.FileName}.");
        await context.SaveChangesAsync();
        return RedirectToWorkspace(clientId);
    }

    [HttpPost("UpdateInsights")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateInsights(int id, string? aiSummary, string? keyInsights, bool usedInReport)
    {
        var document = await context.ClientDocuments.FindAsync(id);
        if (document is null)
        {
            return NotFound();
        }

        document.AiSummary = aiSummary;
        document.KeyInsights = keyInsights;
        document.UsedInReport = usedInReport;
        await LogAsync(document.ClientCompanyId, "Document updated", $"Document insights updated: {document.FileName}.");
        await context.SaveChangesAsync();
        return RedirectToWorkspace(document.ClientCompanyId);
    }

    private async Task MarkWorkflowAsync(int clientId, string stageName, WorkflowStepStatus status)
    {
        var step = await context.ClientWorkflowSteps.FirstOrDefaultAsync(item => item.ClientCompanyId == clientId && item.StageName == stageName);
        if (step is not null)
        {
            step.Status = status;
            step.CompletedAt = status == WorkflowStepStatus.Completed ? DateTime.UtcNow : step.CompletedAt;
        }
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
