using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects.ApplicationProfileCatalog;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Ensures the catalog DetailView has a non-persistent host object (nav / URL reopen).
/// </summary>
public sealed class ApplicationProfileCatalogHostViewController : ViewController<DetailView>
{
    public ApplicationProfileCatalogHostViewController() =>
        TargetViewId = ApplicationProfileCatalogViewIds.DetailView;

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
        if (View.CurrentObject is ApplicationProfileCatalogHost)
            return;

        var host = ObjectSpace.CreateObject<ApplicationProfileCatalogHost>();
        View.CurrentObject = host;
    }
}