using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Visa2026.Blazor.Server.Services;
using Visa2026.Module.Services.LegacySyncDashboard;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Admin-only HTTP routes for the legacy sync HTML/JSON report on each IIS slot.
/// </summary>
[Authorize]
[Route(LegacySyncDashboardPaths.RoutePrefix)]
public sealed class LegacySyncDashboardPageController : Controller
{
    private readonly ILegacySyncDashboardService dashboardService;
    private readonly HttpApplicationRuntimeLogAdminChecker adminChecker;

    public LegacySyncDashboardPageController(
        ILegacySyncDashboardService dashboardService,
        HttpApplicationRuntimeLogAdminChecker adminChecker)
    {
        this.dashboardService = dashboardService;
        this.adminChecker = adminChecker;
    }

    [HttpGet("dashboard")]
    public IActionResult DashboardHtml()
    {
        if (!adminChecker.IsCurrentUserAdministrator())
            return Forbid();

        return ToActionResult(dashboardService.GetDashboardHtmlFile());
    }

    [HttpGet("dashboard.json")]
    public IActionResult DashboardJson()
    {
        if (!adminChecker.IsCurrentUserAdministrator())
            return Forbid();

        return ToActionResult(dashboardService.GetDashboardJsonFile());
    }

    private IActionResult ToActionResult(LegacySyncDashboardFileContent file)
    {
        if (file.Success && file.Content != null)
            return Content(file.Content, file.ContentType);

        var status = file.StatusCode ?? 404;
        return StatusCode(status, file.ErrorMessage ?? "Legacy sync dashboard unavailable.");
    }
}