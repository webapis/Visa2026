using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.RuntimeLogging;

public static class ApplicationRuntimeLogAdminHelper
{
    public static bool IsCurrentUserAdministrator()
    {
        if (SecuritySystem.CurrentUser is not ApplicationUser user)
            return false;

        return user.Roles?.Any(role => role.IsAdministrative) == true;
    }
}
