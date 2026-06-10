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
                .Include(item => item.Responses)
                .Include(item => item.ClientCompany)
                .Where(item => item.ClientToken == token)
                .OrderByDescending(item => item.CreatedAt)
                .ThenByDescending(item => item.Id)
                .FirstOrDefaultAsync(cancellationToken);

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
            var duplicateResponse = await FindDuplicateResponseAsync(assessment.Id, responseId, cancellationToken);
            if (duplicateResponse is not null)
            {
                logger.LogInformation(
                    "Duplicate Google Form notification ignored for assessment {AssessmentId}, external response id {ExternalResponseId}.",
                    assessment.Id,
                    responseId);

                return Ok(new
                {
                    success = true,
                    assessmentId = assessment.Id,
                    responseId = duplicateResponse.Id,
                    responseLabel = duplicateResponse.ResponseLabel,
                    answers = duplicateResponse.AnswerCount
                });
            }

            var response = CreateAssessmentResponse(
                assessment,
                AssessmentResponseSource.GoogleForm,
                AssessmentResponseStatus.Received,
                responseId,
                token,
                submittedAt: null,
                receivedAt,
                JsonSerializer.Serialize(new { token, responseId, receivedAt }));

            assessment.ResponseReceivedAt = receivedAt;
            assessment.ExternalResponseId = responseId;
            assessment.RawResponseJson = response.RawResponseJson;
            assessment.Summary = $"{response.ResponseLabel} received from Google Form notification.";
            assessment.LastModifiedAt = receivedAt;

            if (assessment.ClientCompany is not null)
            {
                assessment.ClientCompany.NextAction = "Review received assessment response";
                assessment.ClientCompany.LastModifiedAt = receivedAt;
            }

            context.ClientActivityLogs.Add(new ClientActivityLog
            {
                ClientCompanyId = assessment.ClientCompanyId,
                ActivityType = "Assessment response received",
                Description = $"Google Form response received: {response.ResponseLabel} with 0 answers.",
                CreatedBy = "Google Forms webhook",
                CreatedAt = receivedAt
            });

            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation(
                "Google Form response webhook created response {ResponseId} for readiness assessment {AssessmentId} and token {ClientToken}.",
                response.Id,
                assessment.Id,
                token);

            return Ok(new
            {
                success = true,
                assessmentId = assessment.Id,
                responseId = response.Id,
                responseLabel = response.ResponseLabel,
                answers = response.AnswerCount
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
            .Include(item => item.Responses)
                .ThenInclude(response => response.Answers)
            .Include(item => item.ClientCompany)
            .Where(item => item.ClientToken == request.ClientToken)
            .OrderByDescending(item => item.CreatedAt)
            .ThenByDescending(item => item.Id)
            .FirstOrDefaultAsync();

        if (assessment is null)
        {
            return NotFound(new { success = false, message = "No assessment was found for the supplied client token." });
        }

        var submittedAt = request.SubmittedAt?.ToUniversalTime();
        var receivedAt = DateTime.UtcNow;
        var externalResponseId = request.ExternalResponseId?.Trim();
        var duplicateResponse = await FindDuplicateResponseAsync(assessment.Id, externalResponseId, HttpContext.RequestAborted);
        if (duplicateResponse is not null)
        {
            logger.LogInformation(
                "Duplicate Google Form response ignored for assessment {AssessmentId}, external response id {ExternalResponseId}.",
                assessment.Id,
                externalResponseId);

            return Ok(new
            {
                success = true,
                assessmentId = assessment.Id,
                responseId = duplicateResponse.Id,
                responseLabel = duplicateResponse.ResponseLabel,
                answers = duplicateResponse.AnswerCount
            });
        }

        var rawResponseJson = request.RawResponse.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
            ? JsonSerializer.Serialize(new
            {
                clientToken = request.ClientToken,
                externalResponseId,
                submittedAt = request.SubmittedAt,
                answers = request.Answers
            })
            : request.RawResponse.GetRawText();

        var response = CreateAssessmentResponse(
            assessment,
            AssessmentResponseSource.GoogleForm,
            AssessmentResponseStatus.Received,
            externalResponseId,
            request.ClientToken?.Trim(),
            submittedAt,
            receivedAt,
            rawResponseJson);

        foreach (var incomingAnswer in request.Answers.Where(answer => !string.IsNullOrWhiteSpace(answer.QuestionText)))
        {
            response.Answers.Add(new AssessmentAnswer
            {
                ReadinessAssessmentId = assessment.Id,
                SectionName = string.IsNullOrWhiteSpace(incomingAnswer.SectionName) ? "Imported from Google Form" : incomingAnswer.SectionName.Trim(),
                QuestionText = incomingAnswer.QuestionText.Trim(),
                AnswerText = incomingAnswer.AnswerText,
                AnswerType = string.IsNullOrWhiteSpace(incomingAnswer.AnswerType) ? "Text" : incomingAnswer.AnswerType.Trim(),
                CompletenessStatus = string.IsNullOrWhiteSpace(incomingAnswer.AnswerText) ? CompletenessStatus.Missing : CompletenessStatus.Complete,
                CreatedAt = DateTime.UtcNow
            });
        }

        response.AnswerCount = response.Answers.Count;
        assessment.FormStatus = response.AnswerCount > 0 ? ReadinessFormStatus.Completed : assessment.FormStatus;
        assessment.CompletedAt = response.AnswerCount > 0 ? receivedAt : assessment.CompletedAt;
        assessment.ResponseReceivedAt = receivedAt;
        assessment.ExternalResponseId = externalResponseId;
        assessment.RawResponseJson = rawResponseJson;
        assessment.Summary = $"{response.ResponseLabel}: received {response.AnswerCount} answers from Google Form.";
        assessment.LastModifiedAt = DateTime.UtcNow;

        if (assessment.ClientCompany is not null)
        {
            if (response.AnswerCount > 0)
            {
                assessment.ClientCompany.CurrentStage = ClientStage.AssessmentCompleted;
                assessment.ClientCompany.NextAction = "Review received assessment response and generate knowledge gap analysis";
            }
            else
            {
                assessment.ClientCompany.NextAction = "Review received assessment response";
            }
            assessment.ClientCompany.LastModifiedAt = DateTime.UtcNow;
        }

        if (response.AnswerCount > 0)
        {
            await MarkWorkflowAsync(assessment.ClientCompanyId, "Assessment Completed");
        }

        context.ClientActivityLogs.Add(new ClientActivityLog
        {
            ClientCompanyId = assessment.ClientCompanyId,
            ActivityType = "Assessment response received",
            Description = $"Google Form response received: {response.ResponseLabel} with {response.AnswerCount} answers.",
            CreatedBy = "Google Forms webhook",
            CreatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();
        return Ok(new
        {
            success = true,
            assessmentId = assessment.Id,
            responseId = response.Id,
            responseLabel = response.ResponseLabel,
            answers = response.AnswerCount
        });
    }

    private async Task<ReadinessFormSettings?> GetActiveSettingsAsync(CancellationToken cancellationToken)
    {
        return await context.ReadinessFormSettings
            .Where(item => item.IsActive)
            .OrderByDescending(item => item.LastModifiedAt ?? item.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<AssessmentResponse?> FindDuplicateResponseAsync(int assessmentId, string? externalResponseId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(externalResponseId))
        {
            return null;
        }

        return await context.AssessmentResponses
            .Where(response =>
                response.ReadinessAssessmentId == assessmentId &&
                response.ExternalResponseId == externalResponseId)
            .OrderByDescending(response => response.ReceivedAt)
            .ThenByDescending(response => response.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static AssessmentResponse CreateAssessmentResponse(
        ReadinessAssessment assessment,
        AssessmentResponseSource source,
        AssessmentResponseStatus status,
        string? externalResponseId,
        string? clientToken,
        DateTime? submittedAt,
        DateTime receivedAt,
        string? rawResponseJson)
    {
        var responseNumber = assessment.Responses
            .Select(response => response.ResponseNumber)
            .DefaultIfEmpty()
            .Max() + 1;
        var response = new AssessmentResponse
        {
            ReadinessAssessmentId = assessment.Id,
            ResponseNumber = responseNumber,
            ResponseLabel = GetResponseLabel(responseNumber),
            Source = source,
            Status = status,
            ExternalResponseId = externalResponseId,
            ClientToken = clientToken,
            SubmittedAt = submittedAt,
            ReceivedAt = receivedAt,
            RawResponseJson = rawResponseJson,
            CreatedAt = receivedAt
        };

        assessment.Responses.Add(response);
        return response;
    }

    private static string GetResponseLabel(int responseNumber)
    {
        return responseNumber switch
        {
            1 => "First response",
            2 => "Second response",
            3 => "Third response",
            _ => $"Response {responseNumber}"
        };
    }

    private async Task MarkWorkflowAsync(int clientId, string stageName, CancellationToken cancellationToken = default)
    {
        var step = await context.ClientWorkflowSteps
            .Where(item => item.ClientCompanyId == clientId && item.StageName == stageName)
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (step is null)
        {
            context.ClientWorkflowSteps.Add(new ClientWorkflowStep
            {
                ClientCompanyId = clientId,
                StageName = stageName,
                DisplayOrder = StakeholderWorkflow.GetDisplayOrder(stageName),
                Status = WorkflowStepStatus.Completed,
                CompletedAt = DateTime.UtcNow
            });
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
