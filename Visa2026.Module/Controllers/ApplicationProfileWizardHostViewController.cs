using System;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects.ApplicationProfileWizard;
using Visa2026.Module.Services.ApplicationProfileWizard;

namespace Visa2026.Module.Controllers;

public sealed class ApplicationProfileWizardHostViewController : ViewController<DetailView>
{
    public ApplicationProfileWizardHostViewController() =>
        TargetViewId = ApplicationProfileWizardViewIds.DetailView;

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
        var profileId = ApplicationProfileWizardPendingOpenGate.Get(Application);
        if (profileId == Guid.Empty)
            return;

        ApplicationProfileWizardHost host;
        if (View.CurrentObject is ApplicationProfileWizardHost current)
        {
            host = current;
        }
        else
        {
            host = ObjectSpace.CreateObject<ApplicationProfileWizardHost>();
            View.CurrentObject = host;
        }

        if (host.ApplicationProfileId == Guid.Empty)
            host.ApplicationProfileId = profileId;
    }
}
