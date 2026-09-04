using System;
using DevExpress.ExpressApp;

namespace Visa2026.Module.Services.OfficerShell;

/// <summary>
/// Circuit-scoped pending page/case for officer shell opens (Blazor sync).
/// </summary>
public static class OfficerShellPendingOpenGate
{
    public static void Set(XafApplication application, OfficerShellPage page, Guid caseApplicationProfileInstanceId)
    {
        if (application?.ServiceProvider == null)
            return;

        if (application.ServiceProvider.GetService(typeof(IOfficerShellPendingOpen))
            is IOfficerShellPendingOpen pending)
        {
            pending.Page = page;
            pending.CaseApplicationProfileInstanceId = caseApplicationProfileInstanceId;
        }
    }

    public static (OfficerShellPage Page, Guid CaseApplicationProfileInstanceId) Get(XafApplication application)
    {
        if (application?.ServiceProvider == null)
            return (OfficerShellPage.Staged, Guid.Empty);

        if (application.ServiceProvider.GetService(typeof(IOfficerShellPendingOpen))
            is IOfficerShellPendingOpen pending)
        {
            return (pending.Page, pending.CaseApplicationProfileInstanceId);
        }

        return (OfficerShellPage.Staged, Guid.Empty);
    }
}
