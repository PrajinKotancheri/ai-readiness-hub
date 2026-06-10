using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AI_Readiness_Hub.Models;
using Microsoft.Extensions.Options;

namespace AI_Readiness_Hub.Services;

public interface IAIProviderClient
{
    Task<AIProviderResult> GenerateStructuredJsonAsync(AIProviderRequest request, CancellationToken cancellationToken = default);
}

public class ConfiguredAIProviderClient(
    MockAIProviderClient mockProvider,
    OpenAIProviderClient openAIProvider,
    IOptions<AIOptions> options) : IAIProviderClient
{
    public Task<AIProviderResult> GenerateStructuredJsonAsync(AIProviderRequest request, CancellationToken cancellationToken = default)
    {
        return options.Value.ProviderKind == AIProviderKind.OpenAI
            ? openAIProvider.GenerateStructuredJsonAsync(request, cancellationToken)
            : mockProvider.GenerateStructuredJsonAsync(request, cancellationToken);
    }
}

public class MockAIProviderClient(IOptions<AIOptions> options) : IAIProviderClient
{
    private readonly AIOptions options = options.Value;

    public Task<AIProviderResult> GenerateStructuredJsonAsync(AIProviderRequest request, CancellationToken cancellationToken = default)
    {
        var content = request.OperationName switch
        {
            AIOperationNames.KnowledgeGapAnalysis => BuildMockKnowledgeGaps(),
            AIOperationNames.CompanySummary => BuildMockCompanySummary(),
            AIOperationNames.AIWorkspaceRefinement => BuildMockRefinement(request),
            AIOperationNames.ReadinessScore => BuildMockReadinessScore(),
            AIOperationNames.IndustryAnalysis => BuildMockIndustryAnalysis(),
            AIOperationNames.CompetitorAnalysis => BuildMockCompetitorAnalysis(),
            AIOperationNames.SwotAnalysis => BuildMockSwot(),
            AIOperationNames.UseCaseIdentification => BuildMockUseCases(),
            AIOperationNames.UseCaseScoring => BuildMockUseCaseScoring(),
            AIOperationNames.RoadmapGeneration => BuildMockRoadmap(),
            AIOperationNames.StrategicReportGeneration => BuildMockStrategicReport(),
            _ => JsonSerializer.Serialize(new { draft = "Mock AI draft for consultant review.", sources = Array.Empty<string>() })
        };

        return Task.FromResult(new AIProviderResult(true, content, AIProviderKind.Mock, options.Model));
    }

    private static string BuildMockKnowledgeGaps()
    {
        return JsonSerializer.Serialize(new
        {
            items = new[]
            {
                new
                {
                    gapArea = "GovernanceCompliance",
                    missingInformation = "AI governance owner, review cadence, and approval pathway need confirmation.",
                    whyItMatters = "Later recommendations depend on knowing who can approve pilots, data access, and risk controls.",
                    followUpQuestion = "Who owns AI policy, model review, privacy approval, and ongoing oversight?",
                    suggestedEvidence = "Governance owner name, risk review workflow, AI policy draft, or approval checklist.",
                    priority = "High",
                    sources = new[]
                    {
                        new
                        {
                            sourceType = "Internal",
                            sourceCategory = "AssessmentResponse",
                            sourceLabel = "Latest assessment response",
                            sourceReference = "Governance",
                            sourceUrl = (string?)null,
                            evidenceText = "Mock provider detected incomplete governance evidence."
                        }
                    }
                },
                new
                {
                    gapArea = "DataOwnership",
                    missingInformation = "Data source owners and access constraints need more detail.",
                    whyItMatters = "Use-case feasibility depends on data availability, quality, and accountable owners.",
                    followUpQuestion = "Which systems contain the data for the first pilot and who owns access approval?",
                    suggestedEvidence = "System list, owner map, data quality notes, and access constraints.",
                    priority = "Medium",
                    sources = new[]
                    {
                        new
                        {
                            sourceType = "Internal",
                            sourceCategory = "Questionnaire",
                            sourceLabel = "Assessment answers",
                            sourceReference = "Data readiness",
                            sourceUrl = (string?)null,
                            evidenceText = "Mock provider detected short or partial data readiness answers."
                        }
                    }
                }
            }
        });
    }

