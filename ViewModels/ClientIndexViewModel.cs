using AI_Readiness_Hub.Models;

namespace AI_Readiness_Hub.ViewModels;

public class ClientIndexViewModel
{
    public ClientStage? Stage { get; set; }
    public string? Industry { get; set; }
    public string? AssignedConsultant { get; set; }
    public ReportStatus? ReportStatus { get; set; }
    public TaskPriority? Priority { get; set; }
    public DateTime? LastModifiedFrom { get; set; }
    public IReadOnlyList<string> Industries { get; set; } = [];
    public IReadOnlyList<string> Consultants { get; set; } = [];
    public IReadOnlyList<ClientListItemViewModel> Clients { get; set; } = [];
}

public class ClientListItemViewModel
{
    public int Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? Industry { get; set; }
    public ClientStage Stage { get; set; }
    public ReadinessFormStatus ReadinessFormStatus { get; set; } = ReadinessFormStatus.NotSent;
    public ReportStatus ReportStatus { get; set; } = ReportStatus.NotStarted;
    public string? NextAction { get; set; }
    public TaskPriority Priority { get; set; }
    public DateTime LastUpdated { get; set; }
}
