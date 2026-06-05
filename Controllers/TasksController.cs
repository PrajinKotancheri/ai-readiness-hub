using AI_Readiness_Hub.Data;
using AI_Readiness_Hub.Models;
using Microsoft.AspNetCore.Mvc;

namespace AI_Readiness_Hub.Controllers;

[Route("Tasks")]
public class TasksController(ApplicationDbContext context) : Controller
{
    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int clientId, ClientTask task)
    {
        task.ClientCompanyId = clientId;
        task.CreatedAt = DateTime.UtcNow;
        context.ClientTasks.Add(task);
        await LogAsync(clientId, "Task created", $"Task created: {task.TaskTitle}.");
        await context.SaveChangesAsync();
        return RedirectToWorkspace(clientId);
    }

    [HttpPost("Complete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id)
    {
        var task = await context.ClientTasks.FindAsync(id);
        if (task is null)
        {
            return NotFound();
        }

        task.Status = ClientTaskStatus.Done;
        task.LastModifiedAt = DateTime.UtcNow;
        await LogAsync(task.ClientCompanyId, "Task completed", $"Task completed: {task.TaskTitle}.");
        await context.SaveChangesAsync();
        return RedirectToWorkspace(task.ClientCompanyId);
    }

    [HttpPost("Update")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, ClientTaskStatus status, TaskPriority priority)
    {
        var task = await context.ClientTasks.FindAsync(id);
        if (task is null)
        {
            return NotFound();
        }

        task.Status = status;
        task.Priority = priority;
        task.LastModifiedAt = DateTime.UtcNow;
        await LogAsync(task.ClientCompanyId, "Task updated", $"Task updated: {task.TaskTitle}.");
        await context.SaveChangesAsync();
        return RedirectToWorkspace(task.ClientCompanyId);
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
        return RedirectToAction("Workspace", "Clients", this.ToWorkspaceRouteValues(clientId));
    }
}
