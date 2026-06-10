using System.Text;
using System.Text.RegularExpressions;
using AI_Readiness_Hub.Data;
using AI_Readiness_Hub.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AI_Readiness_Hub.Services;

public interface IAIContextBuilder
{
    Task<AIContextPackage> BuildAsync(AIContextRequest request);
}

public partial class AIContextBuilder(
    ApplicationDbContext context,
    IPromptTemplateService promptTemplateService,
    IOptions<AIOptions> options) : IAIContextBuilder
{
    private readonly AIOptions options = options.Value;

    public async Task<AIContextPackage> BuildAsync(AIContextRequest request)
    {
        var client = await context.ClientCompanies
            .AsNoTracking()
            .Where(item => item.Id == request.ClientId)
            .Select(item => new
            {
                item.Id,
                item.CompanyName,
                item.Industry,
                item.WebsiteUrl,
                item.Country,
                item.Region,
                item.CompanySizeRange,
                item.RevenueRange,
                item.BusinessModel,
                item.ContactPersonName,
                item.ConsultingPackage,
                item.AssignedConsultant,
                item.CurrentStage,
                item.OverallReadinessScore,
                item.KeyRisksSummary,
                item.NextAction
            })
            .FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("Client was not found.");

        var latestResponse = await context.AssessmentResponses
            .AsNoTracking()
            .Where(response =>
                response.ReadinessAssessment!.ClientCompanyId == request.ClientId &&
                response.Status != AssessmentResponseStatus.Ignored &&
                response.AnswerCount > 0)
            .OrderByDescending(response => response.ReceivedAt)
            .ThenByDescending(response => response.ResponseNumber)
            .ThenByDescending(response => response.Id)
            .Select(response => new
            {
                response.Id,
                response.ResponseLabel,
                response.Source,
                response.ReceivedAt,
                response.AnswerCount,
                response.Status
            })
            .FirstOrDefaultAsync();

        var answers = latestResponse is null
            ? []
            : await context.AssessmentAnswers
                .AsNoTracking()
                .Where(answer => answer.AssessmentResponseId == latestResponse.Id)
                .OrderBy(answer => answer.SectionName)
                .ThenBy(answer => answer.Id)
                .Take(80)
                .Select(answer => new
                {
                    answer.SectionName,
                    answer.QuestionText,
                    answer.AnswerText,
                    answer.CompletenessStatus
                })
                .ToListAsync();

        var knowledgeGaps = await context.KnowledgeGapItems
            .AsNoTracking()
            .Where(item => item.ClientCompanyId == request.ClientId)
            .OrderByDescending(item => item.Status == KnowledgeGapStatus.Approved)
            .ThenByDescending(item => item.Priority)
            .ThenByDescending(item => item.CreatedAt)
            .Take(20)
            .Select(item => new
            {
                item.Id,
                item.GapArea,
                item.MissingInformation,
                item.WhyItMatters,
                item.FollowUpQuestion,
                item.SuggestedEvidence,
                item.Priority,
                item.Status
            })
            .ToListAsync();

        var approvedOutputs = await context.AIAnalysisOutputs
            .AsNoTracking()
            .Where(output => output.ClientCompanyId == request.ClientId && output.Status == DraftStatus.Approved)
            .OrderByDescending(output => output.ApprovedAt ?? output.LastModifiedAt ?? output.CreatedAt)
            .Take(20)
            .Select(output => new
            {
                output.AnalysisType,
                output.Title,
                output.OutputContent,
                output.ApprovedAt
            })
            .ToListAsync();

        var documents = await context.ClientDocuments
            .AsNoTracking()
            .Where(item => item.ClientCompanyId == request.ClientId)
            .OrderByDescending(item => item.UploadedAt)
            .Take(12)
            .Select(item => new
            {
                item.FileName,
                item.DocumentType,
                item.Description,
                item.AiSummary,
                item.KeyInsights,
                item.UploadedAt
            })
            .ToListAsync();

        var notes = await context.ConsultantNotes
            .AsNoTracking()
            .Where(item => item.ClientCompanyId == request.ClientId)
            .OrderByDescending(item => item.CreatedAt)
            .Take(12)
            .Select(item => new
            {
                item.NoteTitle,
                item.NoteType,
                item.NoteText,
                item.CreatedAt
            })
            .ToListAsync();

        var transcripts = await context.MeetingTranscripts
            .AsNoTracking()
            .Where(item => item.ClientCompanyId == request.ClientId)
            .OrderByDescending(item => item.SessionDate)
            .Take(8)
            .Select(item => new
            {
                item.SessionTitle,
                item.SessionDate,
                item.Summary,
                item.KeyDecisions,
                item.FollowUpQuestions
            })
            .ToListAsync();

        var sources = new List<AIContextSource>
        {
            new(
                AIOutputSourceType.Internal,
                latestResponse is null ? AIOutputSourceCategory.Other : AIOutputSourceCategory.AssessmentResponse,
                latestResponse is null ? "Client profile" : $"Assessment Response: {latestResponse.ResponseLabel}",
                latestResponse?.Id.ToString(),
                null,
                latestResponse is null ? "Client profile only." : $"{latestResponse.AnswerCount} assessment answers.")
        };

        sources.AddRange(documents.Select(document => new AIContextSource(
            AIOutputSourceType.Internal,
            AIOutputSourceCategory.Document,
            document.FileName,
            document.DocumentType.ToString(),
            null,
            FirstNonEmpty(document.AiSummary, document.KeyInsights, document.Description))));

        sources.AddRange(notes.Select(note => new AIContextSource(
            AIOutputSourceType.Internal,
            AIOutputSourceCategory.ConsultantNote,
            note.NoteTitle,
            note.NoteType.ToString(),
            null,
            Truncate(note.NoteText, 500))));

        var variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ClientProfile"] = RedactSecrets($"""
                Company: {client.CompanyName}
                Industry: {client.Industry ?? "Not specified"}
                Website: {client.WebsiteUrl ?? "Not specified"}
                Market: {client.Country ?? "Not specified"} {client.Region}
                Size: {client.CompanySizeRange ?? "Not specified"}
                Revenue: {client.RevenueRange ?? "Not specified"}
                Business model: {client.BusinessModel ?? "Not specified"}
                Current workflow stage: {client.CurrentStage}
                Overall readiness score: {client.OverallReadinessScore?.ToString() ?? "Not calculated"}
                Key risks: {client.KeyRisksSummary ?? "Not specified"}
                Next action: {client.NextAction ?? "Not specified"}
                """),
            ["AssessmentSummary"] = latestResponse is null
                ? "No completed assessment response is available."
                : $"{latestResponse.ResponseLabel}; {latestResponse.AnswerCount} answers; received {latestResponse.ReceivedAt:u}; source {latestResponse.Source}.",
            ["AssessmentAnswers"] = BuildAssessmentAnswers(answers),
            ["KnowledgeGaps"] = BuildKnowledgeGapText(knowledgeGaps),
            ["ApprovedCompanySummary"] = FindApprovedOutput(approvedOutputs, AnalysisType.CompanySummary),
            ["ApprovedIndustryAnalysis"] = FindApprovedOutput(approvedOutputs, AnalysisType.IndustryAnalysis),
            ["ApprovedCompetitorAnalysis"] = FindApprovedOutput(approvedOutputs, AnalysisType.CompetitorInsights),
            ["ApprovedSWOT"] = FindApprovedOutput(approvedOutputs, AnalysisType.Swot),
            ["ApprovedUseCases"] = FindApprovedOutput(approvedOutputs, AnalysisType.UseCaseGeneration),
            ["ApprovedRoadmap"] = FindApprovedOutput(approvedOutputs, AnalysisType.Roadmap),
            ["DocumentsSummary"] = BuildDocumentsText(documents),
            ["TranscriptSummary"] = BuildTranscriptsText(transcripts),
            ["ConsultantNotes"] = BuildNotesText(notes),
            ["SourceList"] = string.Join(Environment.NewLine, sources.Select(source => $"- {source.SourceLabel}: {source.EvidenceText}")),
            ["CurrentDraft"] = Truncate(request.CurrentDraft, 8000),
            ["ConsultantFeedback"] = Truncate(request.ConsultantFeedback, 3000),
            ["PreviousMessages"] = Truncate(request.PreviousMessages, 6000)
        };

        var defaultPrompt = DefaultPromptFor(request.OperationName);
        var prompt = await promptTemplateService.BuildPromptAsync(request.OperationName, variables, defaultPrompt);
        var contextText = BuildOperationContext(request.OperationName, variables);
        var boundedContext = BoundContext(RedactSecrets(contextText));
        var warnings = new List<string>();
        if (boundedContext.Length < contextText.Length)
        {
            warnings.Add("Context was truncated to stay within the configured AI input limit.");
        }

        return new AIContextPackage(
            request.OperationName,
            prompt,
            boundedContext,
            variables,
            sources,
            warnings);
    }

    private string BoundContext(string value)
    {
        if (value.Length <= options.MaxInputCharactersPerRequest)
        {
            return value;
        }

        return value[..options.MaxInputCharactersPerRequest] +
            Environment.NewLine +
            "[Context truncated because it exceeded AI__MaxInputCharactersPerRequest.]";
    }

    private static string BuildOperationContext(string operationName, IReadOnlyDictionary<string, string> variables)
    {
        var builder = new StringBuilder();
        AppendSection(builder, "ClientProfile", variables["ClientProfile"]);

        switch (operationName)
        {
            case AIOperationNames.KnowledgeGapAnalysis:
                AppendSection(builder, "AssessmentSummary", variables["AssessmentSummary"]);
                AppendSection(builder, "AssessmentAnswers", variables["AssessmentAnswers"]);
                AppendSection(builder, "ExistingKnowledgeGaps", variables["KnowledgeGaps"]);
                AppendSection(builder, "DocumentsSummary", variables["DocumentsSummary"]);
                AppendSection(builder, "TranscriptSummary", variables["TranscriptSummary"]);
                AppendSection(builder, "ConsultantNotes", variables["ConsultantNotes"]);
                break;
            case AIOperationNames.CompanySummary:
                AppendSection(builder, "AssessmentSummary", variables["AssessmentSummary"]);
                AppendSection(builder, "AssessmentAnswers", variables["AssessmentAnswers"]);
                AppendSection(builder, "ApprovedKnowledgeGaps", variables["KnowledgeGaps"]);
                AppendSection(builder, "ConsultantNotes", variables["ConsultantNotes"]);
                break;
            case AIOperationNames.AIWorkspaceRefinement:
                AppendSection(builder, "CurrentDraft", variables["CurrentDraft"]);
                AppendSection(builder, "ConsultantFeedback", variables["ConsultantFeedback"]);
                AppendSection(builder, "PreviousMessages", variables["PreviousMessages"]);
                AppendSection(builder, "RelevantApprovedOutputs", string.Join(Environment.NewLine, [
                    variables["ApprovedCompanySummary"],
                    variables["ApprovedIndustryAnalysis"],
                    variables["ApprovedCompetitorAnalysis"],
                    variables["ApprovedSWOT"],
                    variables["ApprovedUseCases"],
                    variables["ApprovedRoadmap"]
                ]));
                AppendSection(builder, "SourceList", variables["SourceList"]);
                break;
            default:
                AppendSection(builder, "AssessmentSummary", variables["AssessmentSummary"]);
                AppendSection(builder, "AssessmentAnswers", variables["AssessmentAnswers"]);
                AppendSection(builder, "KnowledgeGaps", variables["KnowledgeGaps"]);
                AppendSection(builder, "ApprovedCompanySummary", variables["ApprovedCompanySummary"]);
                AppendSection(builder, "ApprovedIndustryAnalysis", variables["ApprovedIndustryAnalysis"]);
                AppendSection(builder, "ApprovedCompetitorAnalysis", variables["ApprovedCompetitorAnalysis"]);
                AppendSection(builder, "ApprovedSWOT", variables["ApprovedSWOT"]);
                AppendSection(builder, "ApprovedUseCases", variables["ApprovedUseCases"]);
                AppendSection(builder, "ApprovedRoadmap", variables["ApprovedRoadmap"]);
                break;
        }

        return builder.ToString();
    }

    private static void AppendSection(StringBuilder builder, string title, string content)
    {
        builder.AppendLine($"## {title}");
        builder.AppendLine(string.IsNullOrWhiteSpace(content) ? "Not available." : content);
        builder.AppendLine();
    }

    private static string DefaultPromptFor(string operationName)
    {
        return operationName switch
        {
            AIOperationNames.KnowledgeGapAnalysis => """
                You are assisting an AI readiness consultant. Identify missing understanding that must be clarified before later analysis. Return only valid JSON matching the requested schema.
                """,
            AIOperationNames.CompanySummary => """
                You are assisting an AI readiness consultant. Draft a concise company summary for consultant review. Do not invent facts. Return only valid JSON matching the requested schema.
                """,
            AIOperationNames.AIWorkspaceRefinement => """
                You are assisting a consultant. Improve the provided draft according to the consultant's feedback. Do not invent unsupported facts. Preserve source attribution where available. Return an improved draft suitable for consultant review.
                """,
            _ => "You are assisting an AI readiness consultant. Produce a structured draft for consultant review and approval. Return only valid JSON matching the requested schema."
        };
    }

    private static string BuildAssessmentAnswers(IEnumerable<dynamic> answers)
    {
        var lines = answers.Select(answer =>
            $"- [{answer.SectionName}] {answer.QuestionText} | status: {answer.CompletenessStatus} | answer: {Truncate(answer.AnswerText, 600)}");
        return string.Join(Environment.NewLine, lines.DefaultIfEmpty("No assessment answers available."));
    }

    private static string BuildKnowledgeGapText(IEnumerable<dynamic> gaps)
    {
        var lines = gaps.Select(gap =>
            $"- {gap.Status}/{gap.Priority} {gap.GapArea}: {gap.MissingInformation} Follow-up: {gap.FollowUpQuestion ?? "Not specified"}");
        return string.Join(Environment.NewLine, lines.DefaultIfEmpty("No knowledge gaps available."));
    }

    private static string BuildDocumentsText(IEnumerable<dynamic> documents)
    {
        var lines = documents.Select(document =>
            $"- {document.FileName} ({document.DocumentType}): {FirstNonEmpty(document.AiSummary, document.KeyInsights, document.Description)}");
        return string.Join(Environment.NewLine, lines.DefaultIfEmpty("No document summaries available."));
    }

    private static string BuildNotesText(IEnumerable<dynamic> notes)
    {
        var lines = notes.Select(note => $"- {note.NoteTitle} ({note.NoteType}): {Truncate(note.NoteText, 700)}");
        return string.Join(Environment.NewLine, lines.DefaultIfEmpty("No consultant notes available."));
    }

    private static string BuildTranscriptsText(IEnumerable<dynamic> transcripts)
    {
        var lines = transcripts.Select(transcript =>
            $"- {transcript.SessionTitle} ({transcript.SessionDate:u}): {FirstNonEmpty(transcript.Summary, transcript.KeyDecisions, transcript.FollowUpQuestions)}");
        return string.Join(Environment.NewLine, lines.DefaultIfEmpty("No transcript summaries available."));
    }

    private static string FindApprovedOutput(IEnumerable<dynamic> outputs, AnalysisType type)
    {
        var output = outputs.FirstOrDefault(item => item.AnalysisType == type);
        return output is null ? "Not approved yet." : Truncate(output.OutputContent, 2500);
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        return Truncate(values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)), 700);
    }

    private static string Truncate(string? value, int length)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Not available.";
        }

        var normalized = value.Trim();
        return normalized.Length <= length ? normalized : normalized[..length] + " [truncated]";
    }

    private static string RedactSecrets(string value)
    {
        var redacted = SecretPattern().Replace(value, "$1[redacted]");
        redacted = ConnectionStringPasswordPattern().Replace(redacted, "$1[redacted]");
        return redacted;
    }

    [GeneratedRegex("(OPENAI_API_KEY|SMTP_PASSWORD|ApiKey|API key|Bearer|sk-[A-Za-z0-9_-]+|webhook secret|token|cookie)[:= ]+([^\\s;]+)", RegexOptions.IgnoreCase)]
    private static partial Regex SecretPattern();

    [GeneratedRegex("(Password=)[^;\\s]+", RegexOptions.IgnoreCase)]
    private static partial Regex ConnectionStringPasswordPattern();
}
