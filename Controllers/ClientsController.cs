using AI_Readiness_Hub.Data;
using AI_Readiness_Hub.Models;
using AI_Readiness_Hub.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AI_Readiness_Hub.Controllers;

[Route("Clients")]
public class ClientsController(ApplicationDbContext context) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] ClientIndexViewModel filters)
    {
        var query = context.ClientCompanies
            .Include(client => client.ReadinessAssessments)
            .Include(client => client.Reports)
            .AsQueryable();

        if (filters.Stage.HasValue)
        {
            query = query.Where(client => client.CurrentStage == filters.Stage.Value);
        }

        if (!string.IsNullOrWhiteSpace(filters.Industry))
        {
            query = query.Where(client => client.Industry == filters.Industry);
        }

        if (!string.IsNullOrWhiteSpace(filters.AssignedConsultant))
        {
            query = query.Where(client => client.AssignedConsultant == filters.AssignedConsultant);
        }

        if (filters.Priority.HasValue)
        {
            query = query.Where(client => client.Priority == filters.Priority.Value);
        }

        if (filters.LastModifiedFrom.HasValue)
        {
            query = query.Where(client => (client.LastModifiedAt ?? client.CreatedAt) >= filters.LastModifiedFrom.Value);
        }

        var clients = await query
            .OrderBy(client => client.CompanyName)
            .ToListAsync();

        if (filters.ReportStatus.HasValue)
        {
            clients = clients
                .Where(client => GetLatestReportStatus(client) == filters.ReportStatus.Value)
                .ToList();
        }

        filters.Clients = clients.Select(client => new ClientListItemViewModel
        {
            Id = client.Id,
            CompanyName = client.CompanyName,
            Industry = client.Industry,
            Stage = client.CurrentStage,
            Priority = client.Priority,
            ReadinessFormStatus = client.ReadinessAssessments
                .OrderByDescending(assessment => assessment.CreatedAt)
                .FirstOrDefault()?.FormStatus ?? ReadinessFormStatus.NotSent,
            ReportStatus = GetLatestReportStatus(client),
            NextAction = client.NextAction,
            LastUpdated = client.LastModifiedAt ?? client.CreatedAt
        }).ToList();

        filters.Industries = await context.ClientCompanies
            .Where(client => client.Industry != null)
            .Select(client => client.Industry!)
            .Distinct()
            .OrderBy(industry => industry)
            .ToListAsync();

        filters.Consultants = await context.ClientCompanies
            .Where(client => client.AssignedConsultant != null)
            .Select(client => client.AssignedConsultant!)
            .Distinct()
            .OrderBy(consultant => consultant)
            .ToListAsync();

        return View(filters);
    }

    [HttpGet("Create")]
    public IActionResult Create()
    {
        return View(new ClientCompany
        {
            CurrentStage = ClientStage.New,
            Priority = TaskPriority.Medium,
            CreatedAt = DateTime.UtcNow
        });
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ClientCompany client)
    {
        if (!ModelState.IsValid)
        {
            return View(client);
        }

        client.CreatedAt = DateTime.UtcNow;
        client.CreatedBy = "Consultant";
        client.LastModifiedAt = DateTime.UtcNow;
        client.LastModifiedBy = "Consultant";
        AddDefaultWorkflow(client);
        client.ActivityLogs.Add(new ClientActivityLog
        {
            ActivityType = "Client created",
            Description = "Client workspace created.",
            CreatedBy = "Consultant",
            CreatedAt = DateTime.UtcNow
        });

        context.ClientCompanies.Add(client);
        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Workspace), new { id = client.Id });
    }

    [HttpGet("Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var client = await context.ClientCompanies.FindAsync(id);
        return client is null ? NotFound() : View(client);
    }

    [HttpPost("Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ClientCompany posted)
    {
        var client = await context.ClientCompanies.FindAsync(id);
        if (client is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            posted.Id = id;
            return View(posted);
        }

        client.CompanyName = posted.CompanyName;
        client.Industry = posted.Industry;
        client.WebsiteUrl = posted.WebsiteUrl;
        client.Country = posted.Country;
        client.Region = posted.Region;
        client.CompanySizeRange = posted.CompanySizeRange;
        client.RevenueRange = posted.RevenueRange;
        client.BusinessModel = posted.BusinessModel;
        client.ContactPersonName = posted.ContactPersonName;
        client.ContactPersonEmail = posted.ContactPersonEmail;
        client.ContactPersonPhone = posted.ContactPersonPhone;
        client.ConsultingPackage = posted.ConsultingPackage;
        client.AssignedConsultant = posted.AssignedConsultant;
        client.Priority = posted.Priority;
        client.CurrentStage = posted.CurrentStage;
        client.OverallReadinessScore = posted.OverallReadinessScore;
        client.KeyRisksSummary = posted.KeyRisksSummary;
        client.NextAction = posted.NextAction;
        client.LastModifiedAt = DateTime.UtcNow;
        client.LastModifiedBy = "Consultant";

        context.ClientActivityLogs.Add(new ClientActivityLog
        {
            ClientCompanyId = id,
            ActivityType = "Client updated",
            Description = "Client profile fields updated.",
            CreatedBy = "Consultant",
            CreatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Workspace), new { id });
    }

    [HttpGet("Details/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var client = await LoadWorkspaceClientAsync(id);
        return client is null ? NotFound() : View(client);
    }

    [HttpGet("Workspace/{id:int}")]
    public async Task<IActionResult> Workspace(int id, int? responseId)
    {
        var client = await LoadWorkspaceClientAsync(id);
        if (client is null)
        {
            return NotFound();
        }

        var latestAssessment = client.ReadinessAssessments
            .OrderByDescending(assessment => assessment.CreatedAt)
            .FirstOrDefault();
        var assessmentResponses = latestAssessment?.Responses
            .OrderBy(response => response.ResponseNumber)
            .ToList() ?? [];
        var selectedAssessmentResponse = responseId.HasValue
            ? assessmentResponses.FirstOrDefault(response => response.Id == responseId.Value)
            : assessmentResponses
                .Where(response => response.Status != AssessmentResponseStatus.Ignored)
                .OrderByDescending(response => response.ReceivedAt)
                .ThenByDescending(response => response.ResponseNumber)
                .FirstOrDefault();
        ApplyResponseAwareWorkflow(client);
        var latestReport = client.Reports
            .OrderByDescending(report => report.VersionNumber)
            .ThenByDescending(report => report.CreatedAt)
            .FirstOrDefault();
        var latestScore = client.ReadinessScores
            .OrderByDescending(score => score.CreatedAt)
            .FirstOrDefault();
        var readinessFormSettings = await context.ReadinessFormSettings
            .Where(settings => settings.IsActive)
            .OrderByDescending(settings => settings.LastModifiedAt ?? settings.CreatedAt)
            .FirstOrDefaultAsync();

        var viewModel = new ClientWorkspaceViewModel
        {
            Client = client,
            LatestAssessment = latestAssessment,
            ReadinessFormSettings = readinessFormSettings,
            LatestReport = latestReport,
            LatestScore = latestScore,
            AssessmentResponses = assessmentResponses,
            SelectedAssessmentResponse = selectedAssessmentResponse,
            SelectedAnswersBySection = selectedAssessmentResponse?.Answers
                .OrderBy(answer => answer.SectionName)
                .ThenBy(answer => answer.Id)
                .GroupBy(answer => answer.SectionName)
                .ToList() ?? [],
            SwotByCategory = client.SwotItems
                .OrderBy(item => item.Category)
                .GroupBy(item => item.Category)
                .ToList(),
            RoadmapByPhase = client.RoadmapItems
                .OrderBy(item => item.Phase)
                .GroupBy(item => item.Phase)
                .ToList(),
            RankedUseCases = client.UseCases
                .OrderByDescending(useCase => useCase.Score?.PriorityScore ?? 0)
                .ThenBy(useCase => useCase.Title)
                .ToList(),
            LatestAnalysisOutputs = client.AnalysisOutputs
                .GroupBy(output => output.AnalysisType)
                .Select(group => group.OrderByDescending(output => output.VersionNumber).First())
                .OrderByDescending(output => output.CreatedAt)
                .ToList()
        };

        return View(viewModel);
    }

    private async Task<ClientCompany?> LoadWorkspaceClientAsync(int id)
    {
        return await context.ClientCompanies
            .Include(client => client.WorkflowSteps)
            .Include(client => client.ReadinessAssessments)
                .ThenInclude(assessment => assessment.Answers)
            .Include(client => client.ReadinessAssessments)
                .ThenInclude(assessment => assessment.Responses)
                    .ThenInclude(response => response.Answers)
            .Include(client => client.Documents)
            .Include(client => client.Notes)
            .Include(client => client.MeetingTranscripts)
            .Include(client => client.AnalysisOutputs)
            .Include(client => client.GapAnalysisItems)
            .Include(client => client.SwotItems)
            .Include(client => client.IndustryInsights)
            .Include(client => client.CompetitorInsights)
            .Include(client => client.UseCases)
                .ThenInclude(useCase => useCase.Score)
            .Include(client => client.ReadinessScores)
            .Include(client => client.RoadmapItems)
            .Include(client => client.Reports)
                .ThenInclude(report => report.Sections)
            .Include(client => client.Tasks)
            .Include(client => client.ActivityLogs)
            .FirstOrDefaultAsync(client => client.Id == id);
    }

    private static ReportStatus GetLatestReportStatus(ClientCompany client)
    {
        return client.Reports
            .OrderByDescending(report => report.VersionNumber)
            .FirstOrDefault()?.ReportStatus ?? ReportStatus.NotStarted;
    }

    private static void ApplyResponseAwareWorkflow(ClientCompany client)
    {
        var formCompletedStep = client.WorkflowSteps.FirstOrDefault(step => step.StageName == "Form Completed");
        if (formCompletedStep is null)
        {
            return;
        }

        var latestAnsweredResponse = client.ReadinessAssessments
            .SelectMany(assessment => assessment.Responses)
            .Where(response => response.Status != AssessmentResponseStatus.Ignored && response.Answers.Any())
            .OrderByDescending(response => response.ReceivedAt)
            .FirstOrDefault();

        if (latestAnsweredResponse is null)
        {
            formCompletedStep.Status = WorkflowStepStatus.NotStarted;
            formCompletedStep.CompletedAt = null;
            return;
        }

        formCompletedStep.Status = WorkflowStepStatus.Completed;
        formCompletedStep.CompletedAt ??= latestAnsweredResponse.ReceivedAt;
    }

    private static void AddDefaultWorkflow(ClientCompany client)
    {
        var stages = new[]
        {
            "Client Registered",
            "Readiness Form Sent",
            "Form Completed",
            "Documents Uploaded",
            "Initial AI Analysis Completed",
            "Gap Analysis Completed",
            "Consultant Session Completed",
            "Report Draft Generated",
            "Consultant Review Completed",
            "Final Report Delivered",
            "Client Feedback Collected"
        };

        foreach (var (stage, index) in stages.Select((stage, index) => (stage, index)))
        {
            client.WorkflowSteps.Add(new ClientWorkflowStep
            {
                StageName = stage,
                DisplayOrder = index + 1,
                Status = index == 0 ? WorkflowStepStatus.Completed : WorkflowStepStatus.NotStarted,
                CompletedAt = index == 0 ? DateTime.UtcNow : null
            });
        }
    }
}
