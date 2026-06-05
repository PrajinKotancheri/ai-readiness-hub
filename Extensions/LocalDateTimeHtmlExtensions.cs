using System.Globalization;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AI_Readiness_Hub.Extensions;

public static class LocalDateTimeHtmlExtensions
{
    public static IHtmlContent LocalDateTime(
        this IHtmlHelper html,
        DateTime? utcDate,
        string format = "datetime",
        string emptyText = "-")
    {
        if (!utcDate.HasValue)
        {
            return new HtmlContentBuilder().Append(emptyText);
        }

        var utcValue = NormalizeUtc(utcDate.Value);
        var fallbackText = FormatUtcFallback(utcValue, format);
        var tag = new TagBuilder("span");
        tag.AddCssClass("js-local-datetime");
        tag.Attributes["data-utc"] = utcValue.ToString("O", CultureInfo.InvariantCulture);
        tag.Attributes["data-format"] = format;
        tag.Attributes["title"] = $"UTC: {fallbackText}";
        tag.InnerHtml.Append(fallbackText);

        return tag;
    }

    private static DateTime NormalizeUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            _ => value.ToUniversalTime()
        };
    }

    private static string FormatUtcFallback(DateTime utcValue, string format)
    {
        var pattern = format.ToLowerInvariant() switch
        {
            "date" => "dd MMM yyyy 'UTC'",
            "time" => "HH:mm 'UTC'",
            _ => "dd MMM yyyy HH:mm 'UTC'"
        };

        return utcValue.ToString(pattern, CultureInfo.InvariantCulture);
    }
}
