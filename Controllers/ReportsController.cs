using System.Net;
using System.Text;
using AI_Readiness_Hub.Data;
using AI_Readiness_Hub.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AI_Readiness_Hub.Controllers;

[Route("Reports")]
public class ReportsController(ApplicationDbContext context) : Controller
{
    [HttpPost("UpdateStatus")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int reportId, ReportStatus reportStatus)
    {
        var report = await context.ClientReports
            .Include(item => item.ClientCompany)
            .FirstOrDefaultAsync(item => item.Id == reportId);
        if (report is null)
        {
            return NotFound();
        }

        report.ReportStatus = reportStatus;
        report.LastModifiedAt = DateTime.UtcNow;
        report.ReviewedAt = reportStatus is ReportStatus.ReadyForDelivery or ReportStatus.Delivered ? DateTime.UtcNow : report.ReviewedAt;
        report.DeliveredAt = reportStatus == ReportStatus.Delivered ? DateTime.UtcNow : report.DeliveredAt;

        if (report.ClientCompany is not null)
        {
            report.ClientCompany.CurrentStage = reportStatus switch
            {
                ReportStatus.InConsultantReview => ClientStage.InReview,
                ReportStatus.Delivered => ClientStage.Delivered,
                ReportStatus.Closed => ClientStage.Closed,
                _ => report.ClientCompany.CurrentStage
            };
            report.ClientCompany.NextAction = reportStatus switch
            {
                ReportStatus.InConsultantReview => "Complete consultant review",
                ReportStatus.ReadyForDelivery => "Deliver final report",
                ReportStatus.Delivered => "Collect feedback",
                ReportStatus.Closed => "Archive engagement",
                _ => report.ClientCompany.NextAction
            };
        }

        await LogAsync(report.ClientCompanyId, "Report status changed", $"Report status changed to {reportStatus}.");
        await context.SaveChangesAsync();
        return RedirectToWorkspace(report.ClientCompanyId);
    }

    [HttpPost("Section")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Section(int sectionId, string? sectionContent, SectionStatus sectionStatus)
    {
        var section = await context.ReportSections
            .Include(item => item.ClientReport)
            .FirstOrDefaultAsync(item => item.Id == sectionId);
        if (section?.ClientReport is null)
        {
            return NotFound();
        }

        section.SectionContent = sectionContent;
        section.SectionStatus = sectionStatus;
        section.LastModifiedAt = DateTime.UtcNow;
        await LogAsync(section.ClientReport.ClientCompanyId, "Report section updated", $"{section.SectionTitle} updated.");
        await context.SaveChangesAsync();
        return RedirectToWorkspace(section.ClientReport.ClientCompanyId);
    }

    [HttpGet("Export/{reportId:int}")]
    public async Task<IActionResult> Export(int reportId)
    {
        var report = await context.ClientReports
            .Include(item => item.ClientCompany)
            .Include(item => item.Sections)
            .FirstOrDefaultAsync(item => item.Id == reportId);
        if (report is null)
        {
            return NotFound();
        }

        var builder = new StringBuilder();
        builder.AppendLine($"# {report.ReportTitle}");
        builder.AppendLine();
        builder.AppendLine($"Client: {report.ClientCompany?.CompanyName}");
        builder.AppendLine($"Status: {report.ReportStatus}");
        builder.AppendLine();

        foreach (var section in report.Sections.OrderBy(item => item.SectionOrder))
        {
            builder.AppendLine($"## {section.SectionTitle}");
            builder.AppendLine(WebUtility.HtmlDecode(section.SectionContent ?? string.Empty));
            builder.AppendLine();
        }

        return File(Encoding.UTF8.GetBytes(builder.ToString()), "text/markdown", $"{SanitizeFileName(report.ReportTitle)}.md");
    }

    private static string SanitizeFileName(string value)
    {
        foreach (var invalid in Path.GetInvalidFileNameChars())
        {
            value = value.Replace(invalid, '-');
        }

        return value;
    }

    private Task LogAsync(int clientId, string activityType, string description)
    {
        context.ClientActivityLogs.Add(new ClientActivityLog
        {
            ClientCompanyId = clientId,
            ActivityType = activityType,
            Description = description,
            CreatedBy = "Consultant",
            CreatedAt = DateTime.UtcNow
        });
        return Task.CompletedTask;
    }

    private RedirectToActionResult RedirectToWorkspace(int clientId)
    {
        return RedirectToAction("Workspace", "Clients", new { id = clientId });
    }
}
