using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects.OfficerShell;
using Visa2026.Module.Services.OfficerShell;

namespace Visa2026.Module.Controllers;

public sealed class OfficerShellHostViewController : ViewController<DetailView>
{
    public OfficerShellHostViewController() =>
        TargetViewId = OfficerShellViewIds.DetailView;

    protected override void OnActivated()
    {
        base.OnActivated();
        EnsureHost();
    }

    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        EnsureHost();
    }

    private void EnsureHost()
    {
        if (View.CurrentObject is OfficerShellHost)
            return;

        var host = ObjectSpace.CreateObject<OfficerShellHost>();
        View.CurrentObject = host;
    }
}
