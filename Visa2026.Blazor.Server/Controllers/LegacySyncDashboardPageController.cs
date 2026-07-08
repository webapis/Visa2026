using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Visa2026.Blazor.Server.Services;
using Visa2026.Module.Services.LegacySyncDashboard;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// HTTP routes for the legacy sync HTML/JSON report on each IIS slot.
/// HTML is readable on the LAN when the dashboard is enabled; JSON stays admin-only.
/// </summary>
[Route(LegacySyncDashboardPaths.RoutePrefix)]
public sealed class LegacySyncDashboardPageController : Controller
{
    private readonly ILegacySyncDashboardService dashboardService;
    private readonly HttpApplicationRuntimeLogAdminChecker adminChecker;
    private readonly LegacySyncDashboardOptions options;

    public LegacySyncDashboardPageController(
        ILegacySyncDashboardService dashboardService,
        HttpApplicationRuntimeLogAdminChecker adminChecker,
        IOptions<LegacySyncDashboardOptions> options)
    {
        this.dashboardService = dashboardService;
        this.adminChecker = adminChecker;
        this.options = options.Value;
    }

    [HttpGet("dashboard")]
    [AllowAnonymous]
    public IActionResult DashboardHtml()
    {
        if (!options.Enabled)
            return NotFound("Legacy sync dashboard is disabled in configuration.");

        return ToActionResult(dashboardService.GetDashboardHtmlFile());
    }

    [HttpGet("dashboard.json")]
    [Authorize]
    public IActionResult DashboardJson()
    {
        if (!options.Enabled)
            return NotFound("Legacy sync dashboard is disabled in configuration.");

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