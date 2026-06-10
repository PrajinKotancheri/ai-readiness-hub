using AI_Readiness_Hub.Models;
using System.Text.Json;

namespace AI_Readiness_Hub.Services;

public record AIProviderRequest(
    string OperationName,
    string SystemInstruction,
    string UserPrompt,
    string ContextText,
    string SchemaName,
    JsonElement JsonSchema,
    string? CurrentDraft = null,
    string? ConsultantFeedback = null);

public record AIProviderResult(
    bool Succeeded,
    string? Content,
    AIProviderKind Provider,
    string Model,
    string? FriendlyMessage = null,
    string? DiagnosticMessage = null);

public record AIContextRequest(
    int ClientId,
    string OperationName,
    string? CurrentDraft = null,
    string? ConsultantFeedback = null,
    string? PreviousMessages = null);

public record AIContextPackage(
    string OperationName,
    string PromptText,
    string ContextText,
    IReadOnlyDictionary<string, string> Variables,
    IReadOnlyList<AIContextSource> Sources,
    IReadOnlyList<string> Warnings);

public record AIContextSource(
    AIOutputSourceType SourceType,
    AIOutputSourceCategory SourceCategory,
    string SourceLabel,
    string? SourceReference,
    string? SourceUrl,
    string? EvidenceText);

public record ParsedKnowledgeGapItem(
    KnowledgeGapArea GapArea,
    string MissingInformation,
    string? WhyItMatters,
    string? FollowUpQuestion,
    string? SuggestedEvidence,
    KnowledgeGapPriority Priority,
    IReadOnlyList<AIContextSource> Sources);

public record ParsedCompanySummary(
    string Summary,
    string? BusinessModel,
    IReadOnlyList<string> StrategicGoals,
    string? OperationalContext,
    string? AIReadinessImplications,
    IReadOnlyList<AIContextSource> Sources);

public record ParsedRefinement(
    string ImprovedDraft,
    string? SummaryOfChanges,
    IReadOnlyList<AIContextSource> Sources);
