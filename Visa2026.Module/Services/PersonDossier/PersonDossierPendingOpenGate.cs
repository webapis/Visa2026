using System;
using DevExpress.ExpressApp;

namespace Visa2026.Module.Services.PersonDossier;

/// <summary>
/// Stores pending dossier person id for Blazor Server (scoped per circuit).
/// </summary>
public static class PersonDossierPendingOpenGate
{
    public static void Set(XafApplication application, Guid personId)
    {
        if (application?.ServiceProvider == null)
            return;

        var pending = application.ServiceProvider.GetService(typeof(IPersonDossierPendingOpen))
            as IPersonDossierPendingOpen;
        if (pending != null)
            pending.PersonId = personId;
    }

    public static Guid Get(XafApplication application)
    {
        if (application?.ServiceProvider == null)
            return Guid.Empty;

        var pending = application.ServiceProvider.GetService(typeof(IPersonDossierPendingOpen))
            as IPersonDossierPendingOpen;
        return pending?.PersonId ?? Guid.Empty;
    }
}
