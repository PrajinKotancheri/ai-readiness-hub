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
            var viewModel = await LoadWorkspaceViewModelAsync(id, responseId);
            if (viewModel is null)
            {
                return NotFound();
            }

            logger.LogInformation(
                "Client workspace loaded. ClientCompanyId: {ClientCompanyId}; Responses: {ResponseCount}; SelectedAnswers: {SelectedAnswerCount}; Documents: {DocumentCount}; Notes: {NoteCount}; Transcripts: {TranscriptCount}; AI drafts: {AiDraftCount}; Activity logs shown: {ActivityLogShownCount}/{ActivityLogCount}; ElapsedMs: {ElapsedMs}; RequestId: {RequestId}",
                id,
                viewModel.AssessmentResponseCount,
                viewModel.SelectedAnswerCount,
                viewModel.DocumentCount,
                viewModel.NoteCount,
                viewModel.TranscriptCount,
                viewModel.AiDraftCount,
                viewModel.Client.ActivityLogs.Count,
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

    private async Task<ClientWorkspaceViewModel?> LoadWorkspaceViewModelAsync(int id, int? responseId)
    {
        var stopwatch = Stopwatch.StartNew();
        var client = await context.ClientCompanies
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

        if (client is null)
        {
            return null;
        }

        logger.LogInformation(
            "Client workspace base client loaded. ClientCompanyId: {ClientCompanyId}; ElapsedMs: {ElapsedMs}; RequestId: {RequestId}",
            id,
            stopwatch.ElapsedMilliseconds,
            HttpContext.TraceIdentifier);

        stopwatch.Restart();
        client.WorkflowSteps = await context.ClientWorkflowSteps
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

        var latestAssessment = await context.ReadinessAssessments
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

        if (latestAssessment is not null)
        {
            client.ReadinessAssessments.Add(latestAssessment);
        }

        var assessmentResponses = latestAssessment is null
            ? []
            : await context.AssessmentResponses
                .AsNoTracking()
                .Where(response => response.ReadinessAssessmentId == latestAssessment.Id)
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

        var selectedAssessmentResponse = responseId.HasValue
            ? assessmentResponses.FirstOrDefault(response => response.Id == responseId.Value)
            : assessmentResponses
                .Where(response => response.Status != AssessmentResponseStatus.Ignored)
                .OrderByDescending(response => response.ReceivedAt)
                .ThenByDescending(response => response.ResponseNumber)
                .FirstOrDefault();

        var selectedAnswers = selectedAssessmentResponse is null
            ? []
            : await context.AssessmentAnswers
                .AsNoTracking()
                .Where(answer => answer.AssessmentResponseId == selectedAssessmentResponse.Id)
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

        if (selectedAssessmentResponse is not null)
        {
            selectedAssessmentResponse.Answers = selectedAnswers;
        }

        ApplyResponseAwareWorkflow(client, assessmentResponses);

        logger.LogInformation(
            "Client workspace assessment loaded. ClientCompanyId: {ClientCompanyId}; Responses: {ResponseCount}; SelectedAnswers: {SelectedAnswerCount}; ElapsedMs: {ElapsedMs}; RequestId: {RequestId}",
            id,
            assessmentResponses.Count,
            selectedAnswers.Count,
            stopwatch.ElapsedMilliseconds,
            HttpContext.TraceIdentifier);

        stopwatch.Restart();
        var latestReportId = await context.ClientReports
            .AsNoTracking()
            .Where(report => report.ClientCompanyId == id)
            .OrderByDescending(report => report.VersionNumber)
            .ThenByDescending(report => report.CreatedAt)
            .Select(report => (int?)report.Id)
            .FirstOrDefaultAsync();

        var latestReport = latestReportId.HasValue
            ? await context.ClientReports
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
                .FirstOrDefaultAsync()
            : null;

        if (latestReport is not null)
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
            client.Reports.Add(latestReport);
        }

        var latestScore = await context.ReadinessScores
            .AsNoTracking()
            .Where(score => score.ClientCompanyId == id)
            .OrderByDescending(score => score.CreatedAt)
            .FirstOrDefaultAsync();
        if (latestScore is not null)
        {
            client.ReadinessScores.Add(latestScore);
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
                .FirstOrDefaultAsync();

            if (latestOutput is not null)
            {
                client.AnalysisOutputs.Add(latestOutput);
            }
        }

        logger.LogInformation(
            "Client workspace reports and AI drafts loaded. ClientCompanyId: {ClientCompanyId}; ReportsLoaded: {ReportCount}; AIDraftsLoaded: {AiDraftCount}; ElapsedMs: {ElapsedMs}; RequestId: {RequestId}",
            id,
            latestReport is null ? 0 : 1,
            client.AnalysisOutputs.Count,
            stopwatch.ElapsedMilliseconds,
            HttpContext.TraceIdentifier);

        stopwatch.Restart();
        var counts = new WorkspaceCollectionCounts(
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

        client.Documents = await context.ClientDocuments
            .AsNoTracking()
            .Where(document => document.ClientCompanyId == id)
            .OrderByDescending(document => document.UploadedAt)
            .Take(20)
            .ToListAsync();
        client.Notes = await context.ConsultantNotes
            .AsNoTracking()
            .Where(note => note.ClientCompanyId == id)
            .OrderByDescending(note => note.CreatedAt)
            .Take(10)
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
            .Take(10)
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
        client.GapAnalysisItems = await context.GapAnalysisItems
            .AsNoTracking()
            .Where(gap => gap.ClientCompanyId == id)
            .OrderByDescending(gap => gap.Severity)
            .ThenByDescending(gap => gap.CreatedAt)
            .Take(50)
            .ToListAsync();
        client.SwotItems = await context.SwotAnalysisItems
            .AsNoTracking()
            .Where(item => item.ClientCompanyId == id)
            .OrderBy(item => item.Category)
            .ThenByDescending(item => item.CreatedAt)
            .Take(40)
            .ToListAsync();
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
        client.UseCases = await context.AIUseCases
            .AsNoTracking()
            .Include(useCase => useCase.Score)
            .Where(useCase => useCase.ClientCompanyId == id)
            .OrderByDescending(useCase => useCase.Score == null ? 0 : useCase.Score.PriorityScore)
            .ThenBy(useCase => useCase.Title)
            .Take(50)
            .ToListAsync();
        client.RoadmapItems = await context.AIRoadmapItems
            .AsNoTracking()
            .Where(item => item.ClientCompanyId == id)
            .OrderBy(item => item.Phase)
            .ThenBy(item => item.CreatedAt)
            .Take(50)
            .ToListAsync();
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
            .Take(20)
            .ToListAsync();

        logger.LogInformation(
            "Client workspace related summaries loaded. ClientCompanyId: {ClientCompanyId}; DocumentsLoaded: {DocumentsLoaded}/{DocumentCount}; NotesLoaded: {NotesLoaded}/{NoteCount}; TranscriptsLoaded: {TranscriptsLoaded}/{TranscriptCount}; ActivityLogsLoaded: {ActivityLogsLoaded}/{ActivityLogCount}; ElapsedMs: {ElapsedMs}; RequestId: {RequestId}",
            id,
            client.Documents.Count,
            counts.DocumentCount,
            client.Notes.Count,
            counts.NoteCount,
            client.MeetingTranscripts.Count,
            counts.TranscriptCount,
            client.ActivityLogs.Count,
            counts.ActivityLogCount,
            stopwatch.ElapsedMilliseconds,
            HttpContext.TraceIdentifier);

        var readinessFormSettings = await context.ReadinessFormSettings
            .AsNoTracking()
            .Where(settings => settings.IsActive)
            .OrderByDescending(settings => settings.LastModifiedAt ?? settings.CreatedAt)
            .FirstOrDefaultAsync();

        return new ClientWorkspaceViewModel
        {
            Client = client,
            LatestAssessment = latestAssessment,
            ReadinessFormSettings = readinessFormSettings,
            LatestReport = latestReport,
            LatestScore = latestScore,
            AssessmentResponseCount = assessmentResponses.Count,
            SelectedAnswerCount = selectedAnswers.Count,
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
            ActivityLogCount = counts.ActivityLogCount,
            AssessmentResponses = assessmentResponses,
            SelectedAssessmentResponse = selectedAssessmentResponse,
            SelectedAnswersBySection = selectedAnswers
                .OrderBy(answer => answer.SectionName)
                .ThenBy(answer => answer.Id)
                .GroupBy(answer => answer.SectionName)
                .ToList(),
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
                .OrderByDescending(output => output.CreatedAt)
                .ToList()
        };
    }

    private static ReportStatus GetLatestReportStatus(ClientCompany client)
    {
        return client.Reports
            .OrderByDescending(report => report.VersionNumber)
            .FirstOrDefault()?.ReportStatus ?? ReportStatus.NotStarted;
    }

    private static void ApplyResponseAwareWorkflow(ClientCompany client, IReadOnlyCollection<AssessmentResponse> assessmentResponses)
    {
        var formCompletedStep = client.WorkflowSteps.FirstOrDefault(step => step.StageName == "Form Completed");
        if (formCompletedStep is null)
        {
            return;
        }

        var latestAnsweredResponse = assessmentResponses
            .Where(response => response.Status != AssessmentResponseStatus.Ignored && response.AnswerCount > 0)
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
