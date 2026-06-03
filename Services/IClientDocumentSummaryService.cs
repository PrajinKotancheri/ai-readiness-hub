using AI_Readiness_Hub.Models;

namespace AI_Readiness_Hub.Services;

public interface IClientDocumentSummaryService
{
    Task<string> GeneratePlaceholderSummaryAsync(ClientDocument document);
}
