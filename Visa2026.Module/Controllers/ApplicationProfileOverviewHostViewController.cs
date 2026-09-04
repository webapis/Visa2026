using System;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects.ApplicationProfileOverview;
using Visa2026.Module.Services.ApplicationProfileOverview;

namespace Visa2026.Module.Controllers;

public sealed class ApplicationProfileOverviewHostViewController : ViewController<DetailView>
{
    public ApplicationProfileOverviewHostViewController() =>
        TargetViewId = ApplicationProfileOverviewViewIds.DetailView;

    protected override void OnActivated()
    {
        base.OnActivated();
        EnsureHostProfileId();
    }

    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        EnsureHostProfileId();
    }

    private void EnsureHostProfileId()
    {
        var profileId = ApplicationProfileOverviewPendingOpenGate.Get(Application);
        if (profileId == Guid.Empty)
            return;

        ApplicationProfileOverviewHost host;
        if (View.CurrentObject is ApplicationProfileOverviewHost current)
        {
            host = current;
        }
        else
        {
            host = ObjectSpace.CreateObject<ApplicationProfileOverviewHost>();
            View.CurrentObject = host;
        }

        if (host.ApplicationProfileId == Guid.Empty)
            host.ApplicationProfileId = profileId;
    }
}
