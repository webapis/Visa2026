using System;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects.ApplicationWorkspace;
using Visa2026.Module.Services.ApplicationWorkspace;

namespace Visa2026.Module.Controllers;

public sealed class ApplicationWorkspaceHostViewController : ViewController<DetailView>
{
    public ApplicationWorkspaceHostViewController() =>
        TargetViewId = ApplicationWorkspaceViewIds.DetailView;

    protected override void OnActivated()
    {
        base.OnActivated();
        EnsureHostApplicationId();
    }

    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        EnsureHostApplicationId();
    }

    private void EnsureHostApplicationId()
    {
        var applicationId = ApplicationWorkspacePendingOpenGate.Get(Application);
        if (applicationId == Guid.Empty)
            return;

        ApplicationWorkspaceHost host;
        if (View.CurrentObject is ApplicationWorkspaceHost current)
        {
            host = current;
        }
        else
        {
            host = ObjectSpace.CreateObject<ApplicationWorkspaceHost>();
            View.CurrentObject = host;
        }

        if (host.ApplicationId == Guid.Empty)
            host.ApplicationId = applicationId;
    }
}
