using System.Text.Json;
using AI_Readiness_Hub.Data;
using AI_Readiness_Hub.Models;
using AI_Readiness_Hub.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AI_Readiness_Hub.Controllers;

[Route("Assessments")]
public class AssessmentsController(ApplicationDbContext context, IReadinessFormService readinessFormService) : Controller
{
    [HttpPost("MarkFormSent")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkFormSent(int clientId, string? formUrl)
    {
        var client = await context.ClientCompanies
            .Include(item => item.ReadinessAssessments)
            .FirstOrDefaultAsync(item => item.Id == clientId);
        if (client is null)
        {
            return NotFound();
        }

        var assessment = client.ReadinessAssessments
            .OrderByDescending(item => item.CreatedAt)
            .FirstOrDefault(item => item.FormStatus is not ReadinessFormStatus.Completed and not ReadinessFormStatus.Imported)
            ?? new ReadinessAssessment { ClientCompanyId = clientId, CreatedAt = DateTime.UtcNow };

        if (assessment.Id == 0)
        {
            context.ReadinessAssessments.Add(assessment);
        }

        assessment.FormStatus = ReadinessFormStatus.Sent;
        assessment.FormUrl = formUrl;
        assessment.SentAt = DateTime.UtcNow;
        assessment.LastModifiedAt = DateTime.UtcNow;
        client.CurrentStage = ClientStage.AssessmentSent;
        client.NextAction = "Wait for form completion or import answers";
        client.LastModifiedAt = DateTime.UtcNow;

        await MarkWorkflowAsync(clientId, "Readiness Form Sent", WorkflowStepStatus.Completed);
        await LogAsync(clientId, "Form marked as sent", "Readiness assessment form marked as sent.");
        await context.SaveChangesAsync();
        return RedirectToWorkspace(clientId);
    }

    [HttpPost("GenerateLink")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateLink(int clientId, string? customFormUrl)
    {
        try
        {
            await readinessFormService.EnsureGeneratedFormLinkAsync(clientId, customFormUrl);
            TempData["SuccessMessage"] = "Unique Google Form link generated.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToWorkspace(clientId);
    }

    [HttpPost("UpdateCustomFormLink")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateCustomFormLink(int clientId, string? customFormUrl)
    {
        try
        {
            await readinessFormService.EnsureGeneratedFormLinkAsync(clientId, customFormUrl);
            TempData["SuccessMessage"] = "Custom form link saved and unique link regenerated.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToWorkspace(clientId);
    }

    [HttpPost("SendReadinessForm")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendReadinessForm(int clientId)
    {
        try
        {
            var assessment = await readinessFormService.SendReadinessFormAsync(clientId);
            TempData["SuccessMessage"] = $"Readiness form sent to {assessment.SentToEmail}.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToWorkspace(clientId);
    }

    [HttpPost("SendReminder")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendReminder(int clientId)
    {
        try
        {
            var assessment = await readinessFormService.SendReminderAsync(clientId);
            TempData["SuccessMessage"] = $"Readiness form reminder sent to {assessment.SentToEmail}.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToWorkspace(clientId);
    }

    [HttpPost("Import")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Import(int clientId, string rawResponseText)
    {
        var client = await context.ClientCompanies
            .Include(item => item.ReadinessAssessments)
                .ThenInclude(item => item.Answers)
            .Include(item => item.ReadinessAssessments)
                .ThenInclude(item => item.Responses)
            .FirstOrDefaultAsync(item => item.Id == clientId);
        if (client is null)
        {
            return NotFound();
        }

        var assessment = client.ReadinessAssessments
            .OrderByDescending(item => item.CreatedAt)
            .FirstOrDefault()
            ?? new ReadinessAssessment { ClientCompanyId = clientId, CreatedAt = DateTime.UtcNow };

        if (assessment.Id == 0)
        {
            context.ReadinessAssessments.Add(assessment);
        }

        var importedAt = DateTime.UtcNow;
        var parsedAnswers = ParseAnswers(rawResponseText);
        var response = CreateAssessmentResponse(
            assessment,
            AssessmentResponseSource.ManualImport,
            AssessmentResponseStatus.Imported,
            externalResponseId: null,
            assessment.ClientToken,
            submittedAt: null,
            importedAt,
            rawResponseText);

        foreach (var answer in parsedAnswers)
        {
            answer.ReadinessAssessment = assessment;
            if (assessment.Id > 0)
            {
                answer.ReadinessAssessmentId = assessment.Id;
            }
            response.Answers.Add(answer);
        }

        response.AnswerCount = response.Answers.Count;
        assessment.FormStatus = response.AnswerCount > 0 ? ReadinessFormStatus.Imported : assessment.FormStatus;
        assessment.ImportedAt = importedAt;
        assessment.CompletedAt = response.AnswerCount > 0 ? importedAt : assessment.CompletedAt;
        assessment.ResponseReceivedAt = importedAt;
        assessment.RawResponseJson = rawResponseText;
        assessment.Summary = $"{response.ResponseLabel}: manually imported {response.AnswerCount} answer rows. Consultant should review completeness.";
        assessment.LastModifiedAt = importedAt;

        if (response.AnswerCount > 0)
        {
            client.CurrentStage = ClientStage.AssessmentCompleted;
            client.NextAction = "Review imported answers and generate gap analysis";
        }
        else
        {
            client.NextAction = "Review manual import response";
        }
        client.LastModifiedAt = importedAt;

        if (response.AnswerCount > 0)
        {
            await MarkWorkflowAsync(clientId, "Form Completed", WorkflowStepStatus.Completed);
        }
        await LogAsync(clientId, "Assessment imported", $"Manual import created: {response.ResponseLabel} with {response.AnswerCount} answers.");
        await context.SaveChangesAsync();
        return RedirectToWorkspace(clientId);
    }

    [HttpPost("Answer")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddAnswer(int clientId, string sectionName, string questionText, string? answerText, string? answerType, bool isMandatory)
    {
        var assessment = await context.ReadinessAssessments
            .Include(item => item.Responses)
            .Where(item => item.ClientCompanyId == clientId)
            .OrderByDescending(item => item.CreatedAt)
            .ThenByDescending(item => item.Id)
            .FirstOrDefaultAsync();

        if (assessment is null)
        {
            assessment = new ReadinessAssessment
            {
                ClientCompanyId = clientId,
                FormStatus = ReadinessFormStatus.Imported,
                ImportedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };
            context.ReadinessAssessments.Add(assessment);
        }

        var receivedAt = DateTime.UtcNow;
        var response = CreateAssessmentResponse(
            assessment,
            AssessmentResponseSource.ManualImport,
            AssessmentResponseStatus.Imported,
            externalResponseId: null,
            assessment.ClientToken,
            submittedAt: null,
            receivedAt,
            JsonSerializer.Serialize(new { sectionName, questionText, answerText, answerType, isMandatory }));

        response.Answers.Add(new AssessmentAnswer
        {
            ReadinessAssessment = assessment,
            ReadinessAssessmentId = assessment.Id > 0 ? assessment.Id : 0,
            SectionName = sectionName,
            QuestionText = questionText,
            AnswerText = answerText,
            AnswerType = answerType,
            IsMandatory = isMandatory,
            CompletenessStatus = string.IsNullOrWhiteSpace(answerText) ? CompletenessStatus.Missing : CompletenessStatus.Complete,
            CreatedAt = receivedAt
        });
        response.AnswerCount = response.Answers.Count;
        assessment.FormStatus = ReadinessFormStatus.Imported;
        assessment.ImportedAt = receivedAt;
        assessment.CompletedAt ??= receivedAt;
        assessment.ResponseReceivedAt = receivedAt;
        assessment.LastModifiedAt = receivedAt;

        await MarkWorkflowAsync(clientId, "Form Completed", WorkflowStepStatus.Completed);
        await LogAsync(clientId, "Assessment answer added", $"Manual import created: {response.ResponseLabel} with 1 answer.");
        await context.SaveChangesAsync();
        return RedirectToWorkspace(clientId);
    }

    private static List<AssessmentAnswer> ParseAnswers(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        var answers = TryParseJsonAnswers(raw);
        if (answers.Count > 0)
        {
            return answers;
        }

        return raw.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select((line, index) =>
            {
                var parts = line.Split('|', StringSplitOptions.TrimEntries);
                if (parts.Length >= 3)
                {
                    return CreateAnswer(parts[0], parts[1], parts[2]);
                }

                var colonParts = line.Split(':', 2, StringSplitOptions.TrimEntries);
                return colonParts.Length == 2
                    ? CreateAnswer("Imported", colonParts[0], colonParts[1])
                    : CreateAnswer("Imported", $"Imported answer {index + 1}", line);
            })
            .ToList();
    }

    private static List<AssessmentAnswer> TryParseJsonAnswers(string raw)
    {
        try
        {
            using var document = JsonDocument.Parse(raw);
            var root = document.RootElement;
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("answers", out var answerArray))
            {
                root = answerArray;
            }

            if (root.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            return root.EnumerateArray()
                .Where(item => item.ValueKind == JsonValueKind.Object)
                .Select(item => CreateAnswer(
                    GetJsonString(item, "section") ?? GetJsonString(item, "sectionName") ?? "Imported",
                    GetJsonString(item, "question") ?? GetJsonString(item, "questionText") ?? "Imported question",
                    GetJsonString(item, "answer") ?? GetJsonString(item, "answerText") ?? string.Empty,
                    GetJsonString(item, "answerType")))
                .ToList();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string? GetJsonString(JsonElement item, string name)
    {
        return item.TryGetProperty(name, out var property) && property.ValueKind is JsonValueKind.String or JsonValueKind.Number
            ? property.ToString()
            : null;
    }

    private static AssessmentAnswer CreateAnswer(string section, string question, string? answer, string? answerType = "Text")
    {
        return new AssessmentAnswer
        {
            SectionName = section,
            QuestionText = question,
            AnswerText = answer,
            AnswerType = answerType,
            IsMandatory = false,
            CompletenessStatus = string.IsNullOrWhiteSpace(answer) ? CompletenessStatus.Missing : CompletenessStatus.Complete,
            CreatedAt = DateTime.UtcNow
        };
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
            ReadinessAssessment = assessment,
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
