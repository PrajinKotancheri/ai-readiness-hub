using AI_Readiness_Hub.Models;

namespace AI_Readiness_Hub.ViewModels;

public class DashboardViewModel
{
    public int TotalClients { get; set; }
    public int ActiveClients { get; set; }
    public int OverdueTasks { get; set; }
    public int ReportsInReview { get; set; }
    public int FormsAwaitingCompletion { get; set; }
    public int ClientsAwaitingFormResponse { get; set; }
    public int ResponsesReceivedNotReviewed { get; set; }
    public IReadOnlyDictionary<ClientStage, int> ClientsByStage { get; set; } = new Dictionary<ClientStage, int>();
    public IReadOnlyDictionary<ReportStatus, int> ReportsByStatus { get; set; } = new Dictionary<ReportStatus, int>();
    public IReadOnlyList<ClientTask> PendingTasks { get; set; } = [];
    public IReadOnlyList<ReadinessAssessment> FormsSentNotCompleted { get; set; } = [];
    public IReadOnlyList<AssessmentResponse> RecentlyReceivedAssessmentResponses { get; set; } = [];
    public IReadOnlyList<ClientCompany> ClientsWithMissingDocuments { get; set; } = [];
    public IReadOnlyList<ClientReport> ReportsWaitingForReview { get; set; } = [];
    public IReadOnlyList<DashboardClientSummaryViewModel> RecentlyUpdatedClients { get; set; } = [];
}

public class DashboardClientSummaryViewModel
{
    public int Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? Industry { get; set; }
    public ClientStage CurrentStage { get; set; }
    public ReportStatus LatestReportStatus { get; set; } = ReportStatus.NotStarted;
    public string? NextAction { get; set; }
    public DateTime LastUpdated { get; set; }
}
