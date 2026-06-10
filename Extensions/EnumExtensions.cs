using System.Text.RegularExpressions;

namespace AI_Readiness_Hub.Extensions;

public static partial class EnumExtensions
{
    public static string ToDisplayName(this Enum value)
    {
        var text = value.ToString();
        text = text
            .Replace("Ai", "AI", StringComparison.Ordinal)
            .Replace("Roi", "ROI", StringComparison.Ordinal)
            .Replace("Url", "URL", StringComparison.Ordinal)
            .Replace("Swot", "SWOT", StringComparison.Ordinal)
            .Replace("CautiousAdopter", "Cautious Adopter", StringComparison.Ordinal)
            .Replace("GovernanceCompliance", "Governance / Compliance", StringComparison.Ordinal)
            .Replace("KnowledgeGap", "Knowledge Gap", StringComparison.Ordinal)
            .Replace("AssessmentIntroduction", "Assessment Introduction", StringComparison.Ordinal)
            .Replace("UseCase", "Use Case", StringComparison.Ordinal)
            .Replace("ZeroToThreeMonths", "0-3 Months", StringComparison.Ordinal)
            .Replace("ThreeToSixMonths", "3-6 Months", StringComparison.Ordinal)
            .Replace("SixToTwelveMonths", "6-12 Months", StringComparison.Ordinal)
            .Replace("TwelvePlusMonths", "12+ Months", StringComparison.Ordinal)
            .Replace("RiskCompliance", "Risk / Compliance", StringComparison.Ordinal);

        return PascalCaseBoundaryRegex().Replace(text, " ");
    }

    public static string ToBadgeClass(this Enum value)
    {
        var text = value.ToString();
        if (text.Contains("Blocked", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Critical", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Urgent", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Rejected", StringComparison.OrdinalIgnoreCase))
        {
            return "status-risk";
        }

        if (text.Contains("Needs", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Clarification", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("High", StringComparison.OrdinalIgnoreCase))
        {
            return "status-warn";
        }

        if (text.Contains("Draft", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Review", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Progress", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Sent", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Imported", StringComparison.OrdinalIgnoreCase))
        {
            return "status-info";
        }

        if (text.Contains("Approved", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Completed", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Delivered", StringComparison.OrdinalIgnoreCase) ||
            text.Equals("Done", StringComparison.OrdinalIgnoreCase))
        {
            return "status-good";
        }

        if (text.Contains("Closed", StringComparison.OrdinalIgnoreCase))
        {
            return "status-dark";
        }

        return "status-neutral";
    }

    [GeneratedRegex("(?<=[a-z0-9])(?=[A-Z])")]
    private static partial Regex PascalCaseBoundaryRegex();
}
