using System;
using DevExpress.ExpressApp;

namespace Visa2026.Module.Services.OfficerShell;

/// <summary>
/// Circuit-scoped pending page/case for officer shell opens (Blazor sync).
/// </summary>
public static class OfficerShellPendingOpenGate
{
    public static void Set(XafApplication application, OfficerShellPage page, Guid caseApplicationId)
    {
        if (application?.ServiceProvider == null)
            return;

        if (application.ServiceProvider.GetService(typeof(IOfficerShellPendingOpen))
            is IOfficerShellPendingOpen pending)
        {
            pending.Page = page;
            pending.CaseApplicationId = caseApplicationId;
        }
    }

    public static (OfficerShellPage Page, Guid CaseApplicationId) Get(XafApplication application)
    {
        if (application?.ServiceProvider == null)
            return (OfficerShellPage.Staged, Guid.Empty);

        if (application.ServiceProvider.GetService(typeof(IOfficerShellPendingOpen))
            is IOfficerShellPendingOpen pending)
        {
            return (pending.Page, pending.CaseApplicationId);
        }

        return (OfficerShellPage.Staged, Guid.Empty);
    }
}
