using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AI_Readiness_Hub.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
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
    IWebHostEnvironment environment,
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
            var body = new Dictionary<string, object?>
            {
                ["model"] = options.Model,
                ["input"] = new object[]
                {
                    new
                    {
                        role = "system",
                        content = $"""
                            {request.SystemInstruction}

                            Return only valid JSON. Do not use markdown or code fences.
                            """
                    },
                    new { role = "user", content = $"{request.UserPrompt}{Environment.NewLine}{Environment.NewLine}Context:{Environment.NewLine}{request.ContextText}" }
                },
                ["max_output_tokens"] = options.MaxOutputTokens,
                ["text"] = new
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
            if (SupportsTemperature(options.Model))
            {
                body["temperature"] = options.Temperature;
            }

            var httpClient = httpClientFactory.CreateClient("OpenAI");
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "responses")
            {
                Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
            };
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var requestId = response.Headers.TryGetValues("x-request-id", out var requestIds)
                ? requestIds.FirstOrDefault()
                : null;
            if (!response.IsSuccessStatusCode)
            {
                var error = OpenAIErrorDetails.From(responseJson);
                logger.LogWarning(
                    "OpenAI request failed. Operation: {Operation}; StatusCode: {StatusCode}; Provider: OpenAI; Model: {Model}; ErrorType: {ErrorType}; ErrorCode: {ErrorCode}; ErrorParam: {ErrorParam}; RequestId: {RequestId}; DurationMs: {DurationMs}; Message: {Message}",
                    request.OperationName,
                    response.StatusCode,
                    options.Model,
                    error.Type,
                    error.Code,
                    error.Param,
                    requestId ?? "(none)",
                    stopwatch.ElapsedMilliseconds,
                    error.Message ?? "(none)");
                return new AIProviderResult(
                    false,
                    null,
                    AIProviderKind.OpenAI,
                    options.Model,
                    $"OpenAI could not generate a draft. {BuildFriendlyFailure(response.StatusCode, error)}",
                    BuildDiagnosticMessage(response.StatusCode, error, requestId));
            }

            var extraction = OpenAIResponseExtraction.From(responseJson);
            if (string.IsNullOrWhiteSpace(extraction.OutputText))
            {
                var responseShape = extraction.Diagnostics.BuildSummary(includeDevelopmentDetails: environment.IsDevelopment());
                logger.LogWarning(
                    "OpenAI returned no extractable output text. Operation: {Operation}; StatusCode: {StatusCode}; Provider: OpenAI; Model: {Model}; ResponseId: {ResponseId}; ResponseStatus: {ResponseStatus}; RequestId: {RequestId}; DurationMs: {DurationMs}; DiagnosticCategory: {DiagnosticCategory}; ResponseShape: {ResponseShape}",
                    request.OperationName,
                    response.StatusCode,
                    options.Model,
                    extraction.Diagnostics.ResponseId ?? "(none)",
                    extraction.Diagnostics.ResponseStatus ?? "(unknown)",
                    requestId ?? "(none)",
                    stopwatch.ElapsedMilliseconds,
                    BuildEmptyResponseCategory(extraction.Diagnostics),
                    responseShape);
                return new AIProviderResult(
                    false,
                    null,
                    AIProviderKind.OpenAI,
                    options.Model,
                    BuildEmptyResponseFriendlyFailure(extraction.Diagnostics),
                    $"Empty output text. {BuildEmptyResponseCategory(extraction.Diagnostics)}. {responseShape}");
            }

            logger.LogInformation(
                "AI operation completed. Operation: {Operation}; Provider: OpenAI; Model: {Model}; RequestId: {RequestId}; DurationMs: {DurationMs}",
                request.OperationName,
                options.Model,
                requestId ?? "(none)",
                stopwatch.ElapsedMilliseconds);
            return new AIProviderResult(true, extraction.OutputText, AIProviderKind.OpenAI, options.Model);
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

    private static bool SupportsTemperature(string model)
    {
        return !model.StartsWith("gpt-5", StringComparison.OrdinalIgnoreCase) &&
            !model.StartsWith("o", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildFriendlyFailure(HttpStatusCode statusCode, OpenAIErrorDetails error)
    {
        return statusCode switch
        {
            HttpStatusCode.Unauthorized => "Authentication failed. Check the local OPENAI_API_KEY.",
            HttpStatusCode.TooManyRequests => "The request was rate limited or quota was unavailable.",
            HttpStatusCode.NotFound => "The configured model was not found or is not accessible.",
            HttpStatusCode.BadRequest when !string.IsNullOrWhiteSpace(error.Message) => $"Request was rejected: {error.Message}",
            _ when !string.IsNullOrWhiteSpace(error.Message) => error.Message,
            _ => $"OpenAI returned HTTP {(int)statusCode}."
        };
    }

    private static string BuildDiagnosticMessage(HttpStatusCode statusCode, OpenAIErrorDetails error, string? requestId)
    {
        var parts = new List<string>
        {
            $"status={(int)statusCode}",
            $"type={error.Type ?? "unknown"}",
            $"code={error.Code ?? "unknown"}",
            $"param={error.Param ?? "unknown"}"
        };
        if (!string.IsNullOrWhiteSpace(requestId))
        {
            parts.Add($"request_id={requestId}");
        }
        if (!string.IsNullOrWhiteSpace(error.Message))
        {
            parts.Add($"message={error.Message}");
        }

        return string.Join("; ", parts);
    }

    private static bool TryGetNonEmptyString(JsonElement element, string propertyName, out string? value)
    {
        value = null;
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = property.GetString();
        return !string.IsNullOrWhiteSpace(value);
    }

    private static bool TryGetNonEmptyProperty(JsonElement element, string propertyName, out JsonElement value)
    {
        value = default;
        return element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty(propertyName, out value) &&
            value.ValueKind is not JsonValueKind.Null and not JsonValueKind.Undefined;
    }

    private static string BuildEmptyResponseFriendlyFailure(OpenAIResponseDiagnostics diagnostics)
    {
        if (!string.IsNullOrWhiteSpace(diagnostics.IncompleteReason))
        {
            return $"OpenAI stopped before returning usable JSON. Reason: {diagnostics.IncompleteReason}.";
        }

        if (diagnostics.HasRefusal)
        {
            return "OpenAI refused to generate this draft. Please adjust the request or switch to Mock provider.";
        }

        return "OpenAI returned no extractable output text.";
    }

    private static string BuildEmptyResponseCategory(OpenAIResponseDiagnostics diagnostics)
    {
        if (!string.IsNullOrWhiteSpace(diagnostics.IncompleteReason))
        {
            return $"incomplete:{diagnostics.IncompleteReason}";
        }

        if (diagnostics.HasRefusal)
        {
            return "refusal";
        }

        if (diagnostics.OutputItemCount == 0)
        {
            return "no_output_items";
        }

        if (diagnostics.ContentItemCount == 0)
        {
            return "no_content_items";
        }

        return "no_known_text_fields";
    }

    private sealed record OpenAIErrorDetails(string? Type, string? Code, string? Param, string? Message)
    {
        public static OpenAIErrorDetails From(string responseJson)
        {
            if (string.IsNullOrWhiteSpace(responseJson))
            {
                return new OpenAIErrorDetails(null, null, null, null);
            }

            try
            {
                using var document = JsonDocument.Parse(responseJson);
                var error = document.RootElement.TryGetProperty("error", out var errorElement)
                    ? errorElement
                    : document.RootElement;
                return new OpenAIErrorDetails(
                    GetString(error, "type"),
                    GetString(error, "code"),
                    GetString(error, "param"),
                    GetString(error, "message"));
            }
            catch (JsonException)
            {
                return new OpenAIErrorDetails(null, null, null, "OpenAI returned a non-JSON error response.");
            }
        }

        private static string? GetString(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
        }
    }

    private sealed record OpenAIResponseExtraction(string? OutputText, OpenAIResponseDiagnostics Diagnostics)
    {
        public static OpenAIResponseExtraction From(string responseJson)
        {
            using var document = JsonDocument.Parse(responseJson);
            var root = document.RootElement;
            var diagnostics = OpenAIResponseDiagnostics.From(root);
            var outputText = ExtractKnownOutputText(root);
            diagnostics = diagnostics with { ExtractedTextLength = outputText?.Length ?? 0 };
            return new OpenAIResponseExtraction(outputText, diagnostics);
        }

        private static string? ExtractKnownOutputText(JsonElement root)
        {
            if (TryGetNonEmptyString(root, "output_text", out var outputText))
            {
                return outputText;
            }

            if (TryGetNonEmptyProperty(root, "output", out var output) && output.ValueKind == JsonValueKind.Array)
            {
                foreach (var outputItem in output.EnumerateArray())
                {
                    if (TryExtractFromOutputItem(outputItem, out var itemText))
                    {
                        return itemText;
                    }
                }
            }

            if (TryGetNonEmptyProperty(root, "content", out var content) &&
                TryExtractFromContentValue(content, out var directContentText))
            {
                return directContentText;
            }

            return RecursiveKnownTextSearch(root, depth: 0);
        }

        private static bool TryExtractFromOutputItem(JsonElement outputItem, out string? outputText)
        {
            outputText = null;
            if (TryGetNonEmptyString(outputItem, "output_text", out outputText) ||
                TryGetNonEmptyString(outputItem, "text", out outputText))
            {
                return true;
            }

            return TryGetNonEmptyProperty(outputItem, "content", out var content) &&
                TryExtractFromContentValue(content, out outputText);
        }

        private static bool TryExtractFromContentValue(JsonElement content, out string? outputText)
        {
            outputText = null;
            switch (content.ValueKind)
            {
                case JsonValueKind.String:
                    outputText = content.GetString();
                    return !string.IsNullOrWhiteSpace(outputText);
                case JsonValueKind.Object:
                    if (TryGetContentType(content, out var objectType) && objectType == "refusal")
                    {
                        return false;
                    }

                    if (TryGetNonEmptyString(content, "text", out outputText) ||
                        TryGetNonEmptyString(content, "output_text", out outputText) ||
                        TryGetNonEmptyString(content, "content", out outputText))
                    {
                        return true;
                    }

                    outputText = RecursiveKnownTextSearch(content, depth: 0);
                    return !string.IsNullOrWhiteSpace(outputText);
                case JsonValueKind.Array:
                    foreach (var contentItem in content.EnumerateArray())
                    {
                        if (contentItem.ValueKind == JsonValueKind.Object &&
                            TryGetContentType(contentItem, out var contentType) &&
                            contentType == "refusal")
                        {
                            continue;
                        }

                        if (TryExtractFromContentValue(contentItem, out outputText))
                        {
                            return true;
                        }
                    }

                    return false;
                default:
                    return false;
            }
        }

        private static string? RecursiveKnownTextSearch(JsonElement element, int depth)
        {
            if (depth > 10)
            {
                return null;
            }

            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var property in element.EnumerateObject())
                    {
                        if (ShouldSkipProperty(property.Name))
                        {
                            continue;
                        }

                        if (IsKnownTextProperty(property.Name) &&
                            property.Value.ValueKind == JsonValueKind.String &&
                            !string.IsNullOrWhiteSpace(property.Value.GetString()))
                        {
                            return property.Value.GetString();
                        }

                        if (IsKnownTextProperty(property.Name) || property.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
                        {
                            var candidate = RecursiveKnownTextSearch(property.Value, depth + 1);
                            if (!string.IsNullOrWhiteSpace(candidate))
                            {
                                return candidate;
                            }
                        }
                    }

                    break;
                case JsonValueKind.Array:
                    foreach (var item in element.EnumerateArray())
                    {
                        var candidate = RecursiveKnownTextSearch(item, depth + 1);
                        if (!string.IsNullOrWhiteSpace(candidate))
                        {
                            return candidate;
                        }
                    }

                    break;
            }

            return null;
        }

        private static bool TryGetContentType(JsonElement element, out string? contentType)
        {
            contentType = null;
            if (!TryGetNonEmptyString(element, "type", out var type))
            {
                return false;
            }

            contentType = type?.Trim();
            return !string.IsNullOrWhiteSpace(contentType);
        }

        private static bool IsKnownTextProperty(string propertyName)
        {
            return propertyName.Equals("output_text", StringComparison.OrdinalIgnoreCase) ||
                propertyName.Equals("text", StringComparison.OrdinalIgnoreCase) ||
                propertyName.Equals("content", StringComparison.OrdinalIgnoreCase);
        }

        private static bool ShouldSkipProperty(string propertyName)
        {
            return propertyName.Equals("error", StringComparison.OrdinalIgnoreCase) ||
                propertyName.Equals("debug", StringComparison.OrdinalIgnoreCase) ||
                propertyName.Equals("logprobs", StringComparison.OrdinalIgnoreCase) ||
                propertyName.Equals("annotations", StringComparison.OrdinalIgnoreCase) ||
                propertyName.Equals("usage", StringComparison.OrdinalIgnoreCase) ||
                propertyName.Equals("reasoning", StringComparison.OrdinalIgnoreCase) ||
                propertyName.Equals("refusal", StringComparison.OrdinalIgnoreCase);
        }
    }

    private sealed record OpenAIResponseDiagnostics(
        string? ResponseId,
        string? ResponseStatus,
        int OutputItemCount,
        IReadOnlyList<string> OutputItemTypes,
        int ContentItemCount,
        IReadOnlyList<string> ContentItemTypes,
        bool HasOutputText,
        int ExtractedTextLength,
        string? IncompleteReason,
        bool HasRefusal,
        string? SafetyStatus)
    {
        public static OpenAIResponseDiagnostics From(JsonElement root)
        {
            var outputTypes = new List<string>();
            var contentTypes = new List<string>();
            var contentCount = 0;
            var hasRefusal = false;
            var safetyStatus = TryGetNonEmptyString(root, "safety_status", out var safety) ? safety : null;

            if (TryGetNonEmptyProperty(root, "output", out var output) && output.ValueKind == JsonValueKind.Array)
            {
                foreach (var outputItem in output.EnumerateArray())
                {
                    outputTypes.Add(TryGetNonEmptyString(outputItem, "type", out var outputType) ? outputType! : "unknown");
                    if (!TryGetNonEmptyProperty(outputItem, "content", out var content))
                    {
                        continue;
                    }

                    if (content.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var contentItem in content.EnumerateArray())
                        {
                            contentCount++;
                            var contentType = TryGetNonEmptyString(contentItem, "type", out var type) ? type! : contentItem.ValueKind.ToString();
                            contentTypes.Add(contentType);
                            hasRefusal = hasRefusal || contentType.Equals("refusal", StringComparison.OrdinalIgnoreCase);
                        }
                    }
                    else
                    {
                        contentCount++;
                        contentTypes.Add(content.ValueKind.ToString());
                    }
                }
            }

            var incompleteReason = TryGetNonEmptyProperty(root, "incomplete_details", out var incomplete) &&
                TryGetNonEmptyString(incomplete, "reason", out var reason)
                    ? reason
                    : null;

            return new OpenAIResponseDiagnostics(
                TryGetNonEmptyString(root, "id", out var id) ? id : null,
                TryGetNonEmptyString(root, "status", out var status) ? status : null,
                outputTypes.Count,
                outputTypes,
                contentCount,
                contentTypes,
                TryGetNonEmptyString(root, "output_text", out _),
                0,
                incompleteReason,
                hasRefusal,
                safetyStatus);
        }

        public string BuildSummary(bool includeDevelopmentDetails)
        {
            var parts = new List<string>
            {
                $"response_id={ResponseId ?? "none"}",
                $"response_status={ResponseStatus ?? "unknown"}",
                $"output_item_count={OutputItemCount}",
                $"content_item_count={ContentItemCount}",
                $"has_output_text={HasOutputText}",
                $"extracted_text_length={ExtractedTextLength}",
                $"incomplete_reason={IncompleteReason ?? "none"}",
                $"has_refusal={HasRefusal}",
                $"safety_status={SafetyStatus ?? "none"}"
            };

            if (includeDevelopmentDetails)
            {
                parts.Add($"output_item_types={string.Join(",", OutputItemTypes.DefaultIfEmpty("none"))}");
                parts.Add($"content_item_types={string.Join(",", ContentItemTypes.DefaultIfEmpty("none"))}");
            }

            return string.Join("; ", parts);
        }
    }
}
