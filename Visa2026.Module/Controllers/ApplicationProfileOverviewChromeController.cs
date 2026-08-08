using DevExpress.ExpressApp;
using DevExpress.ExpressApp.SystemModule;
using Visa2026.Module.BusinessObjects.ApplicationProfileOverview;

namespace Visa2026.Module.Controllers;

public sealed class ApplicationProfileOverviewChromeController : ViewController
{
    private const string Reason = "ApplicationProfileOverview";

    protected override void OnActivated()
    {
        base.OnActivated();
        HideCrudChrome();
    }

    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        HideCrudChrome();
    }

    private void HideCrudChrome()
    {
        if (View is not DetailView detailView || detailView.Id != ApplicationProfileOverviewViewIds.DetailView)
            return;

        detailView.AllowDelete.SetItemValue(Reason, false);
        detailView.AllowNew.SetItemValue(Reason, false);
        detailView.AllowEdit.SetItemValue(Reason, false);

        var modifications = Frame.GetController<ModificationsController>();
        if (modifications != null)
        {
            modifications.SaveAction.Active.SetItemValue(Reason, false);
            modifications.SaveAndCloseAction.Active.SetItemValue(Reason, false);
            modifications.SaveAndNewAction.Active.SetItemValue(Reason, false);
        }

        Frame.GetController<DeleteObjectsViewController>()?
            .DeleteAction.Active.SetItemValue(Reason, false);
    }
}
