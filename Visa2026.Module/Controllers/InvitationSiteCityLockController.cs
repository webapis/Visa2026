using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using DevExpress.Persistent.BaseImpl.EF;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationWorkspace;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Keeps City on a lodging, hotel, hospital, or other site created from the invitation case
/// equal to the invitation city already chosen.
/// </summary>
public sealed class InvitationSiteCityLockController : ObjectViewController<DetailView, BaseObject>
{
    private const string Reason = "InvitationSiteCity";

    protected override void OnActivated()
    {
        base.OnActivated();
        if (View.Tag is not InvitationSiteCreateContext)
            return;

        ObjectSpace.ObjectSaving += ObjectSpaceOnObjectSaving;
        LockCity();
    }

    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        LockCity();
    }

    protected override void OnDeactivated()
    {
        ObjectSpace.ObjectSaving -= ObjectSpaceOnObjectSaving;
        base.OnDeactivated();
    }

    private void LockCity()
    {
        if (View.Tag is not InvitationSiteCreateContext)
            return;

        if (View.FindItem(nameof(Lodging.City)) is PropertyEditor editor)
            editor.AllowEdit.SetItemValue(Reason, false);
    }

    private void ObjectSpaceOnObjectSaving(object sender, ObjectManipulatingEventArgs e)
    {
        if (View.Tag is not InvitationSiteCreateContext context)
            return;

        var city = ObjectSpace.GetObjectByKey<City>(context.CityId);
        if (city != null)
            InvitationSiteCity.Assign(e.Object, city);
    }
}