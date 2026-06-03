using System.Text.Json;
using AI_Readiness_Hub.Data;
using AI_Readiness_Hub.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AI_Readiness_Hub.Controllers;

[Route("Assessments")]
public class AssessmentsController(ApplicationDbContext context) : Controller
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

    [HttpPost("Import")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Import(int clientId, string rawResponseText)
    {
        var client = await context.ClientCompanies
            .Include(item => item.ReadinessAssessments)
                .ThenInclude(item => item.Answers)
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

        assessment.FormStatus = ReadinessFormStatus.Imported;
        assessment.ImportedAt = DateTime.UtcNow;
        assessment.CompletedAt ??= DateTime.UtcNow;
        assessment.RawResponseJson = rawResponseText;
        assessment.Summary = $"Imported {ParseAnswers(rawResponseText).Count} answer rows. Consultant should review completeness.";
        assessment.LastModifiedAt = DateTime.UtcNow;

        foreach (var answer in ParseAnswers(rawResponseText))
        {
            assessment.Answers.Add(answer);
        }

        client.CurrentStage = ClientStage.AssessmentCompleted;
        client.NextAction = "Review imported answers and generate gap analysis";
        client.LastModifiedAt = DateTime.UtcNow;

        await MarkWorkflowAsync(clientId, "Form Completed", WorkflowStepStatus.Completed);
        await LogAsync(clientId, "Assessment imported", "Assessment answers imported from pasted text or JSON.");
        await context.SaveChangesAsync();
        return RedirectToWorkspace(clientId);
    }

    [HttpPost("Answer")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddAnswer(int clientId, string sectionName, string questionText, string? answerText, string? answerType, bool isMandatory)
    {
        var assessment = await context.ReadinessAssessments
            .Where(item => item.ClientCompanyId == clientId)
            .OrderByDescending(item => item.CreatedAt)
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

        assessment.Answers.Add(new AssessmentAnswer
        {
            SectionName = sectionName,
            QuestionText = questionText,
            AnswerText = answerText,
            AnswerType = answerType,
            IsMandatory = isMandatory,
            CompletenessStatus = string.IsNullOrWhiteSpace(answerText) ? CompletenessStatus.Missing : CompletenessStatus.Complete,
            CreatedAt = DateTime.UtcNow
        });
        assessment.FormStatus = ReadinessFormStatus.Imported;
        assessment.LastModifiedAt = DateTime.UtcNow;

        await LogAsync(clientId, "Assessment answer added", $"Answer added to {sectionName}.");
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
