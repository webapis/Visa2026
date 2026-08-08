using System;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationProfileWizard;

namespace Visa2026.Blazor.Server.Services;

public sealed class ApplicationProfileWizardPendingOpen : IApplicationProfileWizardPendingOpen
{
    public Guid ApplicationProfileId { get; set; }
}

public sealed class ApplicationProfileWizardSession : IApplicationProfileWizardSession
{
    public IObjectSpace? ObjectSpace { get; set; }

    public XafApplication? Application { get; set; }

    public Guid ApplicationProfileId { get; set; }

    public ApplicationProfile? GetProfile()
    {
        if (ObjectSpace == null || ApplicationProfileId == Guid.Empty)
            return null;

        return ObjectSpace.GetObjectByKey<ApplicationProfile>(ApplicationProfileId);
    }
}
