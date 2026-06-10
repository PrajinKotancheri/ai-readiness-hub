using AI_Readiness_Hub.Models;
using AI_Readiness_Hub.Services;
using Microsoft.AspNetCore.Mvc;

namespace AI_Readiness_Hub.Controllers;

[Route("AIWorkspace")]
public class AIWorkspaceController(
    IAIWorkspaceService workspaceService,
    ILogger<AIWorkspaceController> logger) : Controller
{
    [HttpPost("Open")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Open(int clientId, AIOutputType outputType, int? outputId)
    {
        var sessionId = await workspaceService.OpenAsync(clientId, outputType, outputId);
        return RedirectToAction(nameof(Details), new { id = sessionId });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        try
        {
            var model = await workspaceService.LoadAsync(id);
            ViewData["Title"] = "AI Workspace";
            return View(model);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "AI Workspace could not be loaded. SessionId: {SessionId}", id);
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction("Index", "Dashboard");
        }
    }

    [HttpPost("{id:int}/Feedback")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendFeedback(int id, string currentDraft, string consultantFeedback)
    {
        try
        {
            await workspaceService.RefineAsync(id, currentDraft, consultantFeedback);
            TempData["SuccessMessage"] = "AI returned an improved draft.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("{id:int}/SaveDraft")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveDraft(int id, string currentDraft)
    {
        await workspaceService.SaveDraftAsync(id, currentDraft);
        TempData["SuccessMessage"] = "Workspace draft saved.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("{id:int}/ApproveFinal")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveFinal(int id, string currentDraft)
    {
        await workspaceService.ApproveFinalAsync(id, currentDraft);
        TempData["SuccessMessage"] = "Final draft approved and applied.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("{id:int}/Close")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(int id)
    {
        var model = await workspaceService.LoadAsync(id);
        await workspaceService.CloseAsync(id);
        TempData["SuccessMessage"] = "AI Workspace closed.";
        return RedirectToAction("Workspace", "Clients", this.ToWorkspaceRouteValues(model.Session.ClientCompanyId));
    }
}
