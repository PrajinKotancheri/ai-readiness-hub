using System.Diagnostics;
using AI_Readiness_Hub.Data;
using AI_Readiness_Hub.Models;
using AI_Readiness_Hub.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AI_Readiness_Hub.Controllers;

[Route("Clients")]
public class ClientsController(
    ApplicationDbContext context,
    ILogger<ClientsController> logger) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] ClientIndexViewModel filters)
    {
        var stopwatch = Stopwatch.StartNew();
        var query = context.ClientCompanies
            .AsNoTracking()
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

        if (filters.ReportStatus.HasValue)
        {
            query = query.Where(client =>
                (context.ClientReports
                    .Where(report => report.ClientCompanyId == client.Id)
                    .OrderByDescending(report => report.VersionNumber)
                    .ThenByDescending(report => report.CreatedAt)
                    .Select(report => (ReportStatus?)report.ReportStatus)
                    .FirstOrDefault() ?? ReportStatus.NotStarted) == filters.ReportStatus.Value);
        }

        filters.Clients = await query
            .OrderBy(client => client.CompanyName)
            .Take(250)
            .Select(client => new ClientListItemViewModel
            {
                Id = client.Id,
                CompanyName = client.CompanyName,
                Industry = client.Industry,
                Stage = client.CurrentStage,
                Priority = client.Priority,
                ReadinessFormStatus = context.ReadinessAssessments
                    .Where(assessment => assessment.ClientCompanyId == client.Id)
                    .OrderByDescending(assessment => assessment.CreatedAt)
                    .Select(assessment => (ReadinessFormStatus?)assessment.FormStatus)
                    .FirstOrDefault() ?? ReadinessFormStatus.NotSent,
                ReportStatus = context.ClientReports
                    .Where(report => report.ClientCompanyId == client.Id)
                    .OrderByDescending(report => report.VersionNumber)
                    .ThenByDescending(report => report.CreatedAt)
                    .Select(report => (ReportStatus?)report.ReportStatus)
                    .FirstOrDefault() ?? ReportStatus.NotStarted,
                NextAction = client.NextAction,
                LastUpdated = client.LastModifiedAt ?? client.CreatedAt
            })
            .ToListAsync();

        filters.Industries = await context.ClientCompanies
            .AsNoTracking()
            .Where(client => client.Industry != null)
            .Select(client => client.Industry!)
            .Distinct()
            .OrderBy(industry => industry)
            .ToListAsync();

        filters.Consultants = await context.ClientCompanies
            .AsNoTracking()
            .Where(client => client.AssignedConsultant != null)
            .Select(client => client.AssignedConsultant!)
            .Distinct()
            .OrderBy(consultant => consultant)
            .ToListAsync();

        logger.LogInformation(
            "Clients list loaded. ClientsShown: {ClientCount}; Stage: {Stage}; ReportStatus: {ReportStatus}; IndustryFiltered: {IndustryFiltered}; ConsultantFiltered: {ConsultantFiltered}; ElapsedMs: {ElapsedMs}; RequestId: {RequestId}",
            filters.Clients.Count,
            filters.Stage,
            filters.ReportStatus,
            !string.IsNullOrWhiteSpace(filters.Industry),
            !string.IsNullOrWhiteSpace(filters.AssignedConsultant),
            stopwatch.ElapsedMilliseconds,
            HttpContext.TraceIdentifier);

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
        var client = await context.ClientCompanies
            .AsNoTracking()
            .FirstOrDefaultAsync(client => client.Id == id);
        return client is null ? NotFound() : View(client);
    }

    [HttpGet("Workspace/{id:int}")]
    public async Task<IActionResult> Workspace(
        int id,
        int? responseId,
        string? activeTab,
        int? selectedResponseId,
        int? scrollY)
    {
        var totalStopwatch = Stopwatch.StartNew();
        var requestedResponseId = selectedResponseId ?? responseId;
        try
        {
            var viewModel = await LoadWorkspaceShellViewModelAsync(id, requestedResponseId, activeTab, scrollY);
            if (viewModel is null)
            {
                return NotFound();
            }

            logger.LogInformation(
                "Client workspace shell loaded. ClientCompanyId: {ClientCompanyId}; ActiveTab: {ActiveTab}; SelectedResponseId: {SelectedResponseId}; Responses: {ResponseCount}; LatestReportStatus: {LatestReportStatus}; ElapsedMs: {ElapsedMs}; RequestId: {RequestId}",
                id,
                viewModel.ActiveWorkspaceTabKey,
                viewModel.RequestedResponseId,
                viewModel.AssessmentResponseCount,
                viewModel.LatestReport?.ReportStatus ?? ReportStatus.NotStarted,
                totalStopwatch.ElapsedMilliseconds,
                HttpContext.TraceIdentifier);

            return View(viewModel);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Client workspace failed. ClientCompanyId: {ClientCompanyId}; ActiveTab: {ActiveTab}; ResponseId: {ResponseId}; SelectedResponseId: {SelectedResponseId}; ElapsedMs: {ElapsedMs}; RequestId: {RequestId}",
                id,
                activeTab,
                responseId,
                selectedResponseId,
                totalStopwatch.ElapsedMilliseconds,
                HttpContext.TraceIdentifier);
            throw;
        }
    }

    [HttpGet("Workspace/{id:int}/Tab/{tabKey}")]
    public async Task<IActionResult> WorkspaceTab(int id, string tabKey, int? responseId)
    {
        var normalizedTab = NormalizeWorkspaceTabKey(tabKey);
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = normalizedTab switch
            {
                "overview" => ("WorkspaceTabs/_Overview", "Overview", await LoadWorkspaceShellViewModelAsync(id, responseId)),
                "assessmentanswers" => ("WorkspaceTabs/_AssessmentAnswers", "Assessment answers", await LoadAssessmentTabViewModelAsync(id, responseId)),
                "documents" => ("WorkspaceTabs/_Documents", "Documents", await LoadDocumentsTabViewModelAsync(id)),
                "notestranscripts" => ("WorkspaceTabs/_NotesTranscripts", "Notes and transcripts", await LoadNotesTranscriptsTabViewModelAsync(id)),
                "knowledgegapanalysis" => ("WorkspaceTabs/_KnowledgeGapAnalysis", "Knowledge gap analysis", await LoadKnowledgeGapAnalysisTabViewModelAsync(id)),
                "companysummary" => ("WorkspaceTabs/_CompanySummary", "Company summary", await LoadCompanySummaryTabViewModelAsync(id)),
                "aidrafts" => ("WorkspaceTabs/_AIDrafts", "AI drafts", await LoadAIDraftsTabViewModelAsync(id)),
                "gapanalysis" => ("WorkspaceTabs/_GapAnalysis", "Gap analysis", await LoadGapAnalysisTabViewModelAsync(id)),
                "swot" => ("WorkspaceTabs/_Swot", "SWOT", await LoadSwotTabViewModelAsync(id)),
                "industrycompetitors" => ("WorkspaceTabs/_IndustryCompetitors", "Industry and competitors", await LoadIndustryCompetitorsTabViewModelAsync(id)),
                "usecasesscoring" => ("WorkspaceTabs/_UseCasesScoring", "Use cases and scoring", await LoadUseCasesScoringTabViewModelAsync(id)),
                "roadmap" => ("WorkspaceTabs/_Roadmap", "Roadmap", await LoadRoadmapTabViewModelAsync(id)),
                "reports" => ("WorkspaceTabs/_Reports", "Reports", await LoadReportsTabViewModelAsync(id)),
                "tasks" => ("WorkspaceTabs/_Tasks", "Tasks and follow-ups", await LoadTasksTabViewModelAsync(id)),
                "tasksactivity" => ("WorkspaceTabs/_TasksActivity", "Tasks and activity", await LoadTasksActivityTabViewModelAsync(id)),
                "activitylog" => ("WorkspaceTabs/_ActivityLog", "Activity log", await LoadActivityLogTabViewModelAsync(id)),
                _ => (string.Empty, string.Empty, null)
            };

            if (string.IsNullOrWhiteSpace(result.Item1))
            {
                return NotFound();
            }

            if (result.Item3 is null)
            {
                return NotFound();
            }

            logger.LogInformation(
                "Client workspace tab loaded. Tab: {WorkspaceTab}; ClientCompanyId: {ClientCompanyId}; LoadedItems: {LoadedItems}; ElapsedMs: {ElapsedMs}; RequestId: {RequestId}",
                result.Item2,
                id,
                CountLoadedTabItems(normalizedTab, result.Item3),
                stopwatch.ElapsedMilliseconds,
                HttpContext.TraceIdentifier);

            return PartialView(result.Item1, result.Item3);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Client workspace tab failed. TabKey: {WorkspaceTabKey}; ClientCompanyId: {ClientCompanyId}; ResponseId: {ResponseId}; ElapsedMs: {ElapsedMs}; RequestId: {RequestId}",
                tabKey,
                id,
                responseId,
                stopwatch.ElapsedMilliseconds,
                HttpContext.TraceIdentifier);

            Response.StatusCode = StatusCodes.Status500InternalServerError;
            return PartialView("WorkspaceTabs/_TabError", new WorkspaceTabErrorViewModel
            {
                TabTitle = GetWorkspaceTabTitle(normalizedTab),
                RetryUrl = Url.Action(nameof(WorkspaceTab), new { id, tabKey, responseId }) ?? string.Empty,
                RequestId = HttpContext.TraceIdentifier
            });
        }
    }

    [HttpGet("Workspace/{id:int}/Counts")]
    public async Task<IActionResult> WorkspaceCounts(int id)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var counts = await LoadWorkspaceCollectionCountsAsync(id);
            if (counts is null)
            {
                return NotFound();
            }

            logger.LogInformation(
                "Client workspace deferred tab counts loaded. ClientCompanyId: {ClientCompanyId}; Responses: {ResponseCount}; Documents: {DocumentCount}; Notes: {NoteCount}; Transcripts: {TranscriptCount}; AI drafts: {AiDraftCount}; KnowledgeGaps: {KnowledgeGapCount}; OpenGaps: {OpenGapCount}; SwotItems: {SwotCount}; UseCases: {UseCaseCount}; RoadmapItems: {RoadmapCount}; Reports: {ReportCount}; OpenTasks: {OpenTaskCount}; ActivityLogs: {ActivityLogCount}; LatestActivityType: {LatestActivityType}; LatestActivityAt: {LatestActivityAt}; ElapsedMs: {ElapsedMs}; RequestId: {RequestId}",
                id,
                counts.AssessmentResponseCount,
                counts.DocumentCount,
                counts.NoteCount,
                counts.TranscriptCount,
                counts.AiDraftCount,
                counts.KnowledgeGapCount,
                counts.OpenGapCount,
                counts.SwotCount,
                counts.UseCaseCount,
                counts.RoadmapCount,
                counts.ReportCount,
                counts.OpenTaskCount,
                counts.ActivityLogCount,
                counts.LatestActivityType ?? "(none)",
                counts.LatestActivityCreatedAt,
                stopwatch.ElapsedMilliseconds,
                HttpContext.TraceIdentifier);

            return Json(new
            {
                assessmentResponseCount = counts.AssessmentResponseCount,
                documentCount = counts.DocumentCount,
                noteTranscriptCount = counts.NoteCount + counts.TranscriptCount,
                noteCount = counts.NoteCount,
                transcriptCount = counts.TranscriptCount,
                aiDraftCount = counts.AiDraftCount,
                knowledgeGapCount = counts.KnowledgeGapCount,
                openGapCount = counts.OpenGapCount,
                swotCount = counts.SwotCount,
                useCaseCount = counts.UseCaseCount,
                roadmapCount = counts.RoadmapCount,
                reportCount = counts.ReportCount,
                openTaskCount = counts.OpenTaskCount,
                activityLogCount = counts.ActivityLogCount
            });
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Client workspace deferred tab counts failed. ClientCompanyId: {ClientCompanyId}; ElapsedMs: {ElapsedMs}; RequestId: {RequestId}",
                id,
                stopwatch.ElapsedMilliseconds,
                HttpContext.TraceIdentifier);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    [HttpGet("Workspace/{clientId:int}/AssessmentResponses/{responseId:int}")]
    public async Task<IActionResult> AssessmentResponseDetails(int clientId, int responseId)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var viewModel = await LoadAssessmentResponseDetailsViewModelAsync(clientId, responseId);
            if (viewModel is null)
            {
                return NotFound();
            }

            logger.LogInformation(
                "Assessment response details loaded. ClientCompanyId: {ClientCompanyId}; ResponseId: {ResponseId}; Answers: {AnswerCount}; ElapsedMs: {ElapsedMs}; RequestId: {RequestId}",
                clientId,
                responseId,
                viewModel.SelectedAnswerCount,
                stopwatch.ElapsedMilliseconds,
                HttpContext.TraceIdentifier);

            return PartialView("WorkspaceTabs/_AssessmentResponseDetails", viewModel);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Assessment response details failed. ClientCompanyId: {ClientCompanyId}; ResponseId: {ResponseId}; ElapsedMs: {ElapsedMs}; RequestId: {RequestId}",
                clientId,
                responseId,
                stopwatch.ElapsedMilliseconds,
                HttpContext.TraceIdentifier);

            Response.StatusCode = StatusCodes.Status500InternalServerError;
            return PartialView("WorkspaceTabs/_TabError", new WorkspaceTabErrorViewModel
            {
                TabTitle = "Assessment response details",
                RetryUrl = Url.Action(nameof(AssessmentResponseDetails), new { clientId, responseId }) ?? string.Empty,
                RequestId = HttpContext.TraceIdentifier
            });
        }
    }

    private async Task<ClientWorkspaceViewModel?> LoadWorkspaceShellViewModelAsync(
        int id,
        int? responseId,
        string? activeTab = null,
        int? scrollY = null)
    {
        var stopwatch = Stopwatch.StartNew();
        var stepStopwatch = Stopwatch.StartNew();
        var activePaneKey = GetWorkspacePaneKey(activeTab, responseId);
        var activeTabKey = GetWorkspaceCanonicalTabKey(activePaneKey, responseId);
        var client = await LoadClientSummaryAsync(id);
        if (client is null)
        {
            return null;
        }

        logger.LogInformation(
            "Client workspace base client loaded. ClientCompanyId: {ClientCompanyId}; ElapsedMs: {ElapsedMs}; RequestId: {RequestId}",
            id,
            stepStopwatch.ElapsedMilliseconds,
            HttpContext.TraceIdentifier);
        stepStopwatch.Restart();

        client.WorkflowSteps = await LoadWorkflowStepsAsync(id);
        logger.LogInformation(
            "Client workspace timeline query loaded. ClientCompanyId: {ClientCompanyId}; WorkflowSteps: {WorkflowStepCount}; ElapsedMs: {ElapsedMs}; RequestId: {RequestId}",
            id,
            client.WorkflowSteps.Count,
            stepStopwatch.ElapsedMilliseconds,
            HttpContext.TraceIdentifier);
        stepStopwatch.Restart();

        var latestAssessment = await LoadLatestAssessmentAsync(id);

        if (latestAssessment is not null)
        {
            client.ReadinessAssessments.Add(latestAssessment);
        }

        logger.LogInformation(
            "Client workspace assessment summary loaded. ClientCompanyId: {ClientCompanyId}; LatestAssessmentId: {LatestAssessmentId}; FormStatus: {FormStatus}; ElapsedMs: {ElapsedMs}; RequestId: {RequestId}",
            id,
            latestAssessment?.Id,
            latestAssessment?.FormStatus,
            stepStopwatch.ElapsedMilliseconds,
            HttpContext.TraceIdentifier);
        stepStopwatch.Restart();

        var assessmentResponses = latestAssessment is null
            ? []
            : await LoadAssessmentResponseSummariesAsync(latestAssessment.Id);
        var assessmentResponseCount = assessmentResponses.Count;
        var latestResponse = assessmentResponses
            .Where(response => response.Status != AssessmentResponseStatus.Ignored)
            .OrderByDescending(response => response.ReceivedAt)
            .ThenByDescending(response => response.ResponseNumber)
            .FirstOrDefault();
        var latestAnsweredResponse = assessmentResponses
            .Where(response => response.Status != AssessmentResponseStatus.Ignored && response.AnswerCount > 0)
            .OrderByDescending(response => response.ReceivedAt)
            .ThenByDescending(response => response.ResponseNumber)
            .FirstOrDefault();

        logger.LogInformation(
            "Client workspace latest response summary loaded. ClientCompanyId: {ClientCompanyId}; ResponseCount: {ResponseCount}; LatestResponseId: {LatestResponseId}; LatestAnsweredResponseId: {LatestAnsweredResponseId}; ElapsedMs: {ElapsedMs}; RequestId: {RequestId}",
            id,
            assessmentResponseCount,
            latestResponse?.Id,
            latestAnsweredResponse?.Id,
            stepStopwatch.ElapsedMilliseconds,
            HttpContext.TraceIdentifier);
        stepStopwatch.Restart();

        ApplyResponseAwareWorkflow(client, latestAnsweredResponse);
        logger.LogInformation(
            "Client workspace timeline/stage calculation completed. ClientCompanyId: {ClientCompanyId}; CurrentStage: {CurrentStage}; WorkflowSteps: {WorkflowStepCount}; ElapsedMs: {ElapsedMs}; RequestId: {RequestId}",
            id,
            client.CurrentStage,
            client.WorkflowSteps.Count,
            stepStopwatch.ElapsedMilliseconds,
            HttpContext.TraceIdentifier);
        stepStopwatch.Restart();

        var latestReport = await LoadLatestReportSummaryAsync(id, includeSections: false);
        var latestScore = await LoadLatestReadinessScoreAsync(id);

        logger.LogInformation(
            "Client workspace overview data loaded. ClientCompanyId: {ClientCompanyId}; LatestReportId: {LatestReportId}; LatestReportStatus: {LatestReportStatus}; LatestScoreId: {LatestScoreId}; ElapsedMs: {ElapsedMs}; RequestId: {RequestId}",
            id,
            latestReport?.Id,
            latestReport?.ReportStatus,
            latestScore?.Id,
            stepStopwatch.ElapsedMilliseconds,
            HttpContext.TraceIdentifier);
        stepStopwatch.Restart();

        logger.LogInformation(
            "Client workspace tab counts deferred. ClientCompanyId: {ClientCompanyId}; CountsUrl: {CountsUrl}; ElapsedMs: {ElapsedMs}; RequestId: {RequestId}",
            id,
            Url.Action(nameof(WorkspaceCounts), new { id }) ?? string.Empty,
            stepStopwatch.ElapsedMilliseconds,
            HttpContext.TraceIdentifier);

        logger.LogInformation(
            "Client workspace shell summaries loaded. ClientCompanyId: {ClientCompanyId}; Responses: {ResponseCount}; ElapsedMs: {ElapsedMs}; RequestId: {RequestId}",
            id,
            assessmentResponseCount,
            stopwatch.ElapsedMilliseconds,
            HttpContext.TraceIdentifier);

        return new ClientWorkspaceViewModel
        {
            Client = client,
            LatestAssessment = latestAssessment,
            LatestAssessmentResponse = latestResponse,
            LatestReport = latestReport,
            LatestScore = latestScore,
            ActiveWorkspaceTab = activePaneKey,
            ActiveWorkspaceTabKey = activeTabKey,
            RequestedResponseId = responseId,
            ReturnScrollY = scrollY is >= 0 ? scrollY : null,
            AssessmentResponseCount = assessmentResponseCount,
            DocumentCount = 0,
            NoteCount = 0,
            TranscriptCount = 0,
            AiDraftCount = 0,
            KnowledgeGapCount = 0,
            GapCount = 0,
            OpenGapCount = 0,
            SwotCount = 0,
            UseCaseCount = 0,
            RoadmapCount = 0,
            ReportCount = 0,
            OpenTaskCount = 0,
            ActivityLogCount = 0
        };
    }

    private async Task<ClientWorkspaceViewModel?> LoadAssessmentTabViewModelAsync(int id, int? responseId)
    {
        var client = await LoadClientSummaryAsync(id);
        if (client is null)
        {
            return null;
        }

        var latestAssessment = await LoadLatestAssessmentAsync(id);
        var readinessFormSettings = await context.ReadinessFormSettings
            .AsNoTracking()
            .Where(settings => settings.IsActive)
            .OrderByDescending(settings => settings.LastModifiedAt ?? settings.CreatedAt)
            .FirstOrDefaultAsync();

        var assessmentResponses = latestAssessment is null
            ? []
            : await LoadAssessmentResponseSummariesAsync(latestAssessment.Id);

        var selectedAssessmentResponse = responseId.HasValue
            ? assessmentResponses.FirstOrDefault(response => response.Id == responseId.Value)
            : assessmentResponses
                .Where(response => response.Status != AssessmentResponseStatus.Ignored)
                .OrderByDescending(response => response.ReceivedAt)
                .ThenByDescending(response => response.ResponseNumber)
                .FirstOrDefault();

        var selectedAnswers = selectedAssessmentResponse is null
            ? []
            : await LoadAssessmentAnswersAsync(selectedAssessmentResponse.Id);

        if (selectedAssessmentResponse is not null)
        {
            selectedAssessmentResponse.Answers = selectedAnswers;
        }

        return new ClientWorkspaceViewModel
        {
            Client = client,
            LatestAssessment = latestAssessment,
            ReadinessFormSettings = readinessFormSettings,
            LatestAssessmentResponse = assessmentResponses
                .Where(response => response.Status != AssessmentResponseStatus.Ignored)
                .OrderByDescending(response => response.ReceivedAt)
                .ThenByDescending(response => response.ResponseNumber)
                .FirstOrDefault(),
            AssessmentResponseCount = assessmentResponses.Count,
            SelectedAnswerCount = selectedAnswers.Count,
            AssessmentResponses = assessmentResponses,
            SelectedAssessmentResponse = selectedAssessmentResponse,
            SelectedAnswersBySection = selectedAnswers
                .OrderBy(answer => answer.SectionName)
                .ThenBy(answer => answer.Id)
                .GroupBy(answer => answer.SectionName)
                .ToList()
        };
    }

    private async Task<ClientWorkspaceViewModel?> LoadAssessmentResponseDetailsViewModelAsync(int clientId, int responseId)
    {
        var response = await context.AssessmentResponses
            .AsNoTracking()
            .Where(item => item.Id == responseId && item.ReadinessAssessment!.ClientCompanyId == clientId)
            .Select(item => new AssessmentResponse
            {
                Id = item.Id,
                ReadinessAssessmentId = item.ReadinessAssessmentId,
                ResponseNumber = item.ResponseNumber,
                ResponseLabel = item.ResponseLabel,
                Source = item.Source,
                ExternalResponseId = item.ExternalResponseId,
                ClientToken = item.ClientToken,
                SubmittedAt = item.SubmittedAt,
                ReceivedAt = item.ReceivedAt,
                AnswerCount = item.AnswerCount,
                Status = item.Status,
                CreatedAt = item.CreatedAt,
                LastModifiedAt = item.LastModifiedAt
            })
            .FirstOrDefaultAsync();

        if (response is null)
        {
            return null;
        }

        var assessment = await context.ReadinessAssessments
            .AsNoTracking()
            .Where(item => item.Id == response.ReadinessAssessmentId)
            .Select(item => new ReadinessAssessment
            {
                Id = item.Id,
                ClientCompanyId = item.ClientCompanyId,
                ClientToken = item.ClientToken
            })
            .FirstOrDefaultAsync();

        var selectedAnswers = await LoadAssessmentAnswersAsync(response.Id);
        response.Answers = selectedAnswers;

        return new ClientWorkspaceViewModel
        {
            LatestAssessment = assessment,
            SelectedAssessmentResponse = response,
            SelectedAnswerCount = selectedAnswers.Count,
            SelectedAnswersBySection = selectedAnswers
                .OrderBy(answer => answer.SectionName)
                .ThenBy(answer => answer.Id)
                .GroupBy(answer => answer.SectionName)
                .ToList()
        };
    }

    private async Task<ClientWorkspaceViewModel?> LoadDocumentsTabViewModelAsync(int id)
    {
        var client = await LoadClientSummaryAsync(id);
        if (client is null)
        {
            return null;
        }

        client.Documents = await context.ClientDocuments
            .AsNoTracking()
            .Where(document => document.ClientCompanyId == id)
            .OrderByDescending(document => document.UploadedAt)
            .Take(20)
            .Select(document => new ClientDocument
            {
                Id = document.Id,
                ClientCompanyId = document.ClientCompanyId,
                FileName = document.FileName,
                FilePath = document.FilePath,
                DocumentType = document.DocumentType,
                Description = document.Description,
                UploadedAt = document.UploadedAt,
                UploadedBy = document.UploadedBy,
                AiSummary = Truncate(document.AiSummary, 700),
                KeyInsights = Truncate(document.KeyInsights, 700),
                UsedInReport = document.UsedInReport,
                CreatedAt = document.CreatedAt
            })
            .ToListAsync();

        return new ClientWorkspaceViewModel
        {
            Client = client,
            DocumentCount = await context.ClientDocuments.AsNoTracking().CountAsync(item => item.ClientCompanyId == id)
        };
    }

    private async Task<ClientWorkspaceViewModel?> LoadNotesTranscriptsTabViewModelAsync(int id)
    {
        var client = await LoadClientSummaryAsync(id);
        if (client is null)
        {
            return null;
        }

        client.Notes = await context.ConsultantNotes
            .AsNoTracking()
            .Where(note => note.ClientCompanyId == id)
            .OrderByDescending(note => note.CreatedAt)
            .Take(20)
            .Select(note => new ConsultantNote
            {
                Id = note.Id,
                ClientCompanyId = note.ClientCompanyId,
                NoteTitle = note.NoteTitle,
                NoteText = Truncate(note.NoteText, 700) ?? string.Empty,
                NoteType = note.NoteType,
                CreatedAt = note.CreatedAt,
                CreatedBy = note.CreatedBy,
                LastModifiedAt = note.LastModifiedAt
            })
            .ToListAsync();
        client.MeetingTranscripts = await context.MeetingTranscripts
            .AsNoTracking()
            .Where(transcript => transcript.ClientCompanyId == id)
            .OrderByDescending(transcript => transcript.SessionDate)
            .Take(20)
            .Select(transcript => new MeetingTranscript
            {
                Id = transcript.Id,
                ClientCompanyId = transcript.ClientCompanyId,
                SessionTitle = transcript.SessionTitle,
                SessionDate = transcript.SessionDate,
                TranscriptText = string.Empty,
                Summary = Truncate(transcript.Summary, 700),
                KeyDecisions = Truncate(transcript.KeyDecisions, 500),
                FollowUpQuestions = Truncate(transcript.FollowUpQuestions, 500),
                CreatedAt = transcript.CreatedAt,
                CreatedBy = transcript.CreatedBy
            })
            .ToListAsync();

        return new ClientWorkspaceViewModel
        {
            Client = client,
            NoteCount = await context.ConsultantNotes.AsNoTracking().CountAsync(item => item.ClientCompanyId == id),
            TranscriptCount = await context.MeetingTranscripts.AsNoTracking().CountAsync(item => item.ClientCompanyId == id)
        };
    }

    private async Task<ClientWorkspaceViewModel?> LoadKnowledgeGapAnalysisTabViewModelAsync(int id)
    {
        var client = await LoadClientSummaryAsync(id);
        if (client is null)
        {
            return null;
        }

        var knowledgeGapItems = await context.KnowledgeGapItems
            .AsNoTracking()
            .Where(item => item.ClientCompanyId == id)
            .OrderBy(item => item.Status == KnowledgeGapStatus.Approved)
            .ThenByDescending(item => item.Priority)
            .ThenByDescending(item => item.CreatedAt)
            .Take(80)
            .Select(item => new KnowledgeGapItem
            {
                Id = item.Id,
                ClientCompanyId = item.ClientCompanyId,
                AssessmentResponseId = item.AssessmentResponseId,
                GapArea = item.GapArea,
                MissingInformation = item.MissingInformation,
                WhyItMatters = item.WhyItMatters,
                FollowUpQuestion = item.FollowUpQuestion,
                SuggestedEvidence = item.SuggestedEvidence,
                Priority = item.Priority,
                Status = item.Status,
                CreatedAt = item.CreatedAt,
                LastModifiedAt = item.LastModifiedAt,
                ApprovedAt = item.ApprovedAt,
                ApprovedBy = item.ApprovedBy
            })
            .ToListAsync();

        client.KnowledgeGapItems = knowledgeGapItems;

        var sources = await LoadAIOutputSourcesAsync(id, AIOutputType.KnowledgeGap);
        var latestKnowledgeGapOutput = await context.AIAnalysisOutputs
            .AsNoTracking()
            .Where(output => output.ClientCompanyId == id && output.AnalysisType == AnalysisType.KnowledgeGapAnalysis)
            .OrderByDescending(output => output.VersionNumber)
            .ThenByDescending(output => output.CreatedAt)
            .Select(output => new AIAnalysisOutput
            {
                Id = output.Id,
                ClientCompanyId = output.ClientCompanyId,
                AnalysisType = output.AnalysisType,
                Title = output.Title,
                InputSummary = output.InputSummary,
                OutputContent = output.OutputContent,
                Status = output.Status,
                VersionNumber = output.VersionNumber,
                GeneratedAt = output.GeneratedAt,
                GeneratedBy = output.GeneratedBy,
                ApprovedAt = output.ApprovedAt,
                ApprovedBy = output.ApprovedBy,
                CreatedAt = output.CreatedAt,
                LastModifiedAt = output.LastModifiedAt
            })
            .FirstOrDefaultAsync();

        return new ClientWorkspaceViewModel
        {
            Client = client,
            LatestAnalysisOutputs = latestKnowledgeGapOutput is null ? [] : [latestKnowledgeGapOutput],
            KnowledgeGapItems = knowledgeGapItems,
            KnowledgeGapCount = await context.KnowledgeGapItems.AsNoTracking().CountAsync(item => item.ClientCompanyId == id),
            AIOutputSources = sources
        };
    }

    private async Task<ClientWorkspaceViewModel?> LoadCompanySummaryTabViewModelAsync(int id)
    {
        var client = await LoadClientSummaryAsync(id);
        if (client is null)
        {
            return null;
        }

        var latestSummary = await context.AIAnalysisOutputs
            .AsNoTracking()
            .Where(output => output.ClientCompanyId == id && output.AnalysisType == AnalysisType.CompanySummary)
            .OrderByDescending(output => output.VersionNumber)
            .ThenByDescending(output => output.CreatedAt)
            .Select(output => new AIAnalysisOutput
            {
                Id = output.Id,
                ClientCompanyId = output.ClientCompanyId,
                AnalysisType = output.AnalysisType,
                Title = output.Title,
                InputSummary = output.InputSummary,
                OutputContent = output.OutputContent,
                Status = output.Status,
                VersionNumber = output.VersionNumber,
                GeneratedAt = output.GeneratedAt,
                GeneratedBy = output.GeneratedBy,
                ApprovedAt = output.ApprovedAt,
                ApprovedBy = output.ApprovedBy,
                CreatedAt = output.CreatedAt,
                LastModifiedAt = output.LastModifiedAt
            })
            .FirstOrDefaultAsync();

        client.AnalysisOutputs = latestSummary is null ? [] : [latestSummary];

        var sources = await LoadAIOutputSourcesAsync(id, AIOutputType.CompanySummary, latestSummary?.Id);
        var approvedKnowledgeGaps = await context.KnowledgeGapItems
            .AsNoTracking()
            .Where(item => item.ClientCompanyId == id && item.Status == KnowledgeGapStatus.Approved)
            .OrderByDescending(item => item.ApprovedAt ?? item.LastModifiedAt ?? item.CreatedAt)
            .Take(10)
            .Select(item => new KnowledgeGapItem
            {
                Id = item.Id,
                ClientCompanyId = item.ClientCompanyId,
                GapArea = item.GapArea,
                MissingInformation = item.MissingInformation,
                Status = item.Status,
                ApprovedAt = item.ApprovedAt,
                ApprovedBy = item.ApprovedBy,
                CreatedAt = item.CreatedAt
            })
            .ToListAsync();

        return new ClientWorkspaceViewModel
        {
            Client = client,
            AiDraftCount = await context.AIAnalysisOutputs.AsNoTracking().CountAsync(item => item.ClientCompanyId == id),
            KnowledgeGapCount = await context.KnowledgeGapItems.AsNoTracking().CountAsync(item => item.ClientCompanyId == id),
            LatestAnalysisOutputs = client.AnalysisOutputs.ToList(),
            KnowledgeGapItems = approvedKnowledgeGaps,
            AIOutputSources = sources
        };
    }

    private async Task<ClientWorkspaceViewModel?> LoadAIDraftsTabViewModelAsync(int id)
    {
        var client = await LoadClientSummaryAsync(id);
        if (client is null)
        {
            return null;
        }

        client.AnalysisOutputs = [];
        foreach (var analysisType in Enum.GetValues<AnalysisType>())
        {
            var latestOutput = await context.AIAnalysisOutputs
                .AsNoTracking()
                .Where(output => output.ClientCompanyId == id && output.AnalysisType == analysisType)
                .OrderByDescending(output => output.VersionNumber)
                .ThenByDescending(output => output.CreatedAt)
                .Select(output => new AIAnalysisOutput
                {
                    Id = output.Id,
                    ClientCompanyId = output.ClientCompanyId,
                    AnalysisType = output.AnalysisType,
                    Title = output.Title,
                    InputSummary = output.InputSummary,
                    OutputContent = output.OutputContent,
                    Status = output.Status,
                    VersionNumber = output.VersionNumber,
                    GeneratedAt = output.GeneratedAt,
                    GeneratedBy = output.GeneratedBy,
                    CreatedAt = output.CreatedAt,
                    LastModifiedAt = output.LastModifiedAt
                })
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);

            if (latestOutput is not null)
            {
                client.AnalysisOutputs.Add(latestOutput);
            }
        }

        return new ClientWorkspaceViewModel
        {
            Client = client,
            AiDraftCount = await context.AIAnalysisOutputs.AsNoTracking().CountAsync(item => item.ClientCompanyId == id),
            LatestAnalysisOutputs = client.AnalysisOutputs
                .OrderByDescending(output => output.CreatedAt)
                .ToList(),
            AIOutputSources = await LoadAIOutputSourcesAsync(id)
        };
    }

    private async Task<ClientWorkspaceViewModel?> LoadGapAnalysisTabViewModelAsync(int id)
    {
        var client = await LoadClientSummaryAsync(id);
        if (client is null)
        {
            return null;
        }

        client.GapAnalysisItems = await context.GapAnalysisItems
            .AsNoTracking()
            .Where(gap => gap.ClientCompanyId == id)
            .OrderByDescending(gap => gap.Severity)
            .ThenByDescending(gap => gap.CreatedAt)
            .Take(50)
            .ToListAsync();

        return new ClientWorkspaceViewModel
        {
            Client = client,
            GapCount = await context.GapAnalysisItems.AsNoTracking().CountAsync(item => item.ClientCompanyId == id),
            OpenGapCount = await context.GapAnalysisItems.AsNoTracking().CountAsync(item => item.ClientCompanyId == id && item.Status == GapStatus.Open)
        };
    }

    private async Task<ClientWorkspaceViewModel?> LoadSwotTabViewModelAsync(int id)
    {
        var client = await LoadClientSummaryAsync(id);
        if (client is null)
        {
            return null;
        }

        client.SwotItems = await context.SwotAnalysisItems
            .AsNoTracking()
            .Where(item => item.ClientCompanyId == id)
            .OrderBy(item => item.Category)
            .ThenByDescending(item => item.CreatedAt)
            .Take(60)
            .ToListAsync();

        return new ClientWorkspaceViewModel
        {
            Client = client,
            SwotCount = await context.SwotAnalysisItems.AsNoTracking().CountAsync(item => item.ClientCompanyId == id),
            SwotByCategory = client.SwotItems
                .OrderBy(item => item.Category)
                .GroupBy(item => item.Category)
                .ToList()
        };
    }

    private async Task<ClientWorkspaceViewModel?> LoadIndustryCompetitorsTabViewModelAsync(int id)
    {
        var client = await LoadClientSummaryAsync(id);
        if (client is null)
        {
            return null;
        }

        client.IndustryInsights = await context.IndustryInsights
            .AsNoTracking()
            .Where(item => item.ClientCompanyId == id)
            .OrderByDescending(item => item.CreatedAt)
            .Take(20)
            .ToListAsync();
        client.CompetitorInsights = await context.CompetitorInsights
            .AsNoTracking()
            .Where(item => item.ClientCompanyId == id)
            .OrderByDescending(item => item.CreatedAt)
            .Take(20)
            .ToListAsync();

        return new ClientWorkspaceViewModel
        {
            Client = client
        };
    }

    private async Task<ClientWorkspaceViewModel?> LoadUseCasesScoringTabViewModelAsync(int id)
    {
        var client = await LoadClientSummaryAsync(id);
        if (client is null)
        {
            return null;
        }

        client.UseCases = await context.AIUseCases
            .AsNoTracking()
            .Include(useCase => useCase.Score)
            .Where(useCase => useCase.ClientCompanyId == id)
            .OrderByDescending(useCase => useCase.Score == null ? 0 : useCase.Score.PriorityScore)
            .ThenBy(useCase => useCase.Title)
            .Take(50)
            .ToListAsync();

        return new ClientWorkspaceViewModel
        {
            Client = client,
            UseCaseCount = await context.AIUseCases.AsNoTracking().CountAsync(item => item.ClientCompanyId == id),
            RankedUseCases = client.UseCases
                .OrderByDescending(useCase => useCase.Score?.PriorityScore ?? 0)
                .ThenBy(useCase => useCase.Title)
                .ToList()
        };
    }

    private async Task<ClientWorkspaceViewModel?> LoadRoadmapTabViewModelAsync(int id)
    {
        var client = await LoadClientSummaryAsync(id);
        if (client is null)
        {
            return null;
        }

        client.RoadmapItems = await context.AIRoadmapItems
            .AsNoTracking()
            .Where(item => item.ClientCompanyId == id)
            .OrderBy(item => item.Phase)
            .ThenBy(item => item.CreatedAt)
            .Take(50)
            .ToListAsync();

        return new ClientWorkspaceViewModel
        {
            Client = client,
            RoadmapCount = await context.AIRoadmapItems.AsNoTracking().CountAsync(item => item.ClientCompanyId == id),
            RoadmapByPhase = client.RoadmapItems
                .OrderBy(item => item.Phase)
                .GroupBy(item => item.Phase)
                .ToList()
        };
    }

    private async Task<ClientWorkspaceViewModel?> LoadReportsTabViewModelAsync(int id)
    {
        var client = await LoadClientSummaryAsync(id);
        if (client is null)
        {
            return null;
        }

        var latestReport = await LoadLatestReportSummaryAsync(id, includeSections: true);
        if (latestReport is not null)
        {
            client.Reports.Add(latestReport);
        }

        return new ClientWorkspaceViewModel
        {
            Client = client,
            LatestReport = latestReport,
            ReportCount = await context.ClientReports.AsNoTracking().CountAsync(item => item.ClientCompanyId == id),
            ReportTemplateSections = await context.ReportTemplateSections
                .AsNoTracking()
                .Where(section => section.Status == ReportTemplateSectionStatus.Active)
                .OrderBy(section => section.SectionOrder)
                .ThenBy(section => section.Id)
                .Take(20)
                .ToListAsync()
        };
    }

    private async Task<ClientWorkspaceViewModel?> LoadTasksTabViewModelAsync(int id)
    {
        var client = await LoadClientSummaryAsync(id);
        if (client is null)
        {
            return null;
        }

        client.Tasks = await context.ClientTasks
            .AsNoTracking()
            .Where(task => task.ClientCompanyId == id && task.Status != ClientTaskStatus.Done)
            .OrderBy(task => task.DueDate ?? DateTime.MaxValue)
            .ThenByDescending(task => task.Priority)
            .Take(25)
            .ToListAsync();

        return new ClientWorkspaceViewModel
        {
            Client = client,
            OpenTaskCount = await context.ClientTasks.AsNoTracking().CountAsync(item => item.ClientCompanyId == id && item.Status != ClientTaskStatus.Done)
        };
    }

    private async Task<ClientWorkspaceViewModel?> LoadTasksActivityTabViewModelAsync(int id)
    {
        var client = await LoadClientSummaryAsync(id);
        if (client is null)
        {
            return null;
        }

        client.Tasks = await context.ClientTasks
            .AsNoTracking()
            .Where(task => task.ClientCompanyId == id && task.Status != ClientTaskStatus.Done)
            .OrderBy(task => task.DueDate ?? DateTime.MaxValue)
            .ThenByDescending(task => task.Priority)
            .Take(25)
            .ToListAsync();

        client.ActivityLogs = await context.ClientActivityLogs
            .AsNoTracking()
            .Where(activity => activity.ClientCompanyId == id)
            .OrderByDescending(activity => activity.CreatedAt)
            .ThenByDescending(activity => activity.Id)
            .Take(20)
            .ToListAsync();

        return new ClientWorkspaceViewModel
        {
            Client = client,
            OpenTaskCount = await context.ClientTasks.AsNoTracking().CountAsync(item => item.ClientCompanyId == id && item.Status != ClientTaskStatus.Done),
            ActivityLogCount = await context.ClientActivityLogs.AsNoTracking().CountAsync(item => item.ClientCompanyId == id)
        };
    }

    private async Task<ClientWorkspaceViewModel?> LoadActivityLogTabViewModelAsync(int id)
    {
        var client = await LoadClientSummaryAsync(id);
        if (client is null)
        {
            return null;
        }

        client.ActivityLogs = await context.ClientActivityLogs
            .AsNoTracking()
            .Where(activity => activity.ClientCompanyId == id)
            .OrderByDescending(activity => activity.CreatedAt)
            .Take(20)
            .ToListAsync();

        return new ClientWorkspaceViewModel
        {
            Client = client,
            ActivityLogCount = await context.ClientActivityLogs.AsNoTracking().CountAsync(item => item.ClientCompanyId == id)
        };
    }

    private async Task<List<AIOutputSource>> LoadAIOutputSourcesAsync(int clientId, AIOutputType? outputType = null, int? outputId = null)
    {
        var query = context.AIOutputSources
            .AsNoTracking()
            .Where(source => source.ClientCompanyId == clientId);

        if (outputType.HasValue)
        {
            query = query.Where(source => source.OutputType == outputType.Value);
        }

        if (outputId.HasValue)
        {
            query = query.Where(source => source.OutputId == null || source.OutputId == outputId.Value);
        }

        return await query
            .OrderByDescending(source => source.CreatedAt)
            .ThenByDescending(source => source.Id)
            .Take(40)
            .Select(source => new AIOutputSource
            {
                Id = source.Id,
                ClientCompanyId = source.ClientCompanyId,
                OutputType = source.OutputType,
                OutputId = source.OutputId,
                SourceType = source.SourceType,
                SourceCategory = source.SourceCategory,
                SourceLabel = source.SourceLabel,
                SourceReference = source.SourceReference,
                SourceUrl = source.SourceUrl,
                EvidenceText = source.EvidenceText,
                CreatedAt = source.CreatedAt
            })
            .ToListAsync();
    }

    private async Task<ClientCompany?> LoadClientSummaryAsync(int id)
    {
        return await context.ClientCompanies
            .AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new ClientCompany
            {
                Id = item.Id,
                CompanyName = item.CompanyName,
                Industry = item.Industry,
                WebsiteUrl = item.WebsiteUrl,
                Country = item.Country,
                Region = item.Region,
                CompanySizeRange = item.CompanySizeRange,
                RevenueRange = item.RevenueRange,
                BusinessModel = item.BusinessModel,
                ContactPersonName = item.ContactPersonName,
                ContactPersonEmail = item.ContactPersonEmail,
                ContactPersonPhone = item.ContactPersonPhone,
                ConsultingPackage = item.ConsultingPackage,
                AssignedConsultant = item.AssignedConsultant,
                Priority = item.Priority,
                CurrentStage = item.CurrentStage,
                OverallReadinessScore = item.OverallReadinessScore,
                KeyRisksSummary = item.KeyRisksSummary,
                NextAction = item.NextAction,
                CreatedAt = item.CreatedAt,
                CreatedBy = item.CreatedBy,
                LastModifiedAt = item.LastModifiedAt,
                LastModifiedBy = item.LastModifiedBy
            })
            .FirstOrDefaultAsync();
    }

    private async Task<List<ClientWorkflowStep>> LoadWorkflowStepsAsync(int id)
    {
        var steps = await context.ClientWorkflowSteps
            .AsNoTracking()
            .Where(step => step.ClientCompanyId == id)
            .OrderBy(step => step.DisplayOrder)
            .Select(step => new ClientWorkflowStep
            {
                Id = step.Id,
                ClientCompanyId = step.ClientCompanyId,
                StageName = step.StageName,
                DisplayOrder = step.DisplayOrder,
                Status = step.Status,
                CompletedAt = step.CompletedAt
            })
            .ToListAsync();

        return ApplyStakeholderWorkflow(steps, id);
    }

    private async Task<ReadinessAssessment?> LoadLatestAssessmentAsync(int id)
    {
        return await context.ReadinessAssessments
            .AsNoTracking()
            .Where(assessment => assessment.ClientCompanyId == id)
            .OrderByDescending(assessment => assessment.CreatedAt)
            .ThenByDescending(assessment => assessment.Id)
            .Select(assessment => new ReadinessAssessment
            {
                Id = assessment.Id,
                ClientCompanyId = assessment.ClientCompanyId,
                FormStatus = assessment.FormStatus,
                FormUrl = assessment.FormUrl,
                ClientToken = assessment.ClientToken,
                GeneratedFormUrl = assessment.GeneratedFormUrl,
                CustomFormUrl = assessment.CustomFormUrl,
                SentToEmail = assessment.SentToEmail,
                SentAt = assessment.SentAt,
                LastReminderSentAt = assessment.LastReminderSentAt,
                CompletedAt = assessment.CompletedAt,
                ImportedAt = assessment.ImportedAt,
                ResponseReceivedAt = assessment.ResponseReceivedAt,
                ExternalResponseId = assessment.ExternalResponseId,
                Summary = assessment.Summary,
                CreatedAt = assessment.CreatedAt,
                LastModifiedAt = assessment.LastModifiedAt
            })
            .FirstOrDefaultAsync();
    }

    private async Task<List<AssessmentResponse>> LoadAssessmentResponseSummariesAsync(int assessmentId)
    {
        return await context.AssessmentResponses
            .AsNoTracking()
            .Where(response => response.ReadinessAssessmentId == assessmentId)
            .OrderBy(response => response.ResponseNumber)
            .Select(response => new AssessmentResponse
            {
                Id = response.Id,
                ReadinessAssessmentId = response.ReadinessAssessmentId,
                ResponseNumber = response.ResponseNumber,
                ResponseLabel = response.ResponseLabel,
                Source = response.Source,
                ExternalResponseId = response.ExternalResponseId,
                ClientToken = response.ClientToken,
                SubmittedAt = response.SubmittedAt,
                ReceivedAt = response.ReceivedAt,
                AnswerCount = response.AnswerCount,
                Status = response.Status,
                CreatedAt = response.CreatedAt,
                LastModifiedAt = response.LastModifiedAt
            })
            .ToListAsync();
    }

    private async Task<AssessmentResponse?> LoadLatestAssessmentResponseAsync(int assessmentId)
    {
        return await context.AssessmentResponses
            .AsNoTracking()
            .Where(response => response.ReadinessAssessmentId == assessmentId && response.Status != AssessmentResponseStatus.Ignored)
            .OrderByDescending(response => response.ReceivedAt)
            .ThenByDescending(response => response.ResponseNumber)
            .Select(response => new AssessmentResponse
            {
                Id = response.Id,
                ReadinessAssessmentId = response.ReadinessAssessmentId,
                ResponseNumber = response.ResponseNumber,
                ResponseLabel = response.ResponseLabel,
                Source = response.Source,
                ExternalResponseId = response.ExternalResponseId,
                ClientToken = response.ClientToken,
                SubmittedAt = response.SubmittedAt,
                ReceivedAt = response.ReceivedAt,
                AnswerCount = response.AnswerCount,
                Status = response.Status,
                CreatedAt = response.CreatedAt,
                LastModifiedAt = response.LastModifiedAt
            })
            .FirstOrDefaultAsync();
    }

    private async Task<AssessmentResponse?> LoadLatestAnsweredAssessmentResponseAsync(int assessmentId)
    {
        return await context.AssessmentResponses
            .AsNoTracking()
            .Where(response => response.ReadinessAssessmentId == assessmentId && response.Status != AssessmentResponseStatus.Ignored && response.AnswerCount > 0)
            .OrderByDescending(response => response.ReceivedAt)
            .ThenByDescending(response => response.ResponseNumber)
            .Select(response => new AssessmentResponse
            {
                Id = response.Id,
                ReceivedAt = response.ReceivedAt,
                AnswerCount = response.AnswerCount,
                Status = response.Status
            })
            .FirstOrDefaultAsync();
    }

    private async Task<List<AssessmentAnswer>> LoadAssessmentAnswersAsync(int responseId)
    {
        return await context.AssessmentAnswers
            .AsNoTracking()
            .Where(answer => answer.AssessmentResponseId == responseId)
            .OrderBy(answer => answer.SectionName)
            .ThenBy(answer => answer.Id)
            .Select(answer => new AssessmentAnswer
            {
                Id = answer.Id,
                ReadinessAssessmentId = answer.ReadinessAssessmentId,
                AssessmentResponseId = answer.AssessmentResponseId,
                SectionName = answer.SectionName,
                QuestionText = answer.QuestionText,
                AnswerText = answer.AnswerText,
                AnswerType = answer.AnswerType,
                IsMandatory = answer.IsMandatory,
                CompletenessStatus = answer.CompletenessStatus,
                CreatedAt = answer.CreatedAt
            })
            .ToListAsync();
    }

    private async Task<ClientReport?> LoadLatestReportSummaryAsync(int id, bool includeSections)
    {
        var latestReport = await context.ClientReports
            .AsNoTracking()
            .Where(report => report.ClientCompanyId == id)
            .OrderByDescending(report => report.VersionNumber)
            .ThenByDescending(report => report.CreatedAt)
            .Select(report => new ClientReport
            {
                Id = report.Id,
                ClientCompanyId = report.ClientCompanyId,
                ReportTitle = report.ReportTitle,
                ReportStatus = report.ReportStatus,
                VersionNumber = report.VersionNumber,
                GeneratedAt = report.GeneratedAt,
                ReviewedAt = report.ReviewedAt,
                DeliveredAt = report.DeliveredAt,
                CreatedAt = report.CreatedAt,
                LastModifiedAt = report.LastModifiedAt
            })
            .FirstOrDefaultAsync();

        if (latestReport is not null && includeSections)
        {
            latestReport.Sections = await context.ReportSections
                .AsNoTracking()
                .Where(section => section.ClientReportId == latestReport.Id)
                .OrderBy(section => section.SectionOrder)
                .Select(section => new ReportSection
                {
                    Id = section.Id,
                    ClientReportId = section.ClientReportId,
                    SectionTitle = section.SectionTitle,
                    SectionOrder = section.SectionOrder,
                    SectionContent = section.SectionContent,
                    SectionStatus = section.SectionStatus,
                    ApprovedAt = section.ApprovedAt,
                    ApprovedBy = section.ApprovedBy,
                    SourceSummary = section.SourceSummary,
                    CreatedAt = section.CreatedAt,
                    LastModifiedAt = section.LastModifiedAt
                })
                .ToListAsync();
        }

        return latestReport;
    }

    private async Task<ReadinessScore?> LoadLatestReadinessScoreAsync(int id)
    {
        return await context.ReadinessScores
            .AsNoTracking()
            .Where(score => score.ClientCompanyId == id)
            .OrderByDescending(score => score.CreatedAt)
            .FirstOrDefaultAsync();
    }

    private async Task<WorkspaceCollectionCounts?> LoadWorkspaceCollectionCountsAsync(int id)
    {
        return await context.ClientCompanies
            .AsNoTracking()
            .Where(client => client.Id == id)
            .Select(client => new WorkspaceCollectionCounts(
                context.AssessmentResponses.Count(response =>
                    context.ReadinessAssessments
                        .Where(assessment => assessment.ClientCompanyId == client.Id)
                        .OrderByDescending(assessment => assessment.CreatedAt)
                        .ThenByDescending(assessment => assessment.Id)
                        .Select(assessment => assessment.Id)
                        .Take(1)
                        .Contains(response.ReadinessAssessmentId)),
                context.ClientDocuments.Count(item => item.ClientCompanyId == client.Id),
                context.ConsultantNotes.Count(item => item.ClientCompanyId == client.Id),
                context.MeetingTranscripts.Count(item => item.ClientCompanyId == client.Id),
                context.AIAnalysisOutputs.Count(item => item.ClientCompanyId == client.Id),
                context.KnowledgeGapItems.Count(item => item.ClientCompanyId == client.Id),
                context.GapAnalysisItems.Count(item => item.ClientCompanyId == client.Id),
                context.GapAnalysisItems.Count(item => item.ClientCompanyId == client.Id && item.Status == GapStatus.Open),
                context.SwotAnalysisItems.Count(item => item.ClientCompanyId == client.Id),
                context.AIUseCases.Count(item => item.ClientCompanyId == client.Id),
                context.AIRoadmapItems.Count(item => item.ClientCompanyId == client.Id),
                context.ClientReports.Count(item => item.ClientCompanyId == client.Id),
                context.ClientTasks.Count(item => item.ClientCompanyId == client.Id && item.Status != ClientTaskStatus.Done),
                context.ClientActivityLogs.Count(item => item.ClientCompanyId == client.Id),
                context.ClientActivityLogs
                    .Where(activity => activity.ClientCompanyId == client.Id)
                    .OrderByDescending(activity => activity.CreatedAt)
                    .ThenByDescending(activity => activity.Id)
                    .Select(activity => activity.ActivityType)
                    .FirstOrDefault(),
                context.ClientActivityLogs
                    .Where(activity => activity.ClientCompanyId == client.Id)
                    .OrderByDescending(activity => activity.CreatedAt)
                    .ThenByDescending(activity => activity.Id)
                    .Select(activity => (DateTime?)activity.CreatedAt)
                    .FirstOrDefault()))
            .SingleOrDefaultAsync();
    }

    private static ReportStatus GetLatestReportStatus(ClientCompany client)
    {
        return client.Reports
            .OrderByDescending(report => report.VersionNumber)
            .FirstOrDefault()?.ReportStatus ?? ReportStatus.NotStarted;
    }

    private static List<ClientWorkflowStep> ApplyStakeholderWorkflow(List<ClientWorkflowStep> existingSteps, int clientId)
    {
        if (existingSteps.Count == 0)
        {
            return StakeholderWorkflow.Stages
                .Select((stage, index) => new ClientWorkflowStep
                {
                    ClientCompanyId = clientId,
                    StageName = stage,
                    DisplayOrder = index + 1,
                    Status = index == 0 ? WorkflowStepStatus.Completed : WorkflowStepStatus.NotStarted,
                    CompletedAt = index == 0 ? DateTime.UtcNow : null
                })
                .ToList();
        }

        var mappedSteps = existingSteps
            .Select(step => new { Stage = StakeholderWorkflow.MapLegacyStageName(step.StageName), Step = step })
            .GroupBy(item => item.Stage, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(item => item.Step.Status == WorkflowStepStatus.Completed)
                    .ThenByDescending(item => item.Step.CompletedAt ?? DateTime.MinValue)
                    .ThenBy(item => item.Step.DisplayOrder)
                    .Select(item => item.Step)
                    .First(),
                StringComparer.OrdinalIgnoreCase);

        var nextSteps = new List<ClientWorkflowStep>(StakeholderWorkflow.Stages.Length);
        for (var index = 0; index < StakeholderWorkflow.Stages.Length; index++)
        {
            var stage = StakeholderWorkflow.Stages[index];
            if (mappedSteps.TryGetValue(stage, out var existing))
            {
                existing.StageName = stage;
                existing.DisplayOrder = index + 1;
                nextSteps.Add(existing);
                continue;
            }

            nextSteps.Add(new ClientWorkflowStep
            {
                ClientCompanyId = clientId,
                StageName = stage,
                DisplayOrder = index + 1,
                Status = WorkflowStepStatus.NotStarted
            });
        }

        return nextSteps;
    }

    private static string NormalizeWorkspaceTabKey(string? tabKey)
    {
        if (string.IsNullOrWhiteSpace(tabKey))
        {
            return "overview";
        }

        return tabKey
            .Trim()
            .TrimStart('#')
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .ToLowerInvariant();
    }

    private static string GetWorkspacePaneKey(string? tabKey, int? responseId)
    {
        var hasRequestedTab = !string.IsNullOrWhiteSpace(tabKey);
        var normalizedTab = NormalizeWorkspaceTabKey(tabKey);
        if (!hasRequestedTab && normalizedTab == "overview" && responseId.HasValue)
        {
            normalizedTab = "assessmentanswers";
        }

        return normalizedTab switch
        {
            "overview" => "overview",
            "assessment" or "assessmentanswers" => "assessment",
            "documents" => "documents",
            "notes" or "notestranscripts" => "notes",
            "knowledgegap" or "knowledgegapanalysis" or "gap" or "gapanalysis" => "knowledgegap",
            "companysummary" or "summary" => "companysummary",
            "analysis" or "aidrafts" => "companysummary",
            "swot" => "swot",
            "insights" or "industrycompetitors" => "insights",
            "usecases" or "usecasesscoring" => "usecases",
            "roadmap" => "roadmap",
            "reports" => "reports",
            "tasks" or "tasksactivity" => "tasks",
            "activity" or "activitylog" => "activity",
            _ => responseId.HasValue ? "assessment" : "overview"
        };
    }

    private static string GetWorkspaceCanonicalTabKey(string? tabKey, int? responseId)
    {
        return GetWorkspacePaneKey(tabKey, responseId) switch
        {
            "assessment" => "assessment-answers",
            "documents" => "documents",
            "notes" => "notes-transcripts",
            "knowledgegap" => "knowledge-gap-analysis",
            "companysummary" => "company-summary",
            "analysis" => "ai-drafts",
            "gap" => "gap-analysis",
            "swot" => "swot",
            "insights" => "industry-competitors",
            "usecases" => "use-cases-scoring",
            "roadmap" => "roadmap",
            "reports" => "reports",
            "tasks" => "tasks-activity",
            "activity" => "activity-log",
            _ => "overview"
        };
    }

    private static string GetWorkspaceTabTitle(string normalizedTab)
    {
        return normalizedTab switch
        {
            "overview" => "Overview",
            "assessmentanswers" => "Assessment answers",
            "documents" => "Documents",
            "notestranscripts" => "Notes and transcripts",
            "knowledgegapanalysis" => "Knowledge gap analysis",
            "companysummary" => "Company summary",
            "aidrafts" => "AI drafts",
            "gapanalysis" => "Gap analysis",
            "swot" => "SWOT",
            "industrycompetitors" => "Industry and competitors",
            "usecasesscoring" => "Use cases and scoring",
            "roadmap" => "Roadmap",
            "reports" => "Strategic report",
            "tasks" or "tasksactivity" => "Tasks and activity",
            "activitylog" => "Activity log",
            _ => "Workspace tab"
        };
    }

    private static int CountLoadedTabItems(string normalizedTab, ClientWorkspaceViewModel viewModel)
    {
        return normalizedTab switch
        {
            "assessmentanswers" => viewModel.AssessmentResponses.Count,
            "documents" => viewModel.Client.Documents.Count,
            "notestranscripts" => viewModel.Client.Notes.Count + viewModel.Client.MeetingTranscripts.Count,
            "knowledgegapanalysis" => viewModel.KnowledgeGapItems.Count,
            "companysummary" => viewModel.LatestAnalysisOutputs.Count,
            "aidrafts" => viewModel.LatestAnalysisOutputs.Count,
            "gapanalysis" => viewModel.Client.GapAnalysisItems.Count,
            "swot" => viewModel.Client.SwotItems.Count,
            "industrycompetitors" => viewModel.Client.IndustryInsights.Count + viewModel.Client.CompetitorInsights.Count,
            "usecasesscoring" => viewModel.RankedUseCases.Count,
            "roadmap" => viewModel.Client.RoadmapItems.Count,
            "reports" => viewModel.LatestReport?.Sections.Count ?? 0,
            "tasks" or "tasksactivity" => viewModel.Client.Tasks.Count + viewModel.Client.ActivityLogs.Count,
            "activitylog" => viewModel.Client.ActivityLogs.Count,
            _ => 0
        };
    }

    private static void ApplyResponseAwareWorkflow(ClientCompany client, AssessmentResponse? latestAnsweredResponse)
    {
        var formCompletedStep = client.WorkflowSteps.FirstOrDefault(step =>
            step.StageName == "Assessment Completed" ||
            step.StageName == "Form Completed");
        if (formCompletedStep is null)
        {
            return;
        }

        if (latestAnsweredResponse is null)
        {
            formCompletedStep.Status = WorkflowStepStatus.NotStarted;
            formCompletedStep.CompletedAt = null;
            return;
        }

        formCompletedStep.Status = WorkflowStepStatus.Completed;
        formCompletedStep.CompletedAt ??= latestAnsweredResponse.ReceivedAt;
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length <= maxLength)
        {
            return value;
        }

        return value[..maxLength] + "...";
    }

    private sealed record WorkspaceCollectionCounts(
        int AssessmentResponseCount,
        int DocumentCount,
        int NoteCount,
        int TranscriptCount,
        int AiDraftCount,
        int KnowledgeGapCount,
        int GapCount,
        int OpenGapCount,
        int SwotCount,
        int UseCaseCount,
        int RoadmapCount,
        int ReportCount,
        int OpenTaskCount,
        int ActivityLogCount,
        string? LatestActivityType,
        DateTime? LatestActivityCreatedAt);

    private static void AddDefaultWorkflow(ClientCompany client)
    {
        foreach (var (stage, index) in StakeholderWorkflow.Stages.Select((stage, index) => (stage, index)))
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
