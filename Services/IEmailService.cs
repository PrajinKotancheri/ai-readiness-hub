namespace AI_Readiness_Hub.Services;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string htmlBody, string? plainTextBody = null);
}
