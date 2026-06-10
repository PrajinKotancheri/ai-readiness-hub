using System.Net;
using System.Security.Cryptography;
using AI_Readiness_Hub.Data;
using AI_Readiness_Hub.Models;
using Microsoft.EntityFrameworkCore;

namespace AI_Readiness_Hub.Services;

public class ReadinessFormService(
    ApplicationDbContext context,
    IEmailService emailService,
    ILogger<ReadinessFormService> logger) : IReadinessFormService
{
    public async Task<ReadinessAssessment> EnsureGeneratedFormLinkAsync(
        int clientId,
        string? customFormUrl = null,
        CancellationToken cancellationToken = default)
    {
        var client = await LoadClientAsync(clientId, cancellationToken)
            ?? throw new InvalidOperationException("Client was not found.");
        var settings = await GetActiveSettingsAsync(cancellationToken);
        var assessment = GetOrCreateOpenAssessment(client);

        if (!string.IsNullOrWhiteSpace(customFormUrl))
        {
            assessment.CustomFormUrl = customFormUrl.Trim();
        }

        var baseFormUrl = !string.IsNullOrWhiteSpace(assessment.CustomFormUrl)
            ? assessment.CustomFormUrl
            : settings.DefaultFormUrl;

        if (string.IsNullOrWhiteSpace(baseFormUrl))
        {
            throw new InvalidOperationException("Default Google Form URL is not configured. Add it in Settings > Readiness form.");
        }

        if (string.IsNullOrWhiteSpace(settings.ClientReferenceEntryId))
        {
            throw new InvalidOperationException("Client Reference Entry ID is not configured. Add the Google Forms entry id in Settings > Readiness form.");
        }

        assessment.ClientToken ??= GenerateClientToken();
        assessment.GeneratedFormUrl = BuildGeneratedFormUrl(baseFormUrl, settings.ClientReferenceEntryId, assessment.ClientToken);
        assessment.FormUrl = assessment.GeneratedFormUrl;
        assessment.LastModifiedAt = DateTime.UtcNow;
        client.LastModifiedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
        return assessment;
    }

    public async Task<ReadinessAssessment> SendReadinessFormAsync(int clientId, CancellationToken cancellationToken = default)
    {
        var client = await LoadClientAsync(clientId, cancellationToken)
            ?? throw new InvalidOperationException("Client was not found.");

        if (string.IsNullOrWhiteSpace(client.ContactPersonEmail))
        {
            throw new InvalidOperationException("Add a client contact email before sending the readiness form.");
        }

        var settings = await GetActiveSettingsAsync(cancellationToken);
        var assessment = await EnsureGeneratedFormLinkAsync(clientId, cancellationToken: cancellationToken);
        var subject = ApplyTemplate(settings.EmailSubjectTemplate, client, assessment);
        var plainText = ApplyTemplate(settings.EmailBodyTemplate, client, assessment);
        var html = PlainTextToHtml(plainText);

        await emailService.SendAsync(client.ContactPersonEmail, subject, html, plainText);

        assessment.FormStatus = ReadinessFormStatus.Sent;
        assessment.SentAt = DateTime.UtcNow;
        assessment.SentToEmail = client.ContactPersonEmail;
        assessment.LastModifiedAt = DateTime.UtcNow;
        client.CurrentStage = ClientStage.AssessmentSent;
        client.NextAction = "Wait for Google Form completion or use manual import fallback";
        client.LastModifiedAt = DateTime.UtcNow;

        await MarkWorkflowAsync(clientId, "Assessment Sent", WorkflowStepStatus.Completed, cancellationToken);
        Log(clientId, "Readiness form sent", $"Readiness form sent to {client.ContactPersonEmail}.");
        await context.SaveChangesAsync(cancellationToken);
        return assessment;
    }

    public async Task<ReadinessAssessment> SendReminderAsync(int clientId, CancellationToken cancellationToken = default)
    {
        var client = await LoadClientAsync(clientId, cancellationToken)
            ?? throw new InvalidOperationException("Client was not found.");

        if (string.IsNullOrWhiteSpace(client.ContactPersonEmail))
        {
            throw new InvalidOperationException("Add a client contact email before sending a reminder.");
        }

        var settings = await GetActiveSettingsAsync(cancellationToken);
        var assessment = await EnsureGeneratedFormLinkAsync(clientId, cancellationToken: cancellationToken);
        var subject = $"Reminder: {ApplyTemplate(settings.EmailSubjectTemplate, client, assessment)}";
        var plainText = ApplyTemplate(settings.EmailBodyTemplate, client, assessment);
        var html = PlainTextToHtml(plainText);

        await emailService.SendAsync(client.ContactPersonEmail, subject, html, plainText);

        assessment.LastReminderSentAt = DateTime.UtcNow;
        assessment.LastModifiedAt = DateTime.UtcNow;
        client.NextAction = "Await readiness form response";
        client.LastModifiedAt = DateTime.UtcNow;

        Log(clientId, "Readiness form reminder sent", $"Readiness form reminder sent to {client.ContactPersonEmail}.");
        await context.SaveChangesAsync(cancellationToken);
        return assessment;
    }

    public string BuildGeneratedFormUrl(string baseFormUrl, string clientReferenceEntryId, string clientToken)
    {
        var query = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        string pathAndAuthority;
        string fragment = string.Empty;

        if (Uri.TryCreate(baseFormUrl, UriKind.Absolute, out var uri))
        {
            pathAndAuthority = uri.GetLeftPart(UriPartial.Path);
            fragment = uri.Fragment;
            AddQueryValues(query, uri.Query);
        }
        else
        {
            var fragmentIndex = baseFormUrl.IndexOf('#', StringComparison.Ordinal);
            if (fragmentIndex >= 0)
            {
                fragment = baseFormUrl[fragmentIndex..];
                baseFormUrl = baseFormUrl[..fragmentIndex];
            }

            var queryIndex = baseFormUrl.IndexOf('?', StringComparison.Ordinal);
            pathAndAuthority = queryIndex >= 0 ? baseFormUrl[..queryIndex] : baseFormUrl;
            AddQueryValues(query, queryIndex >= 0 ? baseFormUrl[queryIndex..] : string.Empty);
        }

        query["usp"] = "pp_url";
        query[clientReferenceEntryId.Trim()] = clientToken;

        var queryString = string.Join("&", query
            .Where(item => !string.IsNullOrWhiteSpace(item.Key))
            .Select(item => $"{Uri.EscapeDataString(item.Key)}={Uri.EscapeDataString(item.Value ?? string.Empty)}"));

        return $"{pathAndAuthority}?{queryString}{fragment}";
    }

    private async Task<ClientCompany?> LoadClientAsync(int clientId, CancellationToken cancellationToken)
    {
        return await context.ClientCompanies
            .Include(client => client.ReadinessAssessments)
            .SingleOrDefaultAsync(client => client.Id == clientId, cancellationToken);
    }

    private async Task<ReadinessFormSettings> GetActiveSettingsAsync(CancellationToken cancellationToken)
    {
        return await context.ReadinessFormSettings
            .Where(settings => settings.IsActive)
            .OrderByDescending(settings => settings.LastModifiedAt ?? settings.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            ?? new ReadinessFormSettings();
    }

    private ReadinessAssessment GetOrCreateOpenAssessment(ClientCompany client)
    {
        var assessment = client.ReadinessAssessments
            .OrderByDescending(item => item.CreatedAt)
            .FirstOrDefault(item => item.FormStatus is ReadinessFormStatus.NotSent or ReadinessFormStatus.Sent);

        if (assessment is not null)
        {
            return assessment;
        }

        assessment = new ReadinessAssessment
        {
            ClientCompanyId = client.Id,
            CreatedAt = DateTime.UtcNow
        };
        context.ReadinessAssessments.Add(assessment);
        return assessment;
    }

    private static string GenerateClientToken()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
    }

    private static void AddQueryValues(IDictionary<string, string?> values, string queryString)
    {
        if (string.IsNullOrWhiteSpace(queryString))
        {
            return;
        }

        foreach (var pair in queryString.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            var key = WebUtility.UrlDecode(parts[0]);
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            values[key] = parts.Length == 2 ? WebUtility.UrlDecode(parts[1]) : string.Empty;
        }
    }

    private static string ApplyTemplate(string template, ClientCompany client, ReadinessAssessment assessment)
    {
        return template
            .Replace("{{CompanyName}}", client.CompanyName, StringComparison.OrdinalIgnoreCase)
            .Replace("{{ContactPersonName}}", client.ContactPersonName ?? "there", StringComparison.OrdinalIgnoreCase)
            .Replace("{{FormLink}}", assessment.GeneratedFormUrl ?? assessment.FormUrl ?? string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{{AssignedConsultant}}", client.AssignedConsultant ?? "Your AI readiness consultant", StringComparison.OrdinalIgnoreCase);
    }

    private static string PlainTextToHtml(string text)
    {
        return WebUtility.HtmlEncode(text).Replace(Environment.NewLine, "<br />");
    }

    private async Task MarkWorkflowAsync(int clientId, string stageName, WorkflowStepStatus status, CancellationToken cancellationToken)
    {
        var step = await context.ClientWorkflowSteps
            .Where(item => item.ClientCompanyId == clientId && item.StageName == stageName)
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (step is null)
        {
            context.ClientWorkflowSteps.Add(new ClientWorkflowStep
            {
                ClientCompanyId = clientId,
                StageName = stageName,
                DisplayOrder = StakeholderWorkflow.GetDisplayOrder(stageName),
                Status = status,
                CompletedAt = status == WorkflowStepStatus.Completed ? DateTime.UtcNow : null
            });
            logger.LogDebug("Workflow step {StageName} was created for client {ClientId}.", stageName, clientId);
            return;
        }

        step.Status = status;
        step.CompletedAt = status == WorkflowStepStatus.Completed ? DateTime.UtcNow : step.CompletedAt;
    }

    private void Log(int clientId, string activityType, string description)
    {
        context.ClientActivityLogs.Add(new ClientActivityLog
        {
            ClientCompanyId = clientId,
            ActivityType = activityType,
            Description = description,
            CreatedBy = "Consultant",
            CreatedAt = DateTime.UtcNow
        });
    }
}
