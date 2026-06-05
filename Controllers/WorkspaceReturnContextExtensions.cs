using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace AI_Readiness_Hub.Controllers;

public static class WorkspaceReturnContextExtensions
{
    public static RouteValueDictionary ToWorkspaceRouteValues(this Controller controller, int clientId)
    {
        var values = new RouteValueDictionary
        {
            ["id"] = clientId
        };

        AddStringValue(controller, values, "activeTab");
        AddIntValue(controller, values, "selectedResponseId");
        AddIntValue(controller, values, "scrollY");

        if (values.TryGetValue("selectedResponseId", out var selectedResponseId))
        {
            values["responseId"] = selectedResponseId;
        }

        return values;
    }

    private static void AddStringValue(Controller controller, RouteValueDictionary values, string key)
    {
        var value = ReadValue(controller, key);
        if (!string.IsNullOrWhiteSpace(value))
        {
            values[key] = value;
        }
    }

    private static void AddIntValue(Controller controller, RouteValueDictionary values, string key)
    {
        var value = ReadValue(controller, key);
        if (int.TryParse(value, out var parsed) && parsed >= 0)
        {
            values[key] = parsed;
        }
    }

    private static string? ReadValue(Controller controller, string key)
    {
        if (controller.Request.HasFormContentType &&
            controller.Request.Form.TryGetValue(key, out var formValue) &&
            !string.IsNullOrWhiteSpace(formValue))
        {
            return formValue.ToString();
        }

        if (controller.Request.Query.TryGetValue(key, out var queryValue) &&
            !string.IsNullOrWhiteSpace(queryValue))
        {
            return queryValue.ToString();
        }

        return null;
    }
}
