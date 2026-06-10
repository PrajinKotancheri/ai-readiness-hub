using AI_Readiness_Hub.Data;
using AI_Readiness_Hub.Models;
using AI_Readiness_Hub.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AI_Readiness_Hub.Services;

public interface IAIWorkspaceService
{
    Task<int> OpenAsync(int clientId, AIOutputType outputType, int? outputId);
    Task<AIWorkspaceViewModel> LoadAsync(int sessionId);
    Task RefineAsync(int sessionId, string currentDraft, string consultantFeedback);
    Task SaveDraftAsync(int sessionId, string currentDraft);
    Task ApproveFinalAsync(int sessionId, string currentDraft);
    Task CloseAsync(int sessionId);
}

public class AIWorkspaceService(
    ApplicationDbContext context,
    IAIContextBuilder contextBuilder,
    IAIProviderClient providerClient,
    IStructuredAIResponseParser parser,
    IConfiguration configuration,
    IOptions<AIOptions> options) : IAIWorkspaceService
{
    private readonly AIOptions options = options.Value;

    public async Task<int> OpenAsync(int clientId, AIOutputType outputType, int? outputId)
    {
        var existing = await context.AIWorkspaceSessions
            .AsNoTracking()
            .Where(session =>
                session.ClientCompanyId == clientId &&
                session.OutputType == outputType &&
                session.OutputId == outputId &&
                session.Status == AIWorkspaceStatus.Active)
            .OrderByDescending(session => session.LastModifiedAt ?? session.CreatedAt)
            .Select(session => (int?)session.Id)
            .FirstOrDefaultAsync();
        if (existing.HasValue)
        {
            return existing.Value;
        }

        var draft = await LoadCurrentDraftAsync(clientId, outputType, outputId);
        var title = await BuildTitleAsync(clientId, outputType, outputId);
        var session = new AIWorkspaceSession
        {
            ClientCompanyId = clientId,
            OutputType = outputType,
            OutputId = outputId,
            Title = title,
            Status = AIWorkspaceStatus.Active,
            Provider = options.ProviderKind,
            Model = options.Model,
            CreatedAt = DateTime.UtcNow,
            LastModifiedAt = DateTime.UtcNow
        };
        session.Messages.Add(new AIWorkspaceMessage
        {
            Role = AIWorkspaceMessageRole.System,
            MessageText = "You are assisting a consultant. Improve drafts according to consultant feedback and preserve source attribution.",
            DraftContentSnapshot = draft,
            CreatedAt = DateTime.UtcNow
        });
        session.Revisions.Add(new AIOutputRevision
        {
            ClientCompanyId = clientId,
            OutputType = outputType,
            OutputId = outputId,
            VersionNumber = 1,
            DraftContent = draft,
            Provider = options.ProviderKind,
            Model = options.Model,
            Status = AIOutputRevisionStatus.Draft,
            CreatedAt = DateTime.UtcNow
        });
        context.AIWorkspaceSessions.Add(session);
        AddActivity(clientId, "AI Workspace created", $"{title} opened for consultant refinement.");
        await context.SaveChangesAsync();
        return session.Id;
    }

    public async Task<AIWorkspaceViewModel> LoadAsync(int sessionId)
    {
        var session = await context.AIWorkspaceSessions
            .AsNoTracking()
            .Where(item => item.Id == sessionId)
            .Select(item => new AIWorkspaceSession
            {
                Id = item.Id,
                ClientCompanyId = item.ClientCompanyId,
                OutputType = item.OutputType,
                OutputId = item.OutputId,
                Title = item.Title,
                Status = item.Status,
                Provider = item.Provider,
                Model = item.Model,
                CreatedAt = item.CreatedAt,
                LastModifiedAt = item.LastModifiedAt,
                ApprovedAt = item.ApprovedAt,
                ApprovedBy = item.ApprovedBy
            })
            .FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("AI Workspace session was not found.");

        var clientName = await context.ClientCompanies
            .AsNoTracking()
            .Where(item => item.Id == session.ClientCompanyId)
            .Select(item => item.CompanyName)
            .FirstOrDefaultAsync() ?? "Client";

        var messages = await context.AIWorkspaceMessages
            .AsNoTracking()
            .Where(item => item.AIWorkspaceSessionId == sessionId)
            .OrderBy(item => item.CreatedAt)
            .ThenBy(item => item.Id)
            .ToListAsync();

        var revisions = await context.AIOutputRevisions
            .AsNoTracking()
            .Where(item => item.AIWorkspaceSessionId == sessionId)
            .OrderByDescending(item => item.VersionNumber)
            .ThenByDescending(item => item.CreatedAt)
            .Take(20)
            .ToListAsync();

        var currentDraft = revisions.FirstOrDefault()?.DraftContent ??
            await LoadCurrentDraftAsync(session.ClientCompanyId, session.OutputType, session.OutputId);

        var sourceCount = await context.AIOutputSources
            .AsNoTracking()
            .CountAsync(source =>
                source.ClientCompanyId == session.ClientCompanyId &&
                source.OutputType == session.OutputType &&
                (session.OutputId == null || source.OutputId == null || source.OutputId == session.OutputId));

        var warning = session.Provider == AIProviderKind.OpenAI && string.IsNullOrWhiteSpace(configuration["OPENAI_API_KEY"])
            ? "OpenAI provider is enabled but OPENAI_API_KEY is not configured."
            : null;

        return new AIWorkspaceViewModel
        {
            Session = session,
            ClientCompanyName = clientName,
            CurrentDraft = currentDraft,
            SourceCount = sourceCount,
            Messages = messages,
            Revisions = revisions,
            WarningMessage = warning
        };
    }

    public async Task RefineAsync(int sessionId, string currentDraft, string consultantFeedback)
    {
        if (string.IsNullOrWhiteSpace(consultantFeedback))
        {
            throw new InvalidOperationException("Please enter feedback before sending it to AI.");
        }

        var session = await context.AIWorkspaceSessions.FirstOrDefaultAsync(item => item.Id == sessionId)
            ?? throw new InvalidOperationException("AI Workspace session was not found.");
        var previousMessages = await context.AIWorkspaceMessages
            .AsNoTracking()
            .Where(item => item.AIWorkspaceSessionId == sessionId)
            .OrderBy(item => item.CreatedAt)
            .ThenBy(item => item.Id)
            .Take(20)
            .Select(item => $"{item.Role}: {item.MessageText}")
            .ToListAsync();

        var aiContext = await contextBuilder.BuildAsync(new AIContextRequest(
            session.ClientCompanyId,
            AIOperationNames.AIWorkspaceRefinement,
            currentDraft,
            consultantFeedback,
            string.Join(Environment.NewLine, previousMessages)));
        var request = new AIProviderRequest(
            AIOperationNames.AIWorkspaceRefinement,
            "You are assisting a consultant. Improve the existing draft according to feedback. Return valid JSON only.",
            aiContext.PromptText,
            aiContext.ContextText,
            AIJsonSchemas.GetSchemaName(AIOperationNames.AIWorkspaceRefinement),
            AIJsonSchemas.GetSchema(AIOperationNames.AIWorkspaceRefinement),
            currentDraft,
            consultantFeedback);

        var result = await providerClient.GenerateStructuredJsonAsync(request);
        if (!result.Succeeded || string.IsNullOrWhiteSpace(result.Content))
        {
            throw new InvalidOperationException(result.FriendlyMessage ?? "AI could not refine the draft. Please try again or switch to Mock provider.");
        }

        var parsed = parser.ParseRefinement(result.Content);
        context.AIWorkspaceMessages.Add(new AIWorkspaceMessage
        {
            AIWorkspaceSessionId = session.Id,
            Role = AIWorkspaceMessageRole.Consultant,
            MessageText = consultantFeedback.Trim(),
            DraftContentSnapshot = currentDraft,
            CreatedAt = DateTime.UtcNow
        });
        context.AIWorkspaceMessages.Add(new AIWorkspaceMessage
        {
            AIWorkspaceSessionId = session.Id,
            Role = AIWorkspaceMessageRole.Assistant,
            MessageText = parsed.SummaryOfChanges ?? "Draft refined for consultant review.",
            DraftContentSnapshot = parsed.ImprovedDraft,
            CreatedAt = DateTime.UtcNow
        });

        await AddRevisionAsync(session, parsed.ImprovedDraft, consultantFeedback, AIOutputRevisionStatus.InReview);
        session.LastModifiedAt = DateTime.UtcNow;
        AddActivity(session.ClientCompanyId, "AI Workspace feedback sent", $"{session.Title} refined using {result.Provider}.");
        await context.SaveChangesAsync();
    }

    public async Task SaveDraftAsync(int sessionId, string currentDraft)
    {
        var session = await context.AIWorkspaceSessions.FirstOrDefaultAsync(item => item.Id == sessionId)
            ?? throw new InvalidOperationException("AI Workspace session was not found.");
        await AddRevisionAsync(session, currentDraft, null, AIOutputRevisionStatus.Draft);
        session.Status = AIWorkspaceStatus.DraftSaved;
        session.LastModifiedAt = DateTime.UtcNow;
        await UpdateAnalysisOutputAsync(session, currentDraft, DraftStatus.ConsultantEdited);
        AddActivity(session.ClientCompanyId, "AI Workspace draft saved", $"{session.Title} draft saved.");
        await context.SaveChangesAsync();
    }

    public async Task ApproveFinalAsync(int sessionId, string currentDraft)
    {
        var session = await context.AIWorkspaceSessions.FirstOrDefaultAsync(item => item.Id == sessionId)
            ?? throw new InvalidOperationException("AI Workspace session was not found.");
        await AddRevisionAsync(session, currentDraft, null, AIOutputRevisionStatus.Approved);
        session.Status = AIWorkspaceStatus.Approved;
        session.ApprovedAt = DateTime.UtcNow;
        session.ApprovedBy = "Consultant";
        session.LastModifiedAt = DateTime.UtcNow;
        await UpdateAnalysisOutputAsync(session, currentDraft, DraftStatus.Approved);
        AddActivity(session.ClientCompanyId, "AI Workspace final approved", $"{session.Title} approved.");
        await context.SaveChangesAsync();
    }

    public async Task CloseAsync(int sessionId)
    {
        var session = await context.AIWorkspaceSessions.FirstOrDefaultAsync(item => item.Id == sessionId)
            ?? throw new InvalidOperationException("AI Workspace session was not found.");
        session.Status = AIWorkspaceStatus.Closed;
        session.LastModifiedAt = DateTime.UtcNow;
        AddActivity(session.ClientCompanyId, "AI Workspace closed", $"{session.Title} closed.");
        await context.SaveChangesAsync();
    }

    private async Task AddRevisionAsync(AIWorkspaceSession session, string currentDraft, string? feedback, AIOutputRevisionStatus status)
    {
        var version = await context.AIOutputRevisions
            .Where(item => item.AIWorkspaceSessionId == session.Id)
            .Select(item => (int?)item.VersionNumber)
            .MaxAsync() ?? 0;
        context.AIOutputRevisions.Add(new AIOutputRevision
        {
            ClientCompanyId = session.ClientCompanyId,
            OutputType = session.OutputType,
            OutputId = session.OutputId,
            AIWorkspaceSessionId = session.Id,
            VersionNumber = version + 1,
            DraftContent = currentDraft,
            ConsultantFeedback = feedback,
            Provider = options.ProviderKind,
            Model = options.Model,
            Status = status,
            CreatedAt = DateTime.UtcNow,
            ApprovedAt = status == AIOutputRevisionStatus.Approved ? DateTime.UtcNow : null,
            ApprovedBy = status == AIOutputRevisionStatus.Approved ? "Consultant" : null
        });
    }

    private async Task UpdateAnalysisOutputAsync(AIWorkspaceSession session, string content, DraftStatus status)
    {
        if (session.OutputId is null)
        {
            return;
        }

        var output = await context.AIAnalysisOutputs.FirstOrDefaultAsync(item => item.Id == session.OutputId.Value);
        if (output is null)
        {
            return;
        }

        output.OutputContent = content;
        output.Status = status;
        output.LastModifiedAt = DateTime.UtcNow;
        if (status == DraftStatus.Approved)
        {
            output.ApprovedAt = DateTime.UtcNow;
            output.ApprovedBy = "Consultant";
        }
    }

    private async Task<string> LoadCurrentDraftAsync(int clientId, AIOutputType outputType, int? outputId)
    {
        if (outputId.HasValue)
        {
            var output = await context.AIAnalysisOutputs
                .AsNoTracking()
                .Where(item => item.Id == outputId.Value && item.ClientCompanyId == clientId)
                .Select(item => item.OutputContent)
                .FirstOrDefaultAsync();
            if (!string.IsNullOrWhiteSpace(output))
            {
                return output;
            }
        }

        return outputType switch
        {
            AIOutputType.KnowledgeGap => await BuildKnowledgeGapDraftAsync(clientId),
            AIOutputType.CompanySummary => await LoadLatestAnalysisOutputAsync(clientId, AnalysisType.CompanySummary),
            AIOutputType.IndustryAnalysis => await LoadLatestAnalysisOutputAsync(clientId, AnalysisType.IndustryAnalysis),
            AIOutputType.CompetitorAnalysis => await LoadLatestAnalysisOutputAsync(clientId, AnalysisType.CompetitorInsights),
            AIOutputType.SWOT => await LoadLatestAnalysisOutputAsync(clientId, AnalysisType.Swot),
            AIOutputType.UseCase => await LoadLatestAnalysisOutputAsync(clientId, AnalysisType.UseCaseGeneration),
            AIOutputType.UseCaseScore => await LoadLatestAnalysisOutputAsync(clientId, AnalysisType.UseCaseScoring),
            AIOutputType.Roadmap => await LoadLatestAnalysisOutputAsync(clientId, AnalysisType.Roadmap),
            AIOutputType.Report => await LoadLatestAnalysisOutputAsync(clientId, AnalysisType.FinalReportSection),
            _ => "No draft content is available yet."
        };
    }

    private async Task<string> LoadLatestAnalysisOutputAsync(int clientId, AnalysisType analysisType)
    {
        return await context.AIAnalysisOutputs
            .AsNoTracking()
            .Where(item => item.ClientCompanyId == clientId && item.AnalysisType == analysisType)
            .OrderByDescending(item => item.VersionNumber)
            .ThenByDescending(item => item.CreatedAt)
            .Select(item => item.OutputContent)
            .FirstOrDefaultAsync() ?? "No draft content is available yet.";
    }

    private async Task<string> BuildKnowledgeGapDraftAsync(int clientId)
    {
        var gaps = await context.KnowledgeGapItems
            .AsNoTracking()
            .Where(item => item.ClientCompanyId == clientId)
            .OrderByDescending(item => item.Priority)
            .ThenBy(item => item.GapArea)
            .Take(40)
            .Select(item => new
            {
                item.GapArea,
                item.MissingInformation,
                item.WhyItMatters,
                item.FollowUpQuestion,
                item.SuggestedEvidence,
                item.Priority,
                item.Status
            })
            .ToListAsync();

        return gaps.Count == 0
            ? "No knowledge gap draft content is available yet."
            : string.Join(Environment.NewLine + Environment.NewLine, gaps.Select(item => $"""
                Area: {item.GapArea}
                Priority: {item.Priority}
                Status: {item.Status}
                Missing information: {item.MissingInformation}
                Why it matters: {item.WhyItMatters}
                Follow-up question: {item.FollowUpQuestion}
                Suggested evidence: {item.SuggestedEvidence}
                """));
    }

    private async Task<string> BuildTitleAsync(int clientId, AIOutputType outputType, int? outputId)
    {
        if (outputId.HasValue)
        {
            var title = await context.AIAnalysisOutputs
                .AsNoTracking()
                .Where(item => item.Id == outputId.Value && item.ClientCompanyId == clientId)
                .Select(item => item.Title)
                .FirstOrDefaultAsync();
            if (!string.IsNullOrWhiteSpace(title))
            {
                return $"AI Workspace - {title}";
            }
        }

        return $"AI Workspace - {outputType}";
    }

    private void AddActivity(int clientId, string type, string description)
    {
        context.ClientActivityLogs.Add(new ClientActivityLog
        {
            ClientCompanyId = clientId,
            ActivityType = type,
            Description = description,
            CreatedBy = "Consultant",
            CreatedAt = DateTime.UtcNow
        });
    }
}