    private static string BuildMockCompanySummary()
    {
        return JsonSerializer.Serialize(new
        {
            summary = "Mock AI draft: the client has enough context for an initial AI readiness narrative, but the consultant should validate governance, data ownership, and first-pilot constraints before approval.",
            businessModel = "Draft business model interpretation based on available profile and assessment evidence.",
            strategicGoals = new[] { "Improve operational efficiency", "Prioritize safe measurable AI pilots", "Create reusable governance foundations" },
            operationalContext = "The organization appears to have practical use-case potential, with follow-up needed around data access and ownership.",
            aiReadinessImplications = "The consultant should frame AI as a staged enablement program rather than a fully automated report outcome.",
            sources = new[]
            {
                new
                {
                    sourceType = "Internal",
                    sourceCategory = "AssessmentResponse",
                    sourceLabel = "Latest assessment response",
                    sourceReference = "Company summary mock evidence",
                    sourceUrl = (string?)null,
                    evidenceText = "Mock provider used compact client profile and assessment context."
                }
            }
        });
    }

    private static string BuildMockRefinement(AIProviderRequest request)
    {
        var feedback = string.IsNullOrWhiteSpace(request.ConsultantFeedback)
            ? "No specific feedback was supplied."
            : request.ConsultantFeedback.Trim();
        var draft = string.IsNullOrWhiteSpace(request.CurrentDraft)
            ? "Mock draft was empty, so the assistant created a fresh consultant-review draft."
            : request.CurrentDraft.Trim();

        return JsonSerializer.Serialize(new
        {
            improvedDraft = $"{draft}{Environment.NewLine}{Environment.NewLine}Mock refinement note: revised with consultant feedback in mind: {feedback}",
            summaryOfChanges = "Mock provider appended a refinement note and preserved the existing draft content.",
            sources = Array.Empty<object>()
        });
    }

    private static string BuildMockReadinessScore()
    {
        return JsonSerializer.Serialize(new
        {
            overallScoreOutOf100 = 55,
            overallScoreOutOf10 = 5.5,
            adoptionProfile = "Cautious Adopter",
            benchmark = "Mock benchmark for consultant review.",
            interpretation = "The client has practical pilot potential but needs clearer ownership, data readiness, and governance guardrails.",
            dimensions = new[]
            {
                new { area = "Strategy & Vision", scoreOutOf5 = 3, status = "Developing", interpretation = "Strategic intent is visible but needs prioritization." }
            },
            sources = Array.Empty<object>()
        });
    }

    private static string BuildMockIndustryAnalysis()
    {
        return JsonSerializer.Serialize(new
        {
            industryOverview = "Mock industry overview for consultant review.",
            marketTrends = new[] { "Productivity assistants", "Document intelligence", "Governed automation" },
            aiOpportunities = new[] { "Internal knowledge support", "Reporting acceleration" },
            risksAndConstraints = new[] { "Data quality", "Approval ownership" },
            strategicImplications = "Start with bounded internal use cases before external-facing automation.",
            sources = Array.Empty<object>()
        });
    }

    private static string BuildMockCompetitorAnalysis()
    {
        return JsonSerializer.Serialize(new
        {
            competitors = new[] { new { name = "Representative competitor", positioning = "Comparable market participant", aiActivities = "Likely using productivity and support automation.", relevanceToClient = "Useful benchmark after consultant validation." } },
            competitiveTakeaway = "Mock takeaway: competitor analysis needs sourced validation before approval.",
            sources = Array.Empty<object>()
        });
    }

    private static string BuildMockSwot()
    {
        return JsonSerializer.Serialize(new
        {
            strengths = new[] { "Clear consultant-led review workflow" },
            weaknesses = new[] { "Some readiness inputs remain incomplete" },
            opportunities = new[] { "High-value internal automation pilots" },
            threats = new[] { "Ungoverned AI pilots could create trust risk" },
            strategicTakeaway = "Mock SWOT draft for consultant review.",
            sources = Array.Empty<object>()
        });
    }

    private static string BuildMockUseCases()
    {
        return JsonSerializer.Serialize(new
        {
            useCases = new[]
            {
                new
                {
                    name = "Internal knowledge assistant",
                    description = "Answer internal questions from approved documents and notes.",
                    focus = "Operations",
                    roiPotential = "High",
                    complexity = "Medium",
                    dependencies = new[] { "Approved source documents", "Governance owner" },
                    expectedOutcome = "Faster access to reusable operating knowledge.",
                    sources = Array.Empty<object>()
                }
            }
        });
    }

