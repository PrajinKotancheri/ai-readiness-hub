using AI_Readiness_Hub.Models;

namespace AI_Readiness_Hub.Services;

public class MockClientDocumentSummaryService : IClientDocumentSummaryService
{
    public Task<string> GeneratePlaceholderSummaryAsync(ClientDocument document)
    {
        var summary = $"Placeholder summary for {document.FileName}: review this {document.DocumentType} and capture the client-specific insights before using it in a report.";
        return Task.FromResult(summary);
    }
}
