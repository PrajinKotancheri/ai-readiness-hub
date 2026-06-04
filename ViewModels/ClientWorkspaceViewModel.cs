using AI_Readiness_Hub.Models;

namespace AI_Readiness_Hub.ViewModels;

public class ClientWorkspaceViewModel
{
    public ClientCompany Client { get; set; } = new();
    public ReadinessAssessment? LatestAssessment { get; set; }
    public ReadinessFormSettings? ReadinessFormSettings { get; set; }
    public ClientReport? LatestReport { get; set; }
    public ReadinessScore? LatestScore { get; set; }
    public int AssessmentResponseCount { get; set; }
    public int SelectedAnswerCount { get; set; }
    public int DocumentCount { get; set; }
    public int NoteCount { get; set; }
    public int TranscriptCount { get; set; }
    public int AiDraftCount { get; set; }
    public int GapCount { get; set; }
    public int OpenGapCount { get; set; }
    public int SwotCount { get; set; }
    public int UseCaseCount { get; set; }
    public int RoadmapCount { get; set; }
    public int ReportCount { get; set; }
    public int OpenTaskCount { get; set; }
    public int ActivityLogCount { get; set; }
    public IReadOnlyList<AssessmentResponse> AssessmentResponses { get; set; } = [];
    public AssessmentResponse? SelectedAssessmentResponse { get; set; }
    public IReadOnlyList<IGrouping<string, AssessmentAnswer>> SelectedAnswersBySection { get; set; } = [];
    public IReadOnlyList<IGrouping<SwotCategory, SwotAnalysisItem>> SwotByCategory { get; set; } = [];
    public IReadOnlyList<IGrouping<RoadmapPhase, AIRoadmapItem>> RoadmapByPhase { get; set; } = [];
    public IReadOnlyList<AIUseCase> RankedUseCases { get; set; } = [];
    public IReadOnlyList<AIAnalysisOutput> LatestAnalysisOutputs { get; set; } = [];
}
