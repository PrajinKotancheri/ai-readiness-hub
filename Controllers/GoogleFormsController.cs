using System.Text.Json;
using AI_Readiness_Hub.Data;
using AI_Readiness_Hub.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AI_Readiness_Hub.Controllers;

[ApiController]
[Route("api/google-forms")]
public class GoogleFormsController(ApplicationDbContext context) : ControllerBase
{
    [HttpPost("assessment-response")]
    public async Task<IActionResult> AssessmentResponse(GoogleFormAssessmentResponseRequest request)
    {
        var settings = await context.ReadinessFormSettings
            .Where(item => item.IsActive)
            .OrderByDescending(item => item.LastModifiedAt ?? item.CreatedAt)
            .FirstOrDefaultAsync();

        if (settings is null ||
            string.IsNullOrWhiteSpace(settings.WebhookSecret) ||
            string.IsNullOrWhiteSpace(request.Secret) ||
            !string.Equals(settings.WebhookSecret, request.Secret, StringComparison.Ordinal))
        {
            return Unauthorized(new { success = false, message = "Invalid webhook secret." });
        }

        if (string.IsNullOrWhiteSpace(request.ClientToken))
        {
            return BadRequest(new { success = false, message = "clientToken is required." });
        }

        var assessment = await context.ReadinessAssessments
            .Include(item => item.Answers)
            .Include(item => item.ClientCompany)
            .FirstOrDefaultAsync(item => item.ClientToken == request.ClientToken);

        if (assessment is null)
        {
            return NotFound(new { success = false, message = "No assessment was found for the supplied client token." });
        }

        foreach (var incomingAnswer in request.Answers)
        {
            if (string.IsNullOrWhiteSpace(incomingAnswer.QuestionText))
            {
                continue;
            }

            var existingAnswer = assessment.Answers.FirstOrDefault(answer =>
                string.Equals(answer.QuestionText, incomingAnswer.QuestionText, StringComparison.OrdinalIgnoreCase));

            if (existingAnswer is null)
            {
                assessment.Answers.Add(new AssessmentAnswer
                {
                    SectionName = string.IsNullOrWhiteSpace(incomingAnswer.SectionName) ? "Imported from Google Form" : incomingAnswer.SectionName.Trim(),
                    QuestionText = incomingAnswer.QuestionText.Trim(),
                    AnswerText = incomingAnswer.AnswerText,
                    AnswerType = string.IsNullOrWhiteSpace(incomingAnswer.AnswerType) ? "Text" : incomingAnswer.AnswerType.Trim(),
                    CompletenessStatus = string.IsNullOrWhiteSpace(incomingAnswer.AnswerText) ? CompletenessStatus.Missing : CompletenessStatus.Complete,
                    CreatedAt = DateTime.UtcNow
                });
                continue;
            }

            existingAnswer.SectionName = string.IsNullOrWhiteSpace(incomingAnswer.SectionName)
                ? existingAnswer.SectionName
                : incomingAnswer.SectionName.Trim();
            existingAnswer.AnswerText = incomingAnswer.AnswerText;
            existingAnswer.AnswerType = string.IsNullOrWhiteSpace(incomingAnswer.AnswerType) ? existingAnswer.AnswerType : incomingAnswer.AnswerType.Trim();
            existingAnswer.CompletenessStatus = string.IsNullOrWhiteSpace(incomingAnswer.AnswerText)
                ? CompletenessStatus.Missing
                : CompletenessStatus.Complete;
        }

        var receivedAt = request.SubmittedAt?.ToUniversalTime() ?? DateTime.UtcNow;
        assessment.RawResponseJson = request.RawResponse.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
            ? JsonSerializer.Serialize(request)
            : request.RawResponse.GetRawText();
        assessment.FormStatus = ReadinessFormStatus.Completed;
        assessment.CompletedAt = receivedAt;
        assessment.ResponseReceivedAt = receivedAt;
        assessment.ExternalResponseId = request.ExternalResponseId;
        assessment.Summary = $"Received {request.Answers.Count} answers from Google Form.";
        assessment.LastModifiedAt = DateTime.UtcNow;

        if (assessment.ClientCompany is not null)
        {
            assessment.ClientCompany.CurrentStage = ClientStage.AssessmentCompleted;
            assessment.ClientCompany.NextAction = "Review received assessment response and generate gap analysis";
            assessment.ClientCompany.LastModifiedAt = DateTime.UtcNow;
        }

        await MarkWorkflowAsync(assessment.ClientCompanyId, "Form Completed");
        context.ClientActivityLogs.Add(new ClientActivityLog
        {
            ClientCompanyId = assessment.ClientCompanyId,
            ActivityType = "Assessment response received",
            Description = "Assessment response received from Google Form.",
            CreatedBy = "Google Forms webhook",
            CreatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();
        return Ok(new { success = true, assessmentId = assessment.Id, answers = request.Answers.Count });
    }

    private async Task MarkWorkflowAsync(int clientId, string stageName)
    {
        var step = await context.ClientWorkflowSteps
            .FirstOrDefaultAsync(item => item.ClientCompanyId == clientId && item.StageName == stageName);

        if (step is null)
        {
            return;
        }

        step.Status = WorkflowStepStatus.Completed;
        step.CompletedAt = DateTime.UtcNow;
    }
}

public class GoogleFormAssessmentResponseRequest
{
    public string? Secret { get; set; }
    public string? ClientToken { get; set; }
    public string? ExternalResponseId { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public List<GoogleFormAnswerRequest> Answers { get; set; } = [];
    public JsonElement RawResponse { get; set; }
}

public class GoogleFormAnswerRequest
{
    public string? SectionName { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string? AnswerText { get; set; }
    public string? AnswerType { get; set; }
}
