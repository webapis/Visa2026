using System;
using DevExpress.ExpressApp;

namespace Visa2026.Module.Services.ApplicationWorkspace;

/// <summary>
/// Circuit-scoped pending application id for workspace opens (Blazor URL sync).
/// </summary>
public static class ApplicationWorkspacePendingOpenGate
{
    public static void Set(XafApplication application, Guid applicationId)
    {
        if (application?.ServiceProvider == null)
            return;

        if (application.ServiceProvider.GetService(typeof(IApplicationWorkspacePendingOpen))
            is IApplicationWorkspacePendingOpen pending)
        {
            pending.ApplicationProfileInstanceId = applicationId;
        }
    }

    public static Guid Get(XafApplication application)
    {
        if (application?.ServiceProvider == null)
            return Guid.Empty;

        if (application.ServiceProvider.GetService(typeof(IApplicationWorkspacePendingOpen))
            is IApplicationWorkspacePendingOpen pending)
        {
            return pending.ApplicationProfileInstanceId;
        }

        return Guid.Empty;
    }
}
