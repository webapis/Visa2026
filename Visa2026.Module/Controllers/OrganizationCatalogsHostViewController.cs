using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects.OrganizationCatalogs;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Ensures the Organization catalogs DetailView has a non-persistent host object (nav / URL reopen).
/// </summary>
public sealed class OrganizationCatalogsHostViewController : ViewController<DetailView>
{
    public OrganizationCatalogsHostViewController() =>
        TargetViewId = OrganizationCatalogsViewIds.DetailView;

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
        if (View.CurrentObject is OrganizationCatalogsHost)
            return;

        var host = ObjectSpace.CreateObject<OrganizationCatalogsHost>();
        View.CurrentObject = host;
    }
}