    private static string BuildMockUseCaseScoring()
    {
        return JsonSerializer.Serialize(new
        {
            scores = new[] { new { name = "Internal knowledge assistant", roiScore = 4, feasibilityScore = 4, strategicFitScore = 4, dataReadinessScore = 3, riskSafetyScore = 3, rationale = "Mock scoring rationale." } }
        });
    }

    private static string BuildMockRoadmap()
    {
        return JsonSerializer.Serialize(new
        {
            phases = new[]
            {
                new { phaseName = "Foundation", timeframe = "0-3 months", initiatives = new[] { "Confirm governance owner" }, successCriteria = new[] { "Pilot owner named" }, dependencies = new[] { "Approved first use case" } }
            },
            sources = Array.Empty<object>()
        });
    }

    private static string BuildMockStrategicReport()
    {
        return JsonSerializer.Serialize(new
        {
            sections = new[]
            {
                new { title = "AI Readiness Summary", content = "Mock strategic report section for consultant review.", sources = Array.Empty<object>() }
            }
        });
    }
}

public class OpenAIProviderClient(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    IOptions<AIOptions> options,
    ILogger<OpenAIProviderClient> logger) : IAIProviderClient
{
    private readonly AIOptions options = options.Value;

    public async Task<AIProviderResult> GenerateStructuredJsonAsync(AIProviderRequest request, CancellationToken cancellationToken = default)
    {
        var apiKey = configuration["OPENAI_API_KEY"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            const string message = "OpenAI provider is enabled but OPENAI_API_KEY is not configured.";
            logger.LogWarning("{Message} Operation: {Operation}; Model: {Model}", message, request.OperationName, options.Model);
            return new AIProviderResult(false, null, AIProviderKind.OpenAI, options.Model, message, message);
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var body = new
            {
                model = options.Model,
                input = new object[]
                {
                    new { role = "system", content = request.SystemInstruction },
                    new { role = "user", content = $"{request.UserPrompt}{Environment.NewLine}{Environment.NewLine}Context:{Environment.NewLine}{request.ContextText}" }
                },
                temperature = options.Temperature,
                max_output_tokens = options.MaxOutputTokens,
                text = new
                {
                    format = new
                    {
                        type = "json_schema",
                        name = request.SchemaName,
                        strict = true,
                        schema = request.JsonSchema
                    }
                }
            };

            var httpClient = httpClientFactory.CreateClient("OpenAI");
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "responses")
            {
                Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
            };
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "OpenAI request failed. Operation: {Operation}; StatusCode: {StatusCode}; Provider: OpenAI; Model: {Model}; DurationMs: {DurationMs}",
                    request.OperationName,
                    response.StatusCode,
                    options.Model,
                    stopwatch.ElapsedMilliseconds);
                return new AIProviderResult(false, null, AIProviderKind.OpenAI, options.Model, "OpenAI could not generate a draft. Please try again or switch to Mock provider.", response.StatusCode.ToString());
            }

            var outputText = ExtractOutputText(responseJson);
            if (string.IsNullOrWhiteSpace(outputText))
            {
                return new AIProviderResult(false, null, AIProviderKind.OpenAI, options.Model, "OpenAI returned an empty response.", "Empty output text.");
            }

            logger.LogInformation(
                "AI operation completed. Operation: {Operation}; Provider: OpenAI; Model: {Model}; DurationMs: {DurationMs}",
                request.OperationName,
                options.Model,
                stopwatch.ElapsedMilliseconds);
            return new AIProviderResult(true, outputText, AIProviderKind.OpenAI, options.Model);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "OpenAI request failed. Operation: {Operation}; Provider: OpenAI; Model: {Model}; DurationMs: {DurationMs}",
                request.OperationName,
                options.Model,
                stopwatch.ElapsedMilliseconds);
            return new AIProviderResult(false, null, AIProviderKind.OpenAI, options.Model, "OpenAI could not generate a draft. Please try again or switch to Mock provider.", ex.Message);
        }
    }

    private static string? ExtractOutputText(string responseJson)
    {
        using var document = JsonDocument.Parse(responseJson);
        if (document.RootElement.TryGetProperty("output_text", out var outputText) &&
            outputText.ValueKind == JsonValueKind.String)
        {
            return outputText.GetString();
        }

        if (!document.RootElement.TryGetProperty("output", out var output) || output.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var item in output.EnumerateArray())
        {
            if (!item.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var contentItem in content.EnumerateArray())
            {
                if (contentItem.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String)
                {
                    return text.GetString();
                }
            }
        }

        return null;
    }
}
