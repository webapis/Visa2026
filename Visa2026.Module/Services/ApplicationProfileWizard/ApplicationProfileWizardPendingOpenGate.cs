using System;
using DevExpress.ExpressApp;

namespace Visa2026.Module.Services.ApplicationProfileWizard;

public static class ApplicationProfileWizardPendingOpenGate
{
    public static void Set(XafApplication application, Guid applicationProfileId)
    {
        if (application?.ServiceProvider == null)
            return;

        if (application.ServiceProvider.GetService(typeof(IApplicationProfileWizardPendingOpen))
            is IApplicationProfileWizardPendingOpen pending)
        {
            pending.ApplicationProfileId = applicationProfileId;
        }
    }

    public static Guid Get(XafApplication application)
    {
        if (application?.ServiceProvider == null)
            return Guid.Empty;

        if (application.ServiceProvider.GetService(typeof(IApplicationProfileWizardPendingOpen))
            is IApplicationProfileWizardPendingOpen pending)
        {
            return pending.ApplicationProfileId;
        }

        return Guid.Empty;
    }
}
