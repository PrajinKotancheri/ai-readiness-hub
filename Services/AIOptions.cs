using AI_Readiness_Hub.Models;

namespace AI_Readiness_Hub.Services;

public class AIOptions
{
    public string Provider { get; set; } = "Mock";
    public string Model { get; set; } = "gpt-5-nano";
    public double Temperature { get; set; } = 0.2;
    public int MaxOutputTokens { get; set; } = 4096;
    public int MaxInputCharactersPerRequest { get; set; } = 30000;
    public bool EnableExternalResearch { get; set; }
    public bool RequireApprovalBeforeChaining { get; set; }

    public AIProviderKind ProviderKind =>
        Provider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase)
            ? AIProviderKind.OpenAI
            : AIProviderKind.Mock;

    public string DisplayProvider => ProviderKind == AIProviderKind.OpenAI ? "OpenAI" : "Mock";
}

public static class AIOperationNames
{
    public const string KnowledgeGapAnalysis = "Knowledge Gap Analysis";
    public const string CompanySummary = "Company Summary";
    public const string ReadinessScore = "Readiness Score";
    public const string IndustryAnalysis = "Industry Analysis";
    public const string CompetitorAnalysis = "Competitor Analysis";
    public const string SwotAnalysis = "SWOT Analysis";
    public const string UseCaseIdentification = "Use Case Identification";
    public const string UseCaseScoring = "Use Case Scoring";
    public const string RoadmapGeneration = "Roadmap Generation";
    public const string StrategicReportGeneration = "Strategic Report Generation";
    public const string AIWorkspaceRefinement = "AI Workspace Refinement";
}
