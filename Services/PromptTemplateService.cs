using AI_Readiness_Hub.Data;
using AI_Readiness_Hub.Models;
using Microsoft.EntityFrameworkCore;

namespace AI_Readiness_Hub.Services;

public interface IPromptTemplateService
{
    Task<string> BuildPromptAsync(string operationName, IReadOnlyDictionary<string, string> variables, string defaultPrompt);
}

public class PromptTemplateService(ApplicationDbContext context) : IPromptTemplateService
{
    public async Task<string> BuildPromptAsync(string operationName, IReadOnlyDictionary<string, string> variables, string defaultPrompt)
    {
        var prompt = await context.PromptDefinitions
            .AsNoTracking()
            .Where(item =>
                item.PromptName == operationName &&
                item.Status == PromptStatus.Active &&
                !string.IsNullOrWhiteSpace(item.PromptText))
            .OrderByDescending(item => item.VersionNumber)
            .Select(item => new
            {
                item.Goal,
                item.Inputs,
                item.Outputs,
                item.PromptText
            })
            .FirstOrDefaultAsync();

        var template = prompt is null || IsPlaceholderPrompt(prompt.PromptText)
            ? defaultPrompt
            : $"""
              Goal:
              {prompt.Goal}

              Inputs:
              {prompt.Inputs}

              Expected output:
              {prompt.Outputs}

              Prompt:
              {prompt.PromptText}
              """;

        foreach (var variable in variables)
        {
            template = template.Replace($"{{{{{variable.Key}}}}}", variable.Value, StringComparison.OrdinalIgnoreCase);
        }

        return template;
    }

    private static bool IsPlaceholderPrompt(string? promptText)
    {
        return string.IsNullOrWhiteSpace(promptText) ||
            promptText.Trim().Equals("Stakeholder to provide actual prompt.", StringComparison.OrdinalIgnoreCase);
    }
}
