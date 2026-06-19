using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Security;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF.PermissionPolicy;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.RuntimeLogging;

namespace Visa2026.Blazor.Server.Services;

/// <summary>
/// Spreadsheet iframe host runs on a normal HTTP request (outside the Blazor SignalR / ValueManager context).
/// Use cookie auth + non-secured ObjectSpace instead of <see cref="SecuritySystem"/>.
/// </summary>
public sealed class UserReportTemplateSpreadsheetHttpAccess
{
    private static readonly string UserReportTemplateTypeName = typeof(UserReportTemplate).FullName!;

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly INonSecuredObjectSpaceFactory _objectSpaceFactory;

    public UserReportTemplateSpreadsheetHttpAccess(
        IHttpContextAccessor httpContextAccessor,
        INonSecuredObjectSpaceFactory objectSpaceFactory)
    {
        _httpContextAccessor = httpContextAccessor;
        _objectSpaceFactory = objectSpaceFactory;
    }

    public string? GetAuthenticatedUserName()
    {
        var identity = _httpContextAccessor.HttpContext?.User?.Identity;
        return identity?.IsAuthenticated == true ? identity.Name : null;
    }

    public string? ResolveCurrentUserKey()
    {
        var userName = GetAuthenticatedUserName();
        if (string.IsNullOrWhiteSpace(userName))
            return null;

        using var objectSpace = _objectSpaceFactory.CreateNonSecuredObjectSpace(typeof(ApplicationUser));
        var userId = objectSpace.GetObjectsQuery<ApplicationUser>()
            .Where(user => user.UserName == userName)
            .Select(user => user.ID)
            .FirstOrDefault();

        return userId == Guid.Empty ? null : userId.ToString("N");
    }

    public bool CanReadTemplates() => HasTemplatePermission(requireWrite: false);

    public bool CanEditTemplates() => HasTemplatePermission(requireWrite: true);

    private bool HasTemplatePermission(bool requireWrite)
    {
        var userName = GetAuthenticatedUserName();
        if (string.IsNullOrWhiteSpace(userName))
            return false;

        using var objectSpace = _objectSpaceFactory.CreateNonSecuredObjectSpace(typeof(ApplicationUser));
        var user = objectSpace.GetObjectsQuery<ApplicationUser>()
            .Include(u => u.Roles)
            .ThenInclude(role => role.TypePermissions)
            .AsEnumerable()
            .FirstOrDefault(u => u.UserName != null
                && string.Equals(u.UserName, userName, StringComparison.OrdinalIgnoreCase));

        if (user == null)
            return false;

        if (ApplicationRuntimeLogAdminHelper.IsAdministratorUser(user))
            return true;

        foreach (var role in user.Roles)
        {
            foreach (var typePermission in role.TypePermissions)
            {
                if (!string.Equals(typePermission.TargetTypeFullName, UserReportTemplateTypeName, StringComparison.Ordinal))
                    continue;

                return requireWrite
                    ? typePermission.WriteState == SecurityPermissionState.Allow
                    : typePermission.ReadState == SecurityPermissionState.Allow;
            }
        }

        return false;
    }
}
