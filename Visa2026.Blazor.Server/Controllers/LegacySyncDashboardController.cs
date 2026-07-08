using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Visa2026.Blazor.Server.Services;
using Visa2026.Module.Services.LegacySyncDashboard;

namespace Visa2026.Blazor.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class LegacySyncDashboardController : ControllerBase
{
    private readonly ILegacySyncDashboardService dashboardService;
    private readonly HttpApplicationRuntimeLogAdminChecker adminChecker;

    public LegacySyncDashboardController(
        ILegacySyncDashboardService dashboardService,
        HttpApplicationRuntimeLogAdminChecker adminChecker)
    {
        this.dashboardService = dashboardService;
        this.adminChecker = adminChecker;
    }

    [HttpGet]
    public ActionResult<LegacySyncDashboardSnapshot> Get()
    {
        if (!adminChecker.IsCurrentUserAdministrator())
            return Forbid();

        return Ok(dashboardService.GetSnapshot());
    }
}
