namespace AI_Readiness_Hub.Services;

public interface IAIConsultingAnalysisService
{
    Task GenerateCompanySummaryAsync(int clientId);
    Task GenerateGapAnalysisAsync(int clientId);
    Task GenerateSwotAnalysisAsync(int clientId);
    Task GenerateIndustryAnalysisAsync(int clientId);
    Task GenerateCompetitorInsightsAsync(int clientId);
    Task GenerateUseCasesAsync(int clientId);
    Task ScoreUseCasesAsync(int clientId);
    Task GenerateReadinessScoreAsync(int clientId);
    Task GenerateRoadmapAsync(int clientId);
    Task GenerateReportDraftAsync(int clientId);
}
