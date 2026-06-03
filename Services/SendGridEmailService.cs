using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AI_Readiness_Hub.Services;

public class SendGridEmailService(
    IConfiguration configuration,
    IWebHostEnvironment environment,
    ILogger<SendGridEmailService> logger) : IEmailService
{
    private static readonly HttpClient HttpClient = new()
    {
        BaseAddress = new Uri("https://api.sendgrid.com")
    };

    public async Task SendAsync(string to, string subject, string htmlBody, string? plainTextBody = null)
    {
        var emailConfiguration = EmailConfiguration.From(configuration);

        if (string.IsNullOrWhiteSpace(to))
        {
            throw new InvalidOperationException("Email recipient is required.");
        }

        if (environment.IsDevelopment())
        {
            logger.LogInformation(
                "Email configuration sources. SMTP_PASSWORD env: {ApiKeyEnv}; user-secrets: {ApiKeySecrets}; appsettings: {ApiKeyAppSettings}. Smtp:FromEmail env: {FromEmailEnv}; user-secrets: {FromEmailSecrets}; appsettings: {FromEmailAppSettings}. Smtp:FromName env: {FromNameEnv}; user-secrets: {FromNameSecrets}; appsettings: {FromNameAppSettings}.",
                emailConfiguration.ApiKeySources.EnvironmentVariables,
                emailConfiguration.ApiKeySources.UserSecrets,
                emailConfiguration.ApiKeySources.AppSettings,
                emailConfiguration.FromEmailSources.EnvironmentVariables,
                emailConfiguration.FromEmailSources.UserSecrets,
                emailConfiguration.FromEmailSources.AppSettings,
                emailConfiguration.FromNameSources.EnvironmentVariables,
                emailConfiguration.FromNameSources.UserSecrets,
                emailConfiguration.FromNameSources.AppSettings);
        }

        if (!emailConfiguration.IsComplete)
        {
            throw new InvalidOperationException($"Email configuration is missing: {string.Join(", ", emailConfiguration.MissingRequiredKeys)}. {emailConfiguration.CheckedSourcesSummary}");
        }

        var payload = new
        {
            personalizations = new[]
            {
                new
                {
                    to = new[] { new { email = to.Trim() } },
                    subject
                }
            },
            from = new
            {
                email = emailConfiguration.FromEmail,
                name = string.IsNullOrWhiteSpace(emailConfiguration.FromName) ? null : emailConfiguration.FromName
            },
            content = new[]
            {
                new
                {
                    type = "text/plain",
                    value = plainTextBody ?? HtmlToPlainTextFallback(htmlBody)
                },
                new
                {
                    type = "text/html",
                    value = htmlBody
                }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/v3/mail/send");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", emailConfiguration.ApiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        try
        {
            using var response = await HttpClient.SendAsync(request);
            if ((int)response.StatusCode is >= 200 and <= 299)
            {
                return;
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            logger.LogError(
                "SendGrid send failed with status {StatusCode}. Response: {ResponseBody}",
                response.StatusCode,
                responseBody);

            throw new InvalidOperationException($"SendGrid email could not be sent. Status code: {response.StatusCode}. {responseBody}");
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SendGrid send failed");
            throw new InvalidOperationException(ex.Message, ex);
        }
    }

    private static string HtmlToPlainTextFallback(string htmlBody)
    {
        if (string.IsNullOrWhiteSpace(htmlBody))
        {
            return string.Empty;
        }

        return WebUtility.HtmlDecode(htmlBody)
            .Replace("<br>", Environment.NewLine, StringComparison.OrdinalIgnoreCase)
            .Replace("<br/>", Environment.NewLine, StringComparison.OrdinalIgnoreCase)
            .Replace("<br />", Environment.NewLine, StringComparison.OrdinalIgnoreCase)
            .Replace("</p>", $"{Environment.NewLine}{Environment.NewLine}", StringComparison.OrdinalIgnoreCase)
            .Replace("&nbsp;", " ", StringComparison.OrdinalIgnoreCase);
    }
}
