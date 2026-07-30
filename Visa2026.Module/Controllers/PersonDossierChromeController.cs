using DevExpress.ExpressApp;
using DevExpress.ExpressApp.SystemModule;
using Visa2026.Module.BusinessObjects.PersonDossier;

namespace Visa2026.Module.Controllers;

/// <summary>
/// The dossier is a read-only hand-over page, so the standard CRUD chrome is hidden.
/// </summary>
public sealed class PersonDossierChromeController : ViewController
{
    private const string Reason = "PersonDossier";

    protected override void OnActivated()
    {
        base.OnActivated();
        HideCrudChrome();
    }

    /// <summary>
    /// Reapplied here because when the dossier replaces the current view, the standard controllers
    /// activate after this one and reinstate their actions.
    /// </summary>
    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        HideCrudChrome();
    }

    private void HideCrudChrome()
    {
        // A typed ObjectViewController<DetailView, PersonDossierHost> does not activate for this
        // non-persistent view, so the view id is matched directly.
        if (View is not DetailView detailView || detailView.Id != PersonDossierViewIds.DetailView)
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

        Frame.GetController<RefreshController>()?
            .RefreshAction.Active.SetItemValue(Reason, false);
    }
}
