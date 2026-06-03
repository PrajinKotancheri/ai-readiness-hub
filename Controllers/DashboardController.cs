using AI_Readiness_Hub.Data;
using AI_Readiness_Hub.Models;
using AI_Readiness_Hub.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AI_Readiness_Hub.Controllers;

[Route("Dashboard")]
public class DashboardController(ApplicationDbContext context) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var clients = await context.ClientCompanies
            .Include(client => client.Tasks)
            .Include(client => client.ReadinessAssessments)
            .Include(client => client.Documents)
            .Include(client => client.Reports)
            .OrderBy(client => client.CompanyName)
            .ToListAsync();

        var reports = clients
            .SelectMany(client => client.Reports)
            .ToList();
        var assessments = clients
            .SelectMany(client => client.ReadinessAssessments)
            .ToList();

        var pendingTasks = clients
            .SelectMany(client => client.Tasks)
            .Where(task => task.Status is not ClientTaskStatus.Done)
            .ToList();
        var formsSentNotCompleted = assessments
            .Where(assessment => assessment.FormStatus == ReadinessFormStatus.Sent)
            .OrderBy(assessment => assessment.SentAt ?? assessment.CreatedAt)
            .ToList();
        var recentlyReceivedResponses = assessments
            .Where(assessment => assessment.FormStatus is ReadinessFormStatus.Completed or ReadinessFormStatus.Imported)
            .OrderByDescending(assessment => assessment.ResponseReceivedAt ?? assessment.CompletedAt ?? assessment.ImportedAt ?? assessment.LastModifiedAt ?? assessment.CreatedAt)
            .ToList();
        var reportsWaitingForReview = reports
            .Where(report => report.ReportStatus is ReportStatus.DraftGenerated or ReportStatus.InConsultantReview)
            .OrderByDescending(report => report.GeneratedAt ?? report.CreatedAt)
            .ToList();

        var viewModel = new DashboardViewModel
        {
            TotalClients = clients.Count,
            ActiveClients = clients.Count(client => client.CurrentStage is not ClientStage.Closed),
            OverdueTasks = pendingTasks.Count(task => task.DueDate.HasValue && task.DueDate.Value.Date < DateTime.UtcNow.Date),
            ReportsInReview = reportsWaitingForReview.Count,
            FormsAwaitingCompletion = formsSentNotCompleted.Count,
            ClientsAwaitingFormResponse = clients.Count(client => client.ReadinessAssessments
                .OrderByDescending(assessment => assessment.CreatedAt)
                .FirstOrDefault()?.FormStatus == ReadinessFormStatus.Sent),
            ResponsesReceivedNotReviewed = clients.Count(client =>
                client.CurrentStage == ClientStage.AssessmentCompleted &&
                client.ReadinessAssessments.Any(assessment => assessment.FormStatus is ReadinessFormStatus.Completed or ReadinessFormStatus.Imported)),
            ClientsByStage = Enum.GetValues<ClientStage>()
                .ToDictionary(stage => stage, stage => clients.Count(client => client.CurrentStage == stage)),
            ReportsByStatus = Enum.GetValues<ReportStatus>()
                .ToDictionary(status => status, status => reports.Count(report => report.ReportStatus == status)),
            PendingTasks = pendingTasks
                .OrderBy(task => task.DueDate ?? DateTime.MaxValue)
                .ThenByDescending(task => task.Priority)
                .Take(8)
                .ToList(),
            FormsSentNotCompleted = formsSentNotCompleted
                .Take(8)
                .ToList(),
            RecentlyReceivedAssessmentResponses = recentlyReceivedResponses
                .Take(6)
                .ToList(),
            ClientsWithMissingDocuments = clients
                .Where(client => client.Documents.Count == 0 && client.CurrentStage >= ClientStage.AssessmentCompleted)
                .OrderBy(client => client.CompanyName)
                .Take(8)
                .ToList(),
            ReportsWaitingForReview = reportsWaitingForReview
                .Take(8)
                .ToList(),
            RecentlyUpdatedClients = clients
                .OrderByDescending(client => client.LastModifiedAt ?? client.CreatedAt)
                .Take(8)
                .ToList()
        };

        return View(viewModel);
    }
}
