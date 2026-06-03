using AI_Readiness_Hub.Models;

namespace AI_Readiness_Hub.ViewModels;

public class ClientWorkspaceViewModel
{
    public ClientCompany Client { get; set; } = new();
    public ReadinessAssessment? LatestAssessment { get; set; }
    public ReadinessFormSettings? ReadinessFormSettings { get; set; }
    public ClientReport? LatestReport { get; set; }
    public ReadinessScore? LatestScore { get; set; }
    public IReadOnlyList<IGrouping<string, AssessmentAnswer>> AnswersBySection { get; set; } = [];
    public IReadOnlyList<IGrouping<SwotCategory, SwotAnalysisItem>> SwotByCategory { get; set; } = [];
    public IReadOnlyList<IGrouping<RoadmapPhase, AIRoadmapItem>> RoadmapByPhase { get; set; } = [];
    public IReadOnlyList<AIUseCase> RankedUseCases { get; set; } = [];
    public IReadOnlyList<AIAnalysisOutput> LatestAnalysisOutputs { get; set; } = [];
}
