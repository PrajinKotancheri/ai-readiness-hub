using AI_Readiness_Hub.Data;
using AI_Readiness_Hub.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AI_Readiness_Hub.Controllers;

[Route("Notes")]
public class NotesController(ApplicationDbContext context) : Controller
{
    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int clientId, ConsultantNote note)
    {
        if (!await context.ClientCompanies.AnyAsync(client => client.Id == clientId))
        {
            return NotFound();
        }

        note.ClientCompanyId = clientId;
        note.CreatedAt = DateTime.UtcNow;
        note.CreatedBy = string.IsNullOrWhiteSpace(note.CreatedBy) ? "Consultant" : note.CreatedBy;
        context.ConsultantNotes.Add(note);
        await LogAsync(clientId, "Note added", $"Consultant note added: {note.NoteTitle}.");
        await context.SaveChangesAsync();
        return RedirectToWorkspace(clientId);
    }

    [HttpPost("Transcript")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Transcript(int clientId, MeetingTranscript transcript)
    {
        if (!await context.ClientCompanies.AnyAsync(client => client.Id == clientId))
        {
            return NotFound();
        }

        transcript.ClientCompanyId = clientId;
        transcript.CreatedAt = DateTime.UtcNow;
        transcript.CreatedBy = string.IsNullOrWhiteSpace(transcript.CreatedBy) ? "Consultant" : transcript.CreatedBy;
        transcript.Summary ??= "Placeholder summary. Consultant should edit after reviewing the transcript.";
        context.MeetingTranscripts.Add(transcript);
        await MarkWorkflowAsync(clientId, "Consultant Session Completed", WorkflowStepStatus.Completed);
        await LogAsync(clientId, "Transcript added", $"Meeting transcript added: {transcript.SessionTitle}.");
        await context.SaveChangesAsync();
        return RedirectToWorkspace(clientId);
    }

    private async Task MarkWorkflowAsync(int clientId, string stageName, WorkflowStepStatus status)
    {
        var step = await context.ClientWorkflowSteps
            .Where(item => item.ClientCompanyId == clientId && item.StageName == stageName)
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.Id)
            .FirstOrDefaultAsync();
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
