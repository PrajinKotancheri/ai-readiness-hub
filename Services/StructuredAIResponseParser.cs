using System.Text.Json;
using AI_Readiness_Hub.Models;

namespace AI_Readiness_Hub.Services;

public interface IStructuredAIResponseParser
{
    IReadOnlyList<ParsedKnowledgeGapItem> ParseKnowledgeGaps(string json);
    ParsedCompanySummary ParseCompanySummary(string json);
    ParsedRefinement ParseRefinement(string json);
}

public class StructuredAIResponseParser : IStructuredAIResponseParser
{
    public IReadOnlyList<ParsedKnowledgeGapItem> ParseKnowledgeGaps(string json)
    {
        using var document = JsonDocument.Parse(NormalizeJson(json));
        var items = ReadKnowledgeGapItemElements(document.RootElement);

        var parsed = new List<ParsedKnowledgeGapItem>();
        foreach (var item in items)
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var followUpQuestion = FirstOptionalString(item, "followUpQuestion", "question", "follow_up_question");
            var missingInformation = FirstOptionalString(
                item,
                "missingInformation",
                "missingInfo",
                "gap",
                "missing",
                "clarificationNeeded",
                "description");
            if (string.IsNullOrWhiteSpace(missingInformation))
            {
                missingInformation = followUpQuestion;
            }

            var whyItMatters = FirstOptionalString(item, "whyItMatters", "reason", "rationale", "importance");
            var suggestedEvidence = FirstOptionalString(item, "suggestedEvidence", "evidence", "evidenceNeeded");
            var hasMeaningfulContent = new[] { missingInformation, followUpQuestion, whyItMatters, suggestedEvidence }
                .Any(value => !string.IsNullOrWhiteSpace(value));
            if (!hasMeaningfulContent)
            {
                continue;
            }

            missingInformation ??= "AI identified a knowledge gap but did not include a separate missing information field.";
            parsed.Add(new ParsedKnowledgeGapItem(
                ParseKnowledgeGapArea(item),
                missingInformation,
                whyItMatters,
                followUpQuestion,
                suggestedEvidence,
                ParseKnowledgeGapPriority(item),
                ParseSources(item, "sources", "references", "sourceReferences")));
        }

        if (parsed.Count == 0)
        {
            throw new InvalidOperationException("AI response did not include any meaningful knowledge gap items.");
        }

