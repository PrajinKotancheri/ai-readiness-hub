namespace AI_Readiness_Hub.Services;

public interface IKnowledgeGapAnalysisService
{
    Task<int> GenerateAsync(int clientId);
}
