using DevExpress.ExpressApp;
using Microsoft.AspNetCore.Http;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.RuntimeLogging;

namespace Visa2026.Blazor.Server.Services;

/// <summary>
/// Admin check for _Host-level Blazor components that run outside XAF ValueManager context.
/// </summary>
public sealed class HttpApplicationRuntimeLogAdminChecker
{
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly IServiceScopeFactory scopeFactory;

    public HttpApplicationRuntimeLogAdminChecker(
        IHttpContextAccessor httpContextAccessor,
        IServiceScopeFactory scopeFactory)
    {
        this.httpContextAccessor = httpContextAccessor;
        this.scopeFactory = scopeFactory;
    }

    public bool IsCurrentUserAdministrator()
    {
        var userName = GetAuthenticatedUserName();
        if (userName == null)
            return false;

        using var scope = scopeFactory.CreateScope();
        var objectSpaceFactory = scope.ServiceProvider.GetRequiredService<INonSecuredObjectSpaceFactory>();
        using var objectSpace = objectSpaceFactory.CreateNonSecuredObjectSpace(typeof(ApplicationUser));
        return ApplicationRuntimeLogAdminHelper.IsUserNameAdministrator(objectSpace, userName);
    }

    private string? GetAuthenticatedUserName()
    {
        var identity = httpContextAccessor.HttpContext?.User?.Identity;
        return identity?.IsAuthenticated == true ? identity.Name : null;
    }
}
