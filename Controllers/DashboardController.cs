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
        var today = DateTime.UtcNow.Date;

        var totalClients = await context.ClientCompanies.AsNoTracking().CountAsync();
        var activeClients = await context.ClientCompanies.AsNoTracking().CountAsync(client => client.CurrentStage != ClientStage.Closed);
        var overdueTasks = await context.ClientTasks
            .AsNoTracking()
            .CountAsync(task => task.Status != ClientTaskStatus.Done && task.DueDate.HasValue && task.DueDate.Value.Date < today);
        var reportsInReview = await context.ClientReports
            .AsNoTracking()
            .CountAsync(report => report.ReportStatus == ReportStatus.DraftGenerated || report.ReportStatus == ReportStatus.InConsultantReview);
        var formsAwaitingCompletion = await context.ReadinessAssessments
            .AsNoTracking()
            .CountAsync(assessment => assessment.FormStatus == ReadinessFormStatus.Sent &&
                !context.AssessmentResponses.Any(response => response.ReadinessAssessmentId == assessment.Id));
        var responsesReceivedNotReviewed = await context.AssessmentResponses
            .AsNoTracking()
            .CountAsync(response => response.Status == AssessmentResponseStatus.Received || response.Status == AssessmentResponseStatus.Imported);

        var clientsByStageRows = await context.ClientCompanies
            .AsNoTracking()
            .GroupBy(client => client.CurrentStage)
            .Select(group => new { Stage = group.Key, Count = group.Count() })
            .ToListAsync();
        var reportsByStatusRows = await context.ClientReports
            .AsNoTracking()
            .GroupBy(report => report.ReportStatus)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToListAsync();

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

        var recentlyUpdatedClients = await context.ClientCompanies
            .AsNoTracking()
            .OrderByDescending(client => client.LastModifiedAt ?? client.CreatedAt)
            .Take(8)
            .Select(client => new ClientCompany
            {
                Id = client.Id,
                CompanyName = client.CompanyName,
                Industry = client.Industry,
                CurrentStage = client.CurrentStage,
                NextAction = client.NextAction,
                CreatedAt = client.CreatedAt,
                LastModifiedAt = client.LastModifiedAt
            })
            .ToListAsync();
        var recentlyUpdatedClientIds = recentlyUpdatedClients.Select(client => client.Id).ToList();
        var recentClientReports = await context.ClientReports
            .AsNoTracking()
            .Where(report => recentlyUpdatedClientIds.Contains(report.ClientCompanyId))
            .OrderByDescending(report => report.VersionNumber)
            .ThenByDescending(report => report.CreatedAt)
            .Select(report => new ClientReport
            {
                Id = report.Id,
                ClientCompanyId = report.ClientCompanyId,
                ReportStatus = report.ReportStatus,
                VersionNumber = report.VersionNumber,
                CreatedAt = report.CreatedAt
            })
            .ToListAsync();
        foreach (var client in recentlyUpdatedClients)
        {
            var latestReport = recentClientReports
                .Where(report => report.ClientCompanyId == client.Id)
                .OrderByDescending(report => report.VersionNumber)
                .ThenByDescending(report => report.CreatedAt)
                .FirstOrDefault();
            if (latestReport is not null)
            {
                client.Reports.Add(latestReport);
            }
        }

        var latestAssessmentRows = await context.ReadinessAssessments
            .AsNoTracking()
            .GroupBy(assessment => assessment.ClientCompanyId)
            .Select(group => group
                .OrderByDescending(assessment => assessment.CreatedAt)
                .Select(assessment => new
                {
                    assessment.Id,
                    assessment.FormStatus
                })
                .First())
            .ToListAsync();
        var latestAssessmentIds = latestAssessmentRows.Select(assessment => assessment.Id).ToList();
        var latestAssessmentIdsWithResponses = await context.AssessmentResponses
            .AsNoTracking()
            .Where(response => latestAssessmentIds.Contains(response.ReadinessAssessmentId))
            .Select(response => response.ReadinessAssessmentId)
            .Distinct()
            .ToListAsync();
        var clientsAwaitingFormResponse = latestAssessmentRows.Count(assessment =>
            assessment.FormStatus == ReadinessFormStatus.Sent &&
            !latestAssessmentIdsWithResponses.Contains(assessment.Id));

        var viewModel = new DashboardViewModel
        {
            TotalClients = totalClients,
            ActiveClients = activeClients,
            OverdueTasks = overdueTasks,
            ReportsInReview = reportsInReview,
            FormsAwaitingCompletion = formsAwaitingCompletion,
            ClientsAwaitingFormResponse = clientsAwaitingFormResponse,
            ResponsesReceivedNotReviewed = responsesReceivedNotReviewed,
            ClientsByStage = Enum.GetValues<ClientStage>()
                .ToDictionary(stage => stage, stage => clientsByStageRows.FirstOrDefault(item => item.Stage == stage)?.Count ?? 0),
            ReportsByStatus = Enum.GetValues<ReportStatus>()
                .ToDictionary(status => status, status => reportsByStatusRows.FirstOrDefault(item => item.Status == status)?.Count ?? 0),
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
