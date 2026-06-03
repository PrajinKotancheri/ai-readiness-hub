using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AI_Readiness_Hub.Data;
using AI_Readiness_Hub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AI_Readiness_Hub.Controllers;

[ApiController]
[Route("api/google-forms")]
public class GoogleFormsController(
    ApplicationDbContext context,
    ILogger<GoogleFormsController> logger) : ControllerBase
{
    [AllowAnonymous]
    [EnableCors("GoogleFormsWebhook")]
    [HttpPost("/api/form-response")]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<IActionResult> FormResponse(GoogleFormResponseWebhookRequest request, CancellationToken cancellationToken)
    {
        var token = request.Token?.Trim();
        logger.LogInformation("Google Form response webhook received for token {ClientToken}.", token ?? "(missing)");

        if (string.IsNullOrWhiteSpace(token))
        {
            return BadRequest(new { success = false, message = "token is required." });
        }

        var responseId = request.ResponseId?.Trim();
        if (string.IsNullOrWhiteSpace(responseId))
        {
            return BadRequest(new { success = false, message = "responseId is required." });
        }

        var suppliedSecret = request.Secret?.Trim();
        if (string.IsNullOrWhiteSpace(suppliedSecret))
        {
            return BadRequest(new { success = false, message = "secret is required." });
        }

        try
        {
            var settings = await GetActiveSettingsAsync(cancellationToken);
            if (settings is null || string.IsNullOrWhiteSpace(settings.WebhookSecret))
            {
                logger.LogError("Google Form webhook secret is not configured in active readiness form settings.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { success = false, message = "Webhook secret is not configured." });
            }

            if (!SecretMatches(settings.WebhookSecret, suppliedSecret))
            {
                logger.LogWarning("Google Form response webhook secret validation failed for token {ClientToken}.", token);
                return Unauthorized(new { success = false, message = "Invalid webhook secret." });
            }

            logger.LogInformation("Google Form response webhook secret validated for token {ClientToken}.", token);

            var assessment = await context.ReadinessAssessments
                .Include(item => item.ClientCompany)
                .FirstOrDefaultAsync(item => item.ClientToken == token, cancellationToken);

            if (assessment is null)
            {
                logger.LogWarning("No readiness assessment found for Google Form token {ClientToken}.", token);
                return NotFound(new { success = false, message = "No assessment was found for the supplied token." });
            }

            logger.LogInformation(
                "Readiness assessment {AssessmentId} found for Google Form token {ClientToken}.",
                assessment.Id,
                token);

            var receivedAt = DateTime.UtcNow;
            assessment.FormStatus = ReadinessFormStatus.Completed;
            assessment.ResponseReceivedAt = receivedAt;
            assessment.CompletedAt = receivedAt;
            assessment.ExternalResponseId = responseId;
            assessment.RawResponseJson = JsonSerializer.Serialize(new
            {
                token,
                responseId,
                receivedAt
            });
            assessment.Summary = "Google Form response received.";
            assessment.LastModifiedAt = receivedAt;

            if (assessment.ClientCompany is not null)
            {
                assessment.ClientCompany.CurrentStage = ClientStage.AssessmentCompleted;
                assessment.ClientCompany.NextAction = "Review received assessment response and generate gap analysis";
                assessment.ClientCompany.LastModifiedAt = receivedAt;
            }

            await MarkWorkflowAsync(assessment.ClientCompanyId, "Form Completed", cancellationToken);
            context.ClientActivityLogs.Add(new ClientActivityLog
            {
                ClientCompanyId = assessment.ClientCompanyId,
                ActivityType = "Assessment response received",
                Description = $"Google Form response received. Response ID: {responseId}.",
                CreatedBy = "Google Forms webhook",
                CreatedAt = receivedAt
            });

            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation(
                "Google Form response webhook updated readiness assessment {AssessmentId} for token {ClientToken}.",
                assessment.Id,
                token);

            return Ok(new
            {
                success = true,
                assessmentId = assessment.Id,
                responseReceived = true,
                externalResponseId = assessment.ExternalResponseId
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Google Form response webhook failed for token {ClientToken}.", token);
            return StatusCode(StatusCodes.Status500InternalServerError, new { success = false, message = "Failed to process form response webhook." });
        }
    }

    [AllowAnonymous]
    [HttpPost("assessment-response")]
    public async Task<IActionResult> AssessmentResponse(GoogleFormAssessmentResponseRequest request)
    {
        var settings = await GetActiveSettingsAsync(HttpContext.RequestAborted);

        if (settings is null ||
            string.IsNullOrWhiteSpace(settings.WebhookSecret) ||
            string.IsNullOrWhiteSpace(request.Secret) ||
            !SecretMatches(settings.WebhookSecret, request.Secret))
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
            ? JsonSerializer.Serialize(new
            {
                clientToken = request.ClientToken,
                externalResponseId = request.ExternalResponseId,
                submittedAt = request.SubmittedAt,
                answers = request.Answers
            })
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

    private async Task<ReadinessFormSettings?> GetActiveSettingsAsync(CancellationToken cancellationToken)
    {
        return await context.ReadinessFormSettings
            .Where(item => item.IsActive)
            .OrderByDescending(item => item.LastModifiedAt ?? item.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task MarkWorkflowAsync(int clientId, string stageName, CancellationToken cancellationToken = default)
    {
        var step = await context.ClientWorkflowSteps
            .FirstOrDefaultAsync(item => item.ClientCompanyId == clientId && item.StageName == stageName, cancellationToken);

        if (step is null)
        {
            return;
        }

        step.Status = WorkflowStepStatus.Completed;
        step.CompletedAt = DateTime.UtcNow;
    }

    private static bool SecretMatches(string expectedSecret, string suppliedSecret)
    {
        var expectedBytes = Encoding.UTF8.GetBytes(expectedSecret);
        var suppliedBytes = Encoding.UTF8.GetBytes(suppliedSecret);

        return expectedBytes.Length == suppliedBytes.Length &&
            CryptographicOperations.FixedTimeEquals(expectedBytes, suppliedBytes);
    }
}

public class GoogleFormResponseWebhookRequest
{
    [JsonPropertyName("token")]
    public string? Token { get; set; }

    [JsonPropertyName("responseId")]
    public string? ResponseId { get; set; }

    [JsonPropertyName("secret")]
    public string? Secret { get; set; }
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
