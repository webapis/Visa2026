using DevExpress.ExpressApp;

namespace Visa2026.Module.Services.ApplicationProfileOverview;

public static class ApplicationProfileOverviewPendingOpenGate
{
    public static void Set(XafApplication application, Guid applicationProfileId)
    {
        if (application?.ServiceProvider == null || applicationProfileId == Guid.Empty)
            return;

        if (application.ServiceProvider.GetService(typeof(IApplicationProfileOverviewPendingOpen))
            is IApplicationProfileOverviewPendingOpen pending)
        {
            pending.ApplicationProfileId = applicationProfileId;
        }
    }

    public static Guid Get(XafApplication application)
    {
        if (application?.ServiceProvider == null)
            return Guid.Empty;

        return application.ServiceProvider.GetService(typeof(IApplicationProfileOverviewPendingOpen))
            is IApplicationProfileOverviewPendingOpen pending
            ? pending.ApplicationProfileId
            : Guid.Empty;
    }
}
