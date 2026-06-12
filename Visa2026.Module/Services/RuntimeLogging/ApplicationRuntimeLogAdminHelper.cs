using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.RuntimeLogging;

public static class ApplicationRuntimeLogAdminHelper
{
    public static bool IsAdministratorUser(ApplicationUser? user) =>
        user?.Roles?.Any(role => role.IsAdministrative) == true;

    public static bool IsCurrentUserAdministrator()
    {
        try
        {
            return SecuritySystem.CurrentUser is ApplicationUser user && IsAdministratorUser(user);
        }
        catch (InvalidOperationException)
        {
            // Outside XAF ValueManager context (e.g. _Host header components).
            return false;
        }
    }

    public static bool IsUserNameAdministrator(IObjectSpace objectSpace, string? userName)
    {
        if (string.IsNullOrWhiteSpace(userName))
            return false;

        var user = objectSpace.GetObjectsQuery<ApplicationUser>()
            .AsEnumerable()
            .FirstOrDefault(u => u.UserName != null
                && string.Equals(u.UserName, userName, StringComparison.OrdinalIgnoreCase));

        return IsAdministratorUser(user);
    }
}
