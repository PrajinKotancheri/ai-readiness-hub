using AI_Readiness_Hub.Models;

namespace AI_Readiness_Hub.ViewModels;

public class AIWorkspaceViewModel
{
    public AIWorkspaceSession Session { get; set; } = new();
    public string ClientCompanyName { get; set; } = string.Empty;
    public string CurrentDraft { get; set; } = string.Empty;
    public int SourceCount { get; set; }
    public IReadOnlyList<AIWorkspaceMessage> Messages { get; set; } = [];
    public IReadOnlyList<AIOutputRevision> Revisions { get; set; } = [];
    public string? WarningMessage { get; set; }
}
