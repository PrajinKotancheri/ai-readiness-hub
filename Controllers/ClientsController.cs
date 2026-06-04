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
    public async Task<IActionResult> Workspace(int id, int? responseId)
    {
        var totalStopwatch = Stopwatch.StartNew();
        try
        {
            var viewModel = await LoadWorkspaceShellViewModelAsync(id, responseId);
            if (viewModel is null)
            {
                return NotFound();
            }

            logger.LogInformation(
                "Client workspace shell loaded. ClientCompanyId: {ClientCompanyId}; ActiveTab: {ActiveTab}; Responses: {ResponseCount}; Documents: {DocumentCount}; Notes: {NoteCount}; Transcripts: {TranscriptCount}; AI drafts: {AiDraftCount}; OpenTasks: {OpenTaskCount}; ActivityLogs: {ActivityLogCount}; ElapsedMs: {ElapsedMs}; RequestId: {RequestId}",
                id,
                viewModel.ActiveWorkspaceTab,
                viewModel.AssessmentResponseCount,
                viewModel.DocumentCount,
                viewModel.NoteCount,
                viewModel.TranscriptCount,
                viewModel.AiDraftCount,
                viewModel.OpenTaskCount,
                viewModel.ActivityLogCount,
                totalStopwatch.ElapsedMilliseconds,
                HttpContext.TraceIdentifier);

            return View(viewModel);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Client workspace failed. ClientCompanyId: {ClientCompanyId}; ResponseId: {ResponseId}; ElapsedMs: {ElapsedMs}; RequestId: {RequestId}",
                id,
                responseId,
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
                "aidrafts" => ("WorkspaceTabs/_AIDrafts", "AI drafts", await LoadAIDraftsTabViewModelAsync(id)),
                "gapanalysis" => ("WorkspaceTabs/_GapAnalysis", "Gap analysis", await LoadGapAnalysisTabViewModelAsync(id)),
                "swot" => ("WorkspaceTabs/_Swot", "SWOT", await LoadSwotTabViewModelAsync(id)),
                "industrycompetitors" => ("WorkspaceTabs/_IndustryCompetitors", "Industry and competitors", await LoadIndustryCompetitorsTabViewModelAsync(id)),
                "usecasesscoring" => ("WorkspaceTabs/_UseCasesScoring", "Use cases and scoring", await LoadUseCasesScoringTabViewModelAsync(id)),
                "roadmap" => ("WorkspaceTabs/_Roadmap", "Roadmap", await LoadRoadmapTabViewModelAsync(id)),
                "reports" => ("WorkspaceTabs/_Reports", "Reports", await LoadReportsTabViewModelAsync(id)),
                "tasks" => ("WorkspaceTabs/_Tasks", "Tasks and follow-ups", await LoadTasksTabViewModelAsync(id)),
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

    private async Task<ClientWorkspaceViewModel?> LoadWorkspaceShellViewModelAsync(int id, int? responseId)
    {
        var stopwatch = Stopwatch.StartNew();
        var client = await LoadClientSummaryAsync(id);
        if (client is null)
        {
            return null;
        }

        logger.LogInformation(
            "Client workspace base client loaded. ClientCompanyId: {ClientCompanyId}; ElapsedMs: {ElapsedMs}; RequestId: {RequestId}",
            id,
            stopwatch.ElapsedMilliseconds,
            HttpContext.TraceIdentifier);

        client.WorkflowSteps = await LoadWorkflowStepsAsync(id);
        var latestAssessment = await LoadLatestAssessmentAsync(id);

        if (latestAssessment is not null)
        {
            client.ReadinessAssessments.Add(latestAssessment);
        }

        var assessmentResponseCount = latestAssessment is null
            ? 0
            : await context.AssessmentResponses
                .AsNoTracking()
                .Where(response => response.ReadinessAssessmentId == latestAssessment.Id)
                .CountAsync();

        var latestResponse = latestAssessment is null
            ? null
            : await LoadLatestAssessmentResponseAsync(latestAssessment.Id);
        var latestAnsweredResponse = latestAssessment is null
            ? null
            : await LoadLatestAnsweredAssessmentResponseAsync(latestAssessment.Id);

        ApplyResponseAwareWorkflow(client, latestAnsweredResponse);

        var latestReport = await LoadLatestReportSummaryAsync(id, includeSections: false);
        var latestScore = await LoadLatestReadinessScoreAsync(id);
        var counts = await LoadWorkspaceCollectionCountsAsync(id);

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
            ActiveWorkspaceTab = responseId.HasValue ? "assessment" : "overview",
            RequestedResponseId = responseId,
            AssessmentResponseCount = assessmentResponseCount,
            DocumentCount = counts.DocumentCount,
            NoteCount = counts.NoteCount,
            TranscriptCount = counts.TranscriptCount,
            AiDraftCount = counts.AiDraftCount,
            GapCount = counts.GapCount,
            OpenGapCount = counts.OpenGapCount,
            SwotCount = counts.SwotCount,
            UseCaseCount = counts.UseCaseCount,
            RoadmapCount = counts.RoadmapCount,
            ReportCount = counts.ReportCount,
            OpenTaskCount = counts.OpenTaskCount,
            ActivityLogCount = counts.ActivityLogCount
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
                .ToList()
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
            ReportCount = await context.ClientReports.AsNoTracking().CountAsync(item => item.ClientCompanyId == id)
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
        return await context.ClientWorkflowSteps
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
    }

    private async Task<ReadinessAssessment?> LoadLatestAssessmentAsync(int id)
    {
        return await context.ReadinessAssessments
            .AsNoTracking()
            .Where(assessment => assessment.ClientCompanyId == id)
            .OrderByDescending(assessment => assessment.CreatedAt)
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
        var latestReportId = await context.ClientReports
            .AsNoTracking()
            .Where(report => report.ClientCompanyId == id)
            .OrderByDescending(report => report.VersionNumber)
            .ThenByDescending(report => report.CreatedAt)
            .Select(report => (int?)report.Id)
            .FirstOrDefaultAsync();

        if (!latestReportId.HasValue)
        {
            return null;
        }

        var latestReport = await context.ClientReports
            .AsNoTracking()
            .Where(report => report.Id == latestReportId.Value)
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

    private async Task<WorkspaceCollectionCounts> LoadWorkspaceCollectionCountsAsync(int id)
    {
        return new WorkspaceCollectionCounts(
            DocumentCount: await context.ClientDocuments.AsNoTracking().CountAsync(item => item.ClientCompanyId == id),
            NoteCount: await context.ConsultantNotes.AsNoTracking().CountAsync(item => item.ClientCompanyId == id),
            TranscriptCount: await context.MeetingTranscripts.AsNoTracking().CountAsync(item => item.ClientCompanyId == id),
            AiDraftCount: await context.AIAnalysisOutputs.AsNoTracking().CountAsync(item => item.ClientCompanyId == id),
            GapCount: await context.GapAnalysisItems.AsNoTracking().CountAsync(item => item.ClientCompanyId == id),
            OpenGapCount: await context.GapAnalysisItems.AsNoTracking().CountAsync(item => item.ClientCompanyId == id && item.Status == GapStatus.Open),
            SwotCount: await context.SwotAnalysisItems.AsNoTracking().CountAsync(item => item.ClientCompanyId == id),
            UseCaseCount: await context.AIUseCases.AsNoTracking().CountAsync(item => item.ClientCompanyId == id),
            RoadmapCount: await context.AIRoadmapItems.AsNoTracking().CountAsync(item => item.ClientCompanyId == id),
            ReportCount: await context.ClientReports.AsNoTracking().CountAsync(item => item.ClientCompanyId == id),
            OpenTaskCount: await context.ClientTasks.AsNoTracking().CountAsync(item => item.ClientCompanyId == id && item.Status != ClientTaskStatus.Done),
            ActivityLogCount: await context.ClientActivityLogs.AsNoTracking().CountAsync(item => item.ClientCompanyId == id));
    }

    private static ReportStatus GetLatestReportStatus(ClientCompany client)
    {
        return client.Reports
            .OrderByDescending(report => report.VersionNumber)
            .FirstOrDefault()?.ReportStatus ?? ReportStatus.NotStarted;
    }

    private static string NormalizeWorkspaceTabKey(string tabKey)
    {
        return tabKey
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .Trim()
            .ToLowerInvariant();
    }

    private static string GetWorkspaceTabTitle(string normalizedTab)
    {
        return normalizedTab switch
        {
            "overview" => "Overview",
            "assessmentanswers" => "Assessment answers",
            "documents" => "Documents",
            "notestranscripts" => "Notes and transcripts",
            "aidrafts" => "AI drafts",
            "gapanalysis" => "Gap analysis",
            "swot" => "SWOT",
            "industrycompetitors" => "Industry and competitors",
            "usecasesscoring" => "Use cases and scoring",
            "roadmap" => "Roadmap",
            "reports" => "Reports",
            "tasks" => "Tasks and follow-ups",
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
            "aidrafts" => viewModel.LatestAnalysisOutputs.Count,
            "gapanalysis" => viewModel.Client.GapAnalysisItems.Count,
            "swot" => viewModel.Client.SwotItems.Count,
            "industrycompetitors" => viewModel.Client.IndustryInsights.Count + viewModel.Client.CompetitorInsights.Count,
            "usecasesscoring" => viewModel.RankedUseCases.Count,
            "roadmap" => viewModel.Client.RoadmapItems.Count,
            "reports" => viewModel.LatestReport?.Sections.Count ?? 0,
            "tasks" => viewModel.Client.Tasks.Count,
            "activitylog" => viewModel.Client.ActivityLogs.Count,
            _ => 0
        };
    }

    private static void ApplyResponseAwareWorkflow(ClientCompany client, AssessmentResponse? latestAnsweredResponse)
    {
        var formCompletedStep = client.WorkflowSteps.FirstOrDefault(step => step.StageName == "Form Completed");
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
        int DocumentCount,
        int NoteCount,
        int TranscriptCount,
        int AiDraftCount,
        int GapCount,
        int OpenGapCount,
        int SwotCount,
        int UseCaseCount,
        int RoadmapCount,
        int ReportCount,
        int OpenTaskCount,
        int ActivityLogCount);

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
