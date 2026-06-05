using System.Diagnostics;
using AI_Readiness_Hub.Data;
using AI_Readiness_Hub.Models;
using AI_Readiness_Hub.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AI_Readiness_Hub.Controllers;

[Route("Dashboard")]
public class DashboardController(
    ApplicationDbContext context,
    ILogger<DashboardController> logger) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var stopwatch = Stopwatch.StartNew();
        var stepStopwatch = Stopwatch.StartNew();
        var today = DateTime.UtcNow.Date;

        var clientsByStageRows = await context.ClientCompanies
            .AsNoTracking()
            .GroupBy(client => client.CurrentStage)
            .Select(group => new { Stage = group.Key, Count = group.Count() })
            .ToListAsync();
        var totalClients = clientsByStageRows.Sum(item => item.Count);
        var activeClients = clientsByStageRows
            .Where(item => item.Stage != ClientStage.Closed)
            .Sum(item => item.Count);

        logger.LogInformation(
            "Dashboard client count/stage query completed. TotalClients: {TotalClients}; ActiveClients: {ActiveClients}; StageBuckets: {StageBucketCount}; ElapsedMs: {ElapsedMs}; RequestId: {RequestId}",
            totalClients,
            activeClients,
            clientsByStageRows.Count,
            stepStopwatch.ElapsedMilliseconds,
            HttpContext.TraceIdentifier);
        stepStopwatch.Restart();

        var openTaskRows = await context.ClientTasks
            .AsNoTracking()
            .Where(task => task.Status != ClientTaskStatus.Done)
            .GroupBy(task => task.DueDate.HasValue && task.DueDate < today)
            .Select(group => new { IsOverdue = group.Key, Count = group.Count() })
            .ToListAsync();
        var overdueTasks = openTaskRows.FirstOrDefault(item => item.IsOverdue)?.Count ?? 0;

        var pendingTasks = await context.ClientTasks
            .AsNoTracking()
            .Where(task => task.Status != ClientTaskStatus.Done)
            .OrderBy(task => task.DueDate ?? DateTime.MaxValue)
            .ThenByDescending(task => task.Priority)
            .Take(8)
            .Select(task => new ClientTask
            {
                Id = task.Id,
                ClientCompanyId = task.ClientCompanyId,
                TaskTitle = task.TaskTitle,
                TaskType = task.TaskType,
                DueDate = task.DueDate,
                Priority = task.Priority,
                Status = task.Status,
                ClientCompany = new ClientCompany
                {
                    Id = task.ClientCompanyId,
                    CompanyName = task.ClientCompany!.CompanyName
                }
            })
            .ToListAsync();

        logger.LogInformation(
            "Dashboard pending tasks query completed. OpenTaskBuckets: {OpenTaskBuckets}; OverdueTasks: {OverdueTasks}; PendingTasksShown: {PendingTasksShown}; ElapsedMs: {ElapsedMs}; RequestId: {RequestId}",
            openTaskRows.Count,
            overdueTasks,
            pendingTasks.Count,
            stepStopwatch.ElapsedMilliseconds,
            HttpContext.TraceIdentifier);
        stepStopwatch.Restart();

        var reportsByStatusRows = await context.ClientReports
            .AsNoTracking()
            .GroupBy(report => report.ReportStatus)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToListAsync();
        var reportsInReview = reportsByStatusRows
            .Where(item => item.Status is ReportStatus.DraftGenerated or ReportStatus.InConsultantReview)
            .Sum(item => item.Count);

        var reportsWaitingForReview = await context.ClientReports
            .AsNoTracking()
            .Where(report => report.ReportStatus == ReportStatus.DraftGenerated || report.ReportStatus == ReportStatus.InConsultantReview)
            .OrderByDescending(report => report.GeneratedAt ?? report.CreatedAt)
            .Take(8)
            .Select(report => new ClientReport
            {
                Id = report.Id,
                ClientCompanyId = report.ClientCompanyId,
                ReportTitle = report.ReportTitle,
                ReportStatus = report.ReportStatus,
                GeneratedAt = report.GeneratedAt,
                CreatedAt = report.CreatedAt
            })
            .ToListAsync();

        logger.LogInformation(
            "Dashboard reports for review query completed. ReportsInReview: {ReportsInReview}; ReportsForReviewShown: {ReportsForReviewShown}; ReportStatusBuckets: {ReportStatusBuckets}; ElapsedMs: {ElapsedMs}; RequestId: {RequestId}",
            reportsInReview,
            reportsWaitingForReview.Count,
            reportsByStatusRows.Count,
            stepStopwatch.ElapsedMilliseconds,
            HttpContext.TraceIdentifier);
        stepStopwatch.Restart();

        var formAwaitingSummary = await context.ReadinessAssessments
            .AsNoTracking()
            .Where(assessment => assessment.FormStatus == ReadinessFormStatus.Sent &&
                !context.AssessmentResponses.Any(response => response.ReadinessAssessmentId == assessment.Id))
            .GroupBy(_ => 1)
            .Select(group => new
            {
                FormsAwaitingCompletion = group.Count(),
                ClientsAwaitingFormResponse = group
                    .Select(assessment => assessment.ClientCompanyId)
                    .Distinct()
                    .Count()
            })
            .SingleOrDefaultAsync();

        var formsAwaitingCompletion = formAwaitingSummary?.FormsAwaitingCompletion ?? 0;
        var clientsAwaitingFormResponse = formAwaitingSummary?.ClientsAwaitingFormResponse ?? 0;

        var formsSentNotCompleted = await context.ReadinessAssessments
            .AsNoTracking()
            .Where(assessment => assessment.FormStatus == ReadinessFormStatus.Sent &&
                !context.AssessmentResponses.Any(response => response.ReadinessAssessmentId == assessment.Id))
            .OrderBy(assessment => assessment.SentAt ?? assessment.CreatedAt)
            .Take(8)
            .Select(assessment => new ReadinessAssessment
            {
                Id = assessment.Id,
                ClientCompanyId = assessment.ClientCompanyId,
                SentAt = assessment.SentAt,
                CreatedAt = assessment.CreatedAt,
                FormStatus = assessment.FormStatus,
                ClientCompany = new ClientCompany
                {
                    Id = assessment.ClientCompanyId,
                    CompanyName = assessment.ClientCompany!.CompanyName
                }
            })
            .ToListAsync();

        logger.LogInformation(
            "Dashboard forms awaiting completion query completed. FormsAwaitingCompletion: {FormsAwaitingCompletion}; ClientsAwaitingFormResponse: {ClientsAwaitingFormResponse}; FormsShown: {FormsShown}; ElapsedMs: {ElapsedMs}; RequestId: {RequestId}",
            formsAwaitingCompletion,
            clientsAwaitingFormResponse,
            formsSentNotCompleted.Count,
            stepStopwatch.ElapsedMilliseconds,
            HttpContext.TraceIdentifier);
        stepStopwatch.Restart();

        var responsesReceivedNotReviewed = await context.AssessmentResponses
            .AsNoTracking()
            .CountAsync(response => response.Status == AssessmentResponseStatus.Received || response.Status == AssessmentResponseStatus.Imported);

        var recentlyReceivedResponses = await context.AssessmentResponses
            .AsNoTracking()
            .Where(response => response.Status != AssessmentResponseStatus.Ignored)
            .OrderByDescending(response => response.ReceivedAt)
            .ThenByDescending(response => response.Id)
            .Take(6)
            .Select(response => new AssessmentResponse
            {
                Id = response.Id,
                ReadinessAssessmentId = response.ReadinessAssessmentId,
                ResponseLabel = response.ResponseLabel,
                Source = response.Source,
                ReceivedAt = response.ReceivedAt,
                AnswerCount = response.AnswerCount,
                Status = response.Status,
                ReadinessAssessment = new ReadinessAssessment
                {
                    Id = response.ReadinessAssessmentId,
                    ClientCompanyId = response.ReadinessAssessment!.ClientCompanyId,
                    ClientCompany = new ClientCompany
                    {
                        Id = response.ReadinessAssessment.ClientCompanyId,
                        CompanyName = response.ReadinessAssessment.ClientCompany!.CompanyName
                    }
                }
            })
            .ToListAsync();

        logger.LogInformation(
            "Dashboard recent responses query completed. ResponsesReceivedNotReviewed: {ResponsesReceivedNotReviewed}; RecentResponsesShown: {RecentResponsesShown}; ElapsedMs: {ElapsedMs}; RequestId: {RequestId}",
            responsesReceivedNotReviewed,
            recentlyReceivedResponses.Count,
            stepStopwatch.ElapsedMilliseconds,
            HttpContext.TraceIdentifier);
        stepStopwatch.Restart();

        var recentlyUpdatedClients = await context.ClientCompanies
            .AsNoTracking()
            .OrderByDescending(client => client.LastModifiedAt ?? client.CreatedAt)
            .Take(8)
            .Select(client => new DashboardClientSummaryViewModel
            {
                Id = client.Id,
                CompanyName = client.CompanyName,
                Industry = client.Industry,
                CurrentStage = client.CurrentStage,
                NextAction = client.NextAction,
                LastUpdated = client.LastModifiedAt ?? client.CreatedAt,
                LatestReportStatus = context.ClientReports
                    .Where(report => report.ClientCompanyId == client.Id)
                    .OrderByDescending(report => report.VersionNumber)
                    .ThenByDescending(report => report.CreatedAt)
                    .Select(report => (ReportStatus?)report.ReportStatus)
                    .FirstOrDefault() ?? ReportStatus.NotStarted
            })
            .ToListAsync();

        logger.LogInformation(
            "Dashboard recently updated clients query completed. RecentlyUpdatedClientsShown: {RecentlyUpdatedClientsShown}; ElapsedMs: {ElapsedMs}; RequestId: {RequestId}",
            recentlyUpdatedClients.Count,
            stepStopwatch.ElapsedMilliseconds,
            HttpContext.TraceIdentifier);
        stepStopwatch.Restart();

        var clientsByStage = Enum.GetValues<ClientStage>()
            .ToDictionary(stage => stage, stage => clientsByStageRows.FirstOrDefault(item => item.Stage == stage)?.Count ?? 0);
        var reportsByStatus = Enum.GetValues<ReportStatus>()
            .ToDictionary(status => status, status => reportsByStatusRows.FirstOrDefault(item => item.Status == status)?.Count ?? 0);

        logger.LogInformation(
            "Dashboard grouped/status calculations completed. ClientStageStatuses: {ClientStageStatuses}; ReportStatuses: {ReportStatuses}; ElapsedMs: {ElapsedMs}; RequestId: {RequestId}",
            clientsByStage.Count,
            reportsByStatus.Count,
            stepStopwatch.ElapsedMilliseconds,
            HttpContext.TraceIdentifier);

        var viewModel = new DashboardViewModel
        {
            TotalClients = totalClients,
            ActiveClients = activeClients,
            OverdueTasks = overdueTasks,
            ReportsInReview = reportsInReview,
            FormsAwaitingCompletion = formsAwaitingCompletion,
            ClientsAwaitingFormResponse = clientsAwaitingFormResponse,
            ResponsesReceivedNotReviewed = responsesReceivedNotReviewed,
            ClientsByStage = clientsByStage,
            ReportsByStatus = reportsByStatus,
            PendingTasks = pendingTasks,
            FormsSentNotCompleted = formsSentNotCompleted,
            RecentlyReceivedAssessmentResponses = recentlyReceivedResponses,
            ReportsWaitingForReview = reportsWaitingForReview,
            RecentlyUpdatedClients = recentlyUpdatedClients
        };

        logger.LogInformation(
            "Dashboard loaded. Clients: {TotalClients}; PendingTasksShown: {PendingTasks}; RecentResponsesShown: {RecentResponses}; ReportsForReviewShown: {ReportsForReview}; ElapsedMs: {ElapsedMs}; RequestId: {RequestId}",
            viewModel.TotalClients,
            viewModel.PendingTasks.Count,
            viewModel.RecentlyReceivedAssessmentResponses.Count,
            viewModel.ReportsWaitingForReview.Count,
            stopwatch.ElapsedMilliseconds,
            HttpContext.TraceIdentifier);

        return View(viewModel);
    }
}
