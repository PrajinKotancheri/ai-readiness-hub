using AI_Readiness_Hub.Models;

namespace AI_Readiness_Hub.Services;

public interface IReadinessFormService
{
    Task<ReadinessAssessment> EnsureGeneratedFormLinkAsync(int clientId, string? customFormUrl = null, CancellationToken cancellationToken = default);
    Task<ReadinessAssessment> SendReadinessFormAsync(int clientId, CancellationToken cancellationToken = default);
    Task<ReadinessAssessment> SendReminderAsync(int clientId, CancellationToken cancellationToken = default);
    string BuildGeneratedFormUrl(string baseFormUrl, string clientReferenceEntryId, string clientToken);
}