        return parsed.Take(12).ToList();
    }

    public ParsedCompanySummary ParseCompanySummary(string json)
    {
        using var document = JsonDocument.Parse(NormalizeJson(json));
        var root = document.RootElement;
        return new ParsedCompanySummary(
            RequiredString(root, "summary"),
            OptionalString(root, "businessModel"),
            ReadStringArray(root, "strategicGoals"),
            OptionalString(root, "operationalContext"),
            OptionalString(root, "aiReadinessImplications"),
            ParseSources(root));
    }

    public ParsedRefinement ParseRefinement(string json)
    {
        using var document = JsonDocument.Parse(NormalizeJson(json));
        var root = document.RootElement;
        return new ParsedRefinement(
            RequiredString(root, "improvedDraft"),
            OptionalString(root, "summaryOfChanges"),
            ParseSources(root));
    }

    private static string NormalizeJson(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            var firstNewLine = trimmed.IndexOf('\n', StringComparison.Ordinal);
            var lastFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
            if (firstNewLine >= 0 && lastFence > firstNewLine)
            {
                trimmed = trimmed[(firstNewLine + 1)..lastFence].Trim();
            }
        }

        if (IsValidJsonRoot(trimmed))
        {
            return trimmed;
        }

        var objectCandidate = ExtractJsonCandidate(trimmed, '{', '}', JsonValueKind.Object);
        if (objectCandidate is not null)
        {
            return objectCandidate;
        }

        var arrayCandidate = ExtractJsonCandidate(trimmed, '[', ']', JsonValueKind.Array);
        if (arrayCandidate is not null)
        {
            return arrayCandidate;
        }

        return trimmed;
    }

    private static string? ExtractJsonCandidate(string value, char opening, char closing, JsonValueKind expectedRoot)
    {
        var start = value.IndexOf(opening, StringComparison.Ordinal);
        var end = value.LastIndexOf(closing);
        if (start < 0 || end <= start)
        {
            return null;
        }

        var candidate = value[start..(end + 1)];
        return IsValidJsonRoot(candidate, expectedRoot) ? candidate : null;
    }

    private static bool IsValidJsonRoot(string value, JsonValueKind? expectedRoot = null)
    {
        try
        {
            using var document = JsonDocument.Parse(value);
            return document.RootElement.ValueKind is JsonValueKind.Object or JsonValueKind.Array &&
                (expectedRoot is null || document.RootElement.ValueKind == expectedRoot);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static IReadOnlyList<JsonElement> ReadKnowledgeGapItemElements(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Array)
        {
            return root.EnumerateArray().ToList();
        }

        if (root.ValueKind == JsonValueKind.Object)
        {
            foreach (var propertyName in new[] { "items", "knowledgeGaps", "gaps" })
            {
                if (TryGetProperty(root, propertyName, out var value) && value.ValueKind == JsonValueKind.Array)
                {
                    return value.EnumerateArray().ToList();
                }
            }
        }

        throw new InvalidOperationException("AI response did not include a valid knowledge gap item list.");
    }

    private static KnowledgeGapArea ParseKnowledgeGapArea(JsonElement element)
    {
        var rawValue = FirstOptionalString(element, "gapArea", "area", "category", "topic");
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return KnowledgeGapArea.Other;
        }

        var normalized = NormalizeEnumValue(rawValue);
        if (normalized.Contains("sales", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("crm", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("qualification", StringComparison.OrdinalIgnoreCase))
        {
            return KnowledgeGapArea.SalesQualification;
        }

        if (normalized.Contains("customer", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("onboarding", StringComparison.OrdinalIgnoreCase))
        {
            return KnowledgeGapArea.CustomerOnboarding;
        }

        if (normalized.Contains("report", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("measure", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("analytic", StringComparison.OrdinalIgnoreCase))
        {
            return KnowledgeGapArea.ReportingMeasurement;
        }

        if (normalized.Contains("data", StringComparison.OrdinalIgnoreCase))
        {
            return KnowledgeGapArea.DataOwnership;
        }

        if (normalized.Contains("system", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("integration", StringComparison.OrdinalIgnoreCase))
        {
            return KnowledgeGapArea.SystemsIntegration;
        }

        if (normalized.Contains("governance", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("compliance", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("risk", StringComparison.OrdinalIgnoreCase))
        {
            return KnowledgeGapArea.GovernanceCompliance;
        }

        if (normalized.Contains("operation", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("process", StringComparison.OrdinalIgnoreCase))
        {
            return KnowledgeGapArea.OperationsProcess;
        }

        if (normalized.Contains("strategy", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("vision", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("goal", StringComparison.OrdinalIgnoreCase))
        {
            return KnowledgeGapArea.StrategyVision;
        }

        if (normalized.Contains("business", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("model", StringComparison.OrdinalIgnoreCase))
        {
            return KnowledgeGapArea.BusinessModel;
        }

        return ParseEnumValue(rawValue, KnowledgeGapArea.Other);
    }

    private static KnowledgeGapPriority ParseKnowledgeGapPriority(JsonElement element)
    {
        var rawValue = FirstOptionalString(element, "priority", "severity", "importance");
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return KnowledgeGapPriority.Medium;
        }

        if (rawValue.Contains("critical", StringComparison.OrdinalIgnoreCase) ||
            rawValue.Contains("urgent", StringComparison.OrdinalIgnoreCase))
        {
            return KnowledgeGapPriority.High;
        }

        return ParseEnumValue(rawValue, KnowledgeGapPriority.Medium);
    }

    private static IReadOnlyList<AIContextSource> ParseSources(JsonElement element, params string[] propertyNames)
    {
        IReadOnlyList<string> sourcePropertyNames = propertyNames.Length == 0 ? ["sources"] : propertyNames;
        if (!TryGetArrayProperty(element, sourcePropertyNames, out var sources))
        {
            return [];
        }

        var parsed = new List<AIContextSource>();
        foreach (var source in sources.EnumerateArray().Take(12))
        {
            if (source.ValueKind == JsonValueKind.String)
            {
                var stringLabel = Truncate(source.GetString()?.Trim(), 180);
                if (!string.IsNullOrWhiteSpace(stringLabel))
                {
                    parsed.Add(new AIContextSource(
                        AIOutputSourceType.Internal,
                        AIOutputSourceCategory.Other,
                        stringLabel,
                        null,
                        null,
                        null));
                }

                continue;
            }

            if (source.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var label = FirstOptionalString(source, "sourceLabel", "label", "title", "name", "source");
            label ??= FirstOptionalString(source, "sourceReference", "reference", "ref");
            if (string.IsNullOrWhiteSpace(label))
            {
                continue;
            }

            parsed.Add(new AIContextSource(
                ParseEnum(source, AIOutputSourceType.Internal, "sourceType", "type"),
                ParseEnum(source, AIOutputSourceCategory.Other, "sourceCategory", "category"),
                label,
                FirstOptionalString(source, "sourceReference", "reference", "ref"),
                FirstOptionalString(source, "sourceUrl", "url", "link"),
                FirstOptionalString(source, "evidenceText", "evidence", "quote", "summary")));
        }

        return parsed;
    }

    private static IReadOnlyList<string> ReadStringArray(JsonElement element, string propertyName)
    {
        if (!TryGetProperty(element, propertyName, out var values) || values.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return values
            .EnumerateArray()
            .Where(value => value.ValueKind == JsonValueKind.String)
            .Select(value => value.GetString())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .Take(12)
            .ToList();
    }

    private static TEnum ParseEnum<TEnum>(JsonElement element, string propertyName, TEnum fallback)
        where TEnum : struct, Enum
    {
        return ParseEnum(element, fallback, propertyName);
    }

    private static TEnum ParseEnum<TEnum>(JsonElement element, TEnum fallback, params string[] propertyNames)
        where TEnum : struct, Enum
    {
        var rawValue = FirstOptionalString(element, propertyNames);
        return ParseEnumValue(rawValue, fallback);
    }

    private static TEnum ParseEnumValue<TEnum>(string? rawValue, TEnum fallback)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return fallback;
        }

        var normalized = NormalizeEnumValue(rawValue);
        foreach (var value in Enum.GetValues<TEnum>())
        {
            var name = value.ToString();
            if (name.Equals(rawValue, StringComparison.OrdinalIgnoreCase) ||
                name.Equals(normalized, StringComparison.OrdinalIgnoreCase))
            {
                return value;
            }
        }

        return fallback;
    }

    private static string RequiredString(JsonElement element, string propertyName)
    {
        var value = OptionalString(element, propertyName);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"AI response did not include required field '{propertyName}'.");
        }

        return value;
    }

    private static string? OptionalString(JsonElement element, string propertyName)
    {
        if (!TryGetProperty(element, propertyName, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString()?.Trim(),
            JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => value.ToString(),
            _ => null
        };
    }

    private static string? FirstOptionalString(JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            var value = OptionalString(element, propertyName);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static bool TryGetArrayProperty(JsonElement element, IReadOnlyList<string> propertyNames, out JsonElement value)
    {
        foreach (var propertyName in propertyNames)
        {
            if (TryGetProperty(element, propertyName, out value) && value.ValueKind == JsonValueKind.Array)
            {
                return true;
            }
        }

        value = default;
        return false;
    }

    private static bool TryGetProperty(JsonElement element, string propertyName, out JsonElement value)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            value = default;
            return false;
        }

        if (element.TryGetProperty(propertyName, out value))
        {
            return true;
        }

        var normalizedPropertyName = NormalizePropertyName(propertyName);
        foreach (var property in element.EnumerateObject())
        {
            if (NormalizePropertyName(property.Name).Equals(normalizedPropertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static string NormalizeEnumValue(string value)
    {
        return value.Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("&", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("/", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("-", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("_", string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizePropertyName(string value)
    {
        return value.Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("-", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("_", string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length <= maxLength)
        {
            return value;
        }

        return value[..(maxLength - 3)] + "...";
    }
}
