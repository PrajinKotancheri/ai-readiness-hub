using AI_Readiness_Hub.Data;
using AI_Readiness_Hub.Models;
using Microsoft.EntityFrameworkCore;

namespace AI_Readiness_Hub.Services;

public class RuleBasedKnowledgeGapAnalysisService(
    ApplicationDbContext context,
    ILogger<RuleBasedKnowledgeGapAnalysisService> logger) : IKnowledgeGapAnalysisService
{
    public async Task<int> GenerateAsync(int clientId)
    {
        var client = await context.ClientCompanies
            .Where(item => item.Id == clientId)
            .Select(item => new
            {
                item.Id,
                item.CompanyName,
                item.Industry,
                item.WebsiteUrl,
                item.BusinessModel,
                item.CompanySizeRange,
                item.RevenueRange,
                item.ContactPersonName,
                item.CurrentStage
            })
            .FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("Client was not found.");

        var latestResponse = await context.AssessmentResponses
            .AsNoTracking()
            .Where(response =>
                response.ReadinessAssessment!.ClientCompanyId == clientId &&
                response.Status != AssessmentResponseStatus.Ignored)
            .OrderByDescending(response => response.ReceivedAt)
            .ThenByDescending(response => response.ResponseNumber)
            .ThenByDescending(response => response.Id)
            .Select(response => new
            {
                response.Id,
                response.ResponseLabel,
                response.AnswerCount
            })
            .FirstOrDefaultAsync();

        List<AnswerEvidence> answers = latestResponse is null
            ? []
            : await context.AssessmentAnswers
                .AsNoTracking()
                .Where(answer => answer.AssessmentResponseId == latestResponse.Id)
                .OrderBy(answer => answer.SectionName)
                .ThenBy(answer => answer.Id)
                .Select(answer => new AnswerEvidence(
                    answer.SectionName,
                    answer.QuestionText,
                    answer.AnswerText,
                    answer.CompletenessStatus))
                .ToListAsync();

        var existingKeys = await context.KnowledgeGapItems
            .AsNoTracking()
            .Where(item => item.ClientCompanyId == clientId && item.Status != KnowledgeGapStatus.NotRelevant)
            .Select(item => item.MissingInformation)
            .ToListAsync();
        var existingSet = existingKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var candidates = new List<KnowledgeGapItem>();
        AddClientProfileGap(candidates, client.Id, latestResponse?.Id, client.BusinessModel, KnowledgeGapArea.BusinessModel,
            "Business model and revenue logic need clarification.",
            "Strategic AI use cases depend on how the company creates value and where margins or service pressure exist.",
            "How does the company make money, and which parts of the model are most constrained today?",
            "Business model summary, revenue split, service model notes.");
        AddClientProfileGap(candidates, client.Id, latestResponse?.Id, client.CompanySizeRange, KnowledgeGapArea.OperationsProcess,
            "Company scale and operating complexity are not clear enough.",
            "Roadmap sizing, governance needs, and change effort depend on scale.",
            "How many teams, locations, and users would be affected by the first AI pilots?",
            "Org overview, team counts, process ownership map.");
        AddClientProfileGap(candidates, client.Id, latestResponse?.Id, client.WebsiteUrl, KnowledgeGapArea.StrategyVision,
            "Public positioning and current market story are not captured.",
            "Industry and competitor analysis needs a baseline understanding of the client positioning.",
            "Which customer segments, offerings, and differentiators matter most?",
            "Website, sales deck, positioning document, or strategy brief.");

        foreach (var answer in answers)
        {
            if (IsMissing(answer.AnswerText, answer.CompletenessStatus))
            {
                candidates.Add(CreateAnswerGap(
                    client.Id,
                    latestResponse?.Id,
                    MapGapArea(answer.SectionName, answer.QuestionText),
                    $"Missing answer: {answer.QuestionText}",
                    $"The {answer.SectionName} section lacks enough detail for reliable later-stage analysis.",
                    $"Can you clarify: {answer.QuestionText}",
                    "Ask client to answer the missing questionnaire item or provide a relevant document.",
                    KnowledgeGapPriority.High));
                continue;
            }

            if ((answer.AnswerText?.Trim().Length ?? 0) < 45)
            {
                candidates.Add(CreateAnswerGap(
                    client.Id,
                    latestResponse?.Id,
                    MapGapArea(answer.SectionName, answer.QuestionText),
                    $"Vague answer needs detail: {answer.QuestionText}",
                    "Short answers may hide important constraints, owners, systems, or success metrics.",
                    $"What concrete examples, owners, metrics, or source systems support this answer: {answer.QuestionText}",
                    "Follow-up explanation, process map, owner list, screenshots, or sample reports.",
                    KnowledgeGapPriority.Medium));
            }
        }

        var hasEvidence = await context.ClientDocuments.AsNoTracking().AnyAsync(item => item.ClientCompanyId == clientId) ||
            await context.ConsultantNotes.AsNoTracking().AnyAsync(item => item.ClientCompanyId == clientId) ||
            await context.MeetingTranscripts.AsNoTracking().AnyAsync(item => item.ClientCompanyId == clientId);
        if (!hasEvidence)
        {
            candidates.Add(new KnowledgeGapItem
            {
                ClientCompanyId = client.Id,
                AssessmentResponseId = latestResponse?.Id,
                GapArea = KnowledgeGapArea.OperationsProcess,
                MissingInformation = "No supporting notes, documents, or transcripts are attached yet.",
                WhyItMatters = "The consultant should validate questionnaire claims before generating final recommendations.",
                FollowUpQuestion = "Which documents, process maps, reports, or discovery notes can support the assessment answers?",
                SuggestedEvidence = "Discovery meeting transcript, process map, CRM/reporting screenshots, policy documents.",
                Priority = KnowledgeGapPriority.High,
                Status = KnowledgeGapStatus.Open,
                CreatedAt = DateTime.UtcNow
            });
        }

        var newItems = candidates
            .Where(item => !existingSet.Contains(item.MissingInformation))
            .Take(12)
            .ToList();

        context.KnowledgeGapItems.AddRange(newItems);
        foreach (var item in newItems)
        {
            context.AIOutputSources.Add(new AIOutputSource
            {
                ClientCompanyId = client.Id,
                OutputType = AIOutputType.KnowledgeGap,
                SourceType = AIOutputSourceType.Internal,
                SourceCategory = latestResponse is null ? AIOutputSourceCategory.Other : AIOutputSourceCategory.AssessmentResponse,
                SourceLabel = latestResponse is null ? "Client profile" : $"Assessment Response: {latestResponse.ResponseLabel}",
                SourceReference = item.GapArea.ToString(),
                EvidenceText = item.MissingInformation,
                CreatedAt = DateTime.UtcNow
            });
        }

        var clientForUpdate = await context.ClientCompanies.FindAsync(clientId)
            ?? throw new InvalidOperationException("Client was not found.");
        clientForUpdate.CurrentStage = ClientStage.KnowledgeGapAnalysis;
        clientForUpdate.NextAction = "Review knowledge gaps and prepare discovery agenda";
        clientForUpdate.LastModifiedAt = DateTime.UtcNow;
        clientForUpdate.LastModifiedBy = "System";

        await MarkWorkflowAsync(clientId, "Assessment Completed", WorkflowStepStatus.Completed);
        await MarkWorkflowAsync(clientId, "Knowledge Gap Analysis", WorkflowStepStatus.InProgress);

        context.ClientActivityLogs.Add(new ClientActivityLog
        {
            ClientCompanyId = clientId,
            ActivityType = "Knowledge Gap Analysis generated",
            Description = $"Generated {newItems.Count} missing-understanding item{(newItems.Count == 1 ? string.Empty : "s")}.",
            CreatedBy = "System",
            CreatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();
        logger.LogInformation(
            "Knowledge gap analysis generated. ClientCompanyId: {ClientCompanyId}; CreatedItems: {CreatedItems}; LatestResponseId: {LatestResponseId}",
            clientId,
            newItems.Count,
            latestResponse?.Id);
        return newItems.Count;
    }

    private static void AddClientProfileGap(
        List<KnowledgeGapItem> candidates,
        int clientId,
        int? responseId,
        string? value,
        KnowledgeGapArea area,
        string missingInformation,
        string whyItMatters,
        string followUpQuestion,
        string suggestedEvidence)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        candidates.Add(new KnowledgeGapItem
        {
            ClientCompanyId = clientId,
            AssessmentResponseId = responseId,
            GapArea = area,
            MissingInformation = missingInformation,
            WhyItMatters = whyItMatters,
            FollowUpQuestion = followUpQuestion,
            SuggestedEvidence = suggestedEvidence,
            Priority = KnowledgeGapPriority.Medium,
            Status = KnowledgeGapStatus.Open,
            CreatedAt = DateTime.UtcNow
        });
    }

    private static KnowledgeGapItem CreateAnswerGap(
        int clientId,
        int? responseId,
        KnowledgeGapArea area,
        string missingInformation,
        string whyItMatters,
        string followUpQuestion,
        string suggestedEvidence,
        KnowledgeGapPriority priority)
    {
        return new KnowledgeGapItem
        {
            ClientCompanyId = clientId,
            AssessmentResponseId = responseId,
            GapArea = area,
            MissingInformation = missingInformation,
            WhyItMatters = whyItMatters,
            FollowUpQuestion = followUpQuestion,
            SuggestedEvidence = suggestedEvidence,
            Priority = priority,
            Status = KnowledgeGapStatus.Open,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static KnowledgeGapArea MapGapArea(string section, string question)
    {
        var text = $"{section} {question}".ToLowerInvariant();
        if (text.Contains("sales", StringComparison.Ordinal) || text.Contains("qualification", StringComparison.Ordinal))
        {
            return KnowledgeGapArea.SalesQualification;
        }

        if (text.Contains("customer", StringComparison.Ordinal) || text.Contains("onboarding", StringComparison.Ordinal))
        {
            return KnowledgeGapArea.CustomerOnboarding;
        }

        if (text.Contains("data", StringComparison.Ordinal) || text.Contains("ownership", StringComparison.Ordinal))
        {
            return KnowledgeGapArea.DataOwnership;
        }

        if (text.Contains("system", StringComparison.Ordinal) || text.Contains("integration", StringComparison.Ordinal) || text.Contains("crm", StringComparison.Ordinal))
        {
            return KnowledgeGapArea.SystemsIntegration;
        }

        if (text.Contains("report", StringComparison.Ordinal) || text.Contains("metric", StringComparison.Ordinal) || text.Contains("measure", StringComparison.Ordinal))
        {
            return KnowledgeGapArea.ReportingMeasurement;
        }

        if (text.Contains("governance", StringComparison.Ordinal) || text.Contains("compliance", StringComparison.Ordinal) || text.Contains("risk", StringComparison.Ordinal))
        {
            return KnowledgeGapArea.GovernanceCompliance;
        }

        if (text.Contains("strategy", StringComparison.Ordinal) || text.Contains("vision", StringComparison.Ordinal) || text.Contains("goal", StringComparison.Ordinal))
        {
            return KnowledgeGapArea.StrategyVision;
        }

        if (text.Contains("process", StringComparison.Ordinal) || text.Contains("workflow", StringComparison.Ordinal) || text.Contains("operation", StringComparison.Ordinal))
        {
            return KnowledgeGapArea.OperationsProcess;
        }

        return KnowledgeGapArea.Other;
    }

    private static bool IsMissing(string? answerText, CompletenessStatus status)
    {
        return string.IsNullOrWhiteSpace(answerText) ||
            status == CompletenessStatus.Missing;
    }

    private async Task MarkWorkflowAsync(int clientId, string stageName, WorkflowStepStatus status)
    {
        var step = await context.ClientWorkflowSteps
            .Where(item => item.ClientCompanyId == clientId && item.StageName == stageName)
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.Id)
            .FirstOrDefaultAsync();
        if (step is null)
        {
            context.ClientWorkflowSteps.Add(new ClientWorkflowStep
            {
                ClientCompanyId = clientId,
                StageName = stageName,
                DisplayOrder = StakeholderWorkflow.GetDisplayOrder(stageName),
                Status = status,
                CompletedAt = status == WorkflowStepStatus.Completed ? DateTime.UtcNow : null
            });
            return;
        }

        step.Status = status;
        step.CompletedAt = status == WorkflowStepStatus.Completed ? DateTime.UtcNow : step.CompletedAt;
    }

    private sealed record AnswerEvidence(
        string SectionName,
        string QuestionText,
        string? AnswerText,
        CompletenessStatus CompletenessStatus);
}
