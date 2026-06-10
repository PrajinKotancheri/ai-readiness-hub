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
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("AI response did not include a valid knowledge gap item list.");
        }

        var parsed = new List<ParsedKnowledgeGapItem>();
        foreach (var item in items.EnumerateArray())
        {
            var missingInformation = RequiredString(item, "missingInformation");
            parsed.Add(new ParsedKnowledgeGapItem(
                ParseEnum(item, "gapArea", KnowledgeGapArea.Other),
                missingInformation,
                OptionalString(item, "whyItMatters"),
                OptionalString(item, "followUpQuestion"),
                OptionalString(item, "suggestedEvidence"),
                ParseEnum(item, "priority", KnowledgeGapPriority.Medium),
                ParseSources(item)));
        }

        if (parsed.Count == 0)
        {
            throw new InvalidOperationException("AI response did not include any knowledge gap items.");
        }

        return parsed.Take(12).ToList();
    }

    public ParsedCompanySummary ParseCompanySummary(string json)
    {
        using var document = JsonDocument.Parse(json);
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
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        return new ParsedRefinement(
            RequiredString(root, "improvedDraft"),
            OptionalString(root, "summaryOfChanges"),
            ParseSources(root));
    }

    private static IReadOnlyList<AIContextSource> ParseSources(JsonElement element)
    {
        if (!element.TryGetProperty("sources", out var sources) || sources.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var parsed = new List<AIContextSource>();
        foreach (var source in sources.EnumerateArray().Take(12))
        {
            var label = OptionalString(source, "sourceLabel");
            if (string.IsNullOrWhiteSpace(label))
            {
                continue;
            }

            parsed.Add(new AIContextSource(
                ParseEnum(source, "sourceType", AIOutputSourceType.Internal),
                ParseEnum(source, "sourceCategory", AIOutputSourceCategory.Other),
                label,
                OptionalString(source, "sourceReference"),
                OptionalString(source, "sourceUrl"),
                OptionalString(source, "evidenceText")));
        }

        return parsed;
    }

    private static IReadOnlyList<string> ReadStringArray(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var values) || values.ValueKind != JsonValueKind.Array)
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
        var rawValue = OptionalString(element, propertyName);
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return fallback;
        }

        var normalized = rawValue.Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("&", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("/", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("-", string.Empty, StringComparison.OrdinalIgnoreCase);
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
        if (!element.TryGetProperty(propertyName, out var value))
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
}
