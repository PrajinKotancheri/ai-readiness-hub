using System.Text.Json;
using AI_Readiness_Hub.Data;
using AI_Readiness_Hub.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace AI_Readiness_Hub.Services;

public class AIKnowledgeGapAnalysisService(
    ApplicationDbContext context,
    IAIContextBuilder contextBuilder,
    IAIProviderClient providerClient,
    IStructuredAIResponseParser parser,
    ILogger<AIKnowledgeGapAnalysisService> logger,
    IWebHostEnvironment environment) : IKnowledgeGapAnalysisService
{
    public async Task<int> GenerateAsync(int clientId)
    {
        var aiContext = await contextBuilder.BuildAsync(new AIContextRequest(clientId, AIOperationNames.KnowledgeGapAnalysis));
        var request = BuildKnowledgeGapRequest(aiContext);

        var result = await providerClient.GenerateStructuredJsonAsync(request);
        ThrowIfGenerationFailed(result);

        IReadOnlyList<ParsedKnowledgeGapItem> parsedItems;
        if (!TryParseKnowledgeGaps(clientId, result, attempt: 1, out parsedItems))
        {
            logger.LogInformation(
                "Retrying knowledge gap AI generation after parse failure. ClientCompanyId: {ClientCompanyId}; Provider: {Provider}; Model: {Model}",
                clientId,
                result.Provider,
                result.Model);

            result = await providerClient.GenerateStructuredJsonAsync(BuildKnowledgeGapRepairRequest(aiContext));
            ThrowIfGenerationFailed(result);
            if (!TryParseKnowledgeGaps(clientId, result, attempt: 2, out parsedItems))
            {
                throw new InvalidOperationException("AI returned a response that could not be parsed after retry. No knowledge gaps were saved.");
            }
        }

        var client = await context.ClientCompanies.FirstOrDefaultAsync(item => item.Id == clientId)
            ?? throw new InvalidOperationException("Client was not found.");

        var latestResponseId = await context.AssessmentResponses
            .AsNoTracking()
            .Where(response => response.ReadinessAssessment!.ClientCompanyId == clientId && response.Status != AssessmentResponseStatus.Ignored)
            .OrderByDescending(response => response.ReceivedAt)
            .ThenByDescending(response => response.ResponseNumber)
            .ThenByDescending(response => response.Id)
            .Select(response => (int?)response.Id)
            .FirstOrDefaultAsync();

        var existing = await context.KnowledgeGapItems
            .AsNoTracking()
            .Where(item => item.ClientCompanyId == clientId && item.Status != KnowledgeGapStatus.NotRelevant)
            .Select(item => item.MissingInformation)
            .ToListAsync();
        var existingSet = existing.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var newItems = parsedItems
            .Where(item => !existingSet.Contains(item.MissingInformation))
            .Select(item => new KnowledgeGapItem
            {
                ClientCompanyId = clientId,
                AssessmentResponseId = latestResponseId,
                GapArea = item.GapArea,
                MissingInformation = item.MissingInformation,
                WhyItMatters = item.WhyItMatters,
                FollowUpQuestion = item.FollowUpQuestion,
                SuggestedEvidence = item.SuggestedEvidence,
                Priority = item.Priority,
                Status = KnowledgeGapStatus.Open,
                CreatedAt = DateTime.UtcNow
            })
            .ToList();

        var outputVersion = await context.AIAnalysisOutputs
            .Where(output => output.ClientCompanyId == clientId && output.AnalysisType == AnalysisType.KnowledgeGapAnalysis)
            .Select(output => (int?)output.VersionNumber)
            .MaxAsync() ?? 0;
        var output = new AIAnalysisOutput
        {
            ClientCompanyId = clientId,
            AnalysisType = AnalysisType.KnowledgeGapAnalysis,
            Title = "Knowledge gap analysis draft",
            InputSummary = $"{aiContext.Sources.Count} compact source references; provider {result.Provider}.",
            OutputContent = JsonSerializer.Serialize(parsedItems, new JsonSerializerOptions { WriteIndented = true }),
            Status = DraftStatus.DraftGenerated,
            VersionNumber = outputVersion + 1,
            GeneratedAt = DateTime.UtcNow,
            GeneratedBy = result.Provider.ToString(),
            CreatedAt = DateTime.UtcNow
        };

        context.KnowledgeGapItems.AddRange(newItems);
        context.AIAnalysisOutputs.Add(output);
        foreach (var source in parsedItems.SelectMany(item => item.Sources).Concat(aiContext.Sources).Take(40))
        {
            context.AIOutputSources.Add(new AIOutputSource
            {
                ClientCompanyId = clientId,
                OutputType = AIOutputType.KnowledgeGap,
                SourceType = source.SourceType,
                SourceCategory = source.SourceCategory,
                SourceLabel = source.SourceLabel,
                SourceReference = source.SourceReference,
                SourceUrl = source.SourceUrl,
                EvidenceText = source.EvidenceText,
                CreatedAt = DateTime.UtcNow
            });
        }

        client.CurrentStage = ClientStage.KnowledgeGapAnalysis;
        client.NextAction = "Review knowledge gaps and prepare discovery agenda";
        client.LastModifiedAt = DateTime.UtcNow;
        client.LastModifiedBy = "System";

        await MarkWorkflowAsync(clientId, "Assessment Completed", WorkflowStepStatus.Completed);
        await MarkWorkflowAsync(clientId, "Knowledge Gap Analysis", WorkflowStepStatus.InProgress);

        context.ClientActivityLogs.Add(new ClientActivityLog
        {
            ClientCompanyId = clientId,
            ActivityType = "Knowledge Gap Analysis generated",
            Description = $"Knowledge Gap Analysis generated using {result.Provider}. Created {newItems.Count} new item{(newItems.Count == 1 ? string.Empty : "s")}.",
            CreatedBy = "System",
            CreatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();
        logger.LogInformation(
            "Knowledge gap analysis generated. ClientCompanyId: {ClientCompanyId}; Provider: {Provider}; Model: {Model}; CreatedItems: {CreatedItems}",
            clientId,
            result.Provider,
            result.Model,
            newItems.Count);
        return newItems.Count;
    }

    private static AIProviderRequest BuildKnowledgeGapRequest(AIContextPackage aiContext)
    {
        return new AIProviderRequest(
            AIOperationNames.KnowledgeGapAnalysis,
            "You are an AI-assisted consultant. Identify missing understanding only from the supplied context. Return valid JSON only.",
            aiContext.PromptText,
            aiContext.ContextText,
            AIJsonSchemas.GetSchemaName(AIOperationNames.KnowledgeGapAnalysis),
            AIJsonSchemas.GetSchema(AIOperationNames.KnowledgeGapAnalysis));
    }

    private static AIProviderRequest BuildKnowledgeGapRepairRequest(AIContextPackage aiContext)
    {
        return new AIProviderRequest(
            AIOperationNames.KnowledgeGapAnalysis,
            """
            You are an AI-assisted consultant. You returned invalid or incomplete JSON.
            Recreate the response from the original task.
            Return only complete valid JSON using the exact schema.
            Do not include markdown, code fences, commentary, trailing prose, or partial strings.
            If source evidence is unavailable for an item, use an empty sources array.
            """,
            $"""
            {aiContext.PromptText}

            Repair instruction: produce a complete JSON object with an items array. Each item must include gapArea, missingInformation, priority, and sources. Use null or an empty string only for optional text fields when necessary.
            """,
            aiContext.ContextText,
            AIJsonSchemas.GetSchemaName(AIOperationNames.KnowledgeGapAnalysis),
            AIJsonSchemas.GetSchema(AIOperationNames.KnowledgeGapAnalysis));
    }

    private static void ThrowIfGenerationFailed(AIProviderResult result)
    {
        if (!result.Succeeded || string.IsNullOrWhiteSpace(result.Content))
        {
            throw new InvalidOperationException(result.FriendlyMessage ?? "AI could not generate knowledge gaps. Please try again or switch to Mock provider.");
        }
    }

    private bool TryParseKnowledgeGaps(
        int clientId,
        AIProviderResult result,
        int attempt,
        out IReadOnlyList<ParsedKnowledgeGapItem> parsedItems)
    {
        parsedItems = [];
        try
        {
            parsedItems = parser.ParseKnowledgeGaps(result.Content!);
            return true;
        }
        catch (JsonException ex)
        {
            LogKnowledgeGapParseFailure(clientId, result, result.Content!, attempt, ex);
            return false;
        }
        catch (InvalidOperationException ex)
        {
            LogKnowledgeGapParseFailure(clientId, result, result.Content!, attempt, ex);
            return false;
        }
    }

    private void LogKnowledgeGapParseFailure(
        int clientId,
        AIProviderResult result,
        string content,
        int attempt,
        Exception exception)
    {
        logger.LogWarning(
            exception,
            "Knowledge gap AI response could not be parsed. ClientCompanyId: {ClientCompanyId}; Operation: {Operation}; Provider: {Provider}; Model: {Model}; Attempt: {Attempt}; TextLength: {TextLength}; JsonRoot: {JsonRoot}; ParserException: {ParserException}; BytePositionInLine: {BytePositionInLine}; AppearsTruncated: {AppearsTruncated}; ContentPreview: {ContentPreview}",
            clientId,
            AIOperationNames.KnowledgeGapAnalysis,
            result.Provider,
            result.Model,
            attempt,
            content.Length,
            GetJsonRootHint(content),
            exception.GetType().Name,
            exception is JsonException jsonException ? jsonException.BytePositionInLine : null,
            AppearsTruncated(content, exception),
            GetSafeContentPreview(content));
    }

    private string GetSafeContentPreview(string content)
    {
        if (!environment.IsDevelopment())
        {
            return "(suppressed outside Development)";
        }

        var normalized = content.ReplaceLineEndings(" ").Trim();
        return normalized.Length <= 320 ? normalized : normalized[..317] + "...";
    }

    private static string GetJsonRootHint(string content)
    {
        var trimmed = content.Trim();
        if (trimmed.Length == 0)
        {
            return "Empty";
        }

        return trimmed[0] switch
        {
            '{' when trimmed.EndsWith('}') => "Object",
            '{' => "IncompleteObject",
            '[' when trimmed.EndsWith(']') => "Array",
            '[' => "IncompleteArray",
            '`' => "CodeFenceOrMarkdown",
            _ => "Other"
        };
    }

    private static bool AppearsTruncated(string content, Exception exception)
    {
        var trimmed = content.Trim();
        if (trimmed.Length == 0)
        {
            return true;
        }

        return !trimmed.EndsWith('}') && !trimmed.EndsWith(']') ||
            HasUnclosedJsonString(trimmed) ||
            exception.Message.Contains("reached end of data", StringComparison.OrdinalIgnoreCase) ||
            exception.Message.Contains("Expected end of string", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasUnclosedJsonString(string value)
    {
        var inString = false;
        var escaped = false;
        foreach (var character in value)
        {
            if (escaped)
            {
                escaped = false;
                continue;
            }

            if (character == '\\' && inString)
            {
                escaped = true;
                continue;
            }

            if (character == '"')
            {
                inString = !inString;
            }
        }

        return inString;
    }

    private async Task MarkWorkflowAsync(int clientId, string stageName, WorkflowStepStatus status)
    {
        var normalizedStage = StakeholderWorkflow.MapLegacyStageName(stageName);
        var step = await context.ClientWorkflowSteps
            .FirstOrDefaultAsync(item => item.ClientCompanyId == clientId && item.StageName == normalizedStage);
        if (step is null)
        {
            step = new ClientWorkflowStep
            {
                ClientCompanyId = clientId,
                StageName = normalizedStage,
                DisplayOrder = StakeholderWorkflow.GetDisplayOrder(normalizedStage)
            };
            context.ClientWorkflowSteps.Add(step);
        }

        step.Status = status;
        if (status == WorkflowStepStatus.Completed)
        {
            step.CompletedAt = DateTime.UtcNow;
        }
    }
}
