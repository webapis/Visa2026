using DevExpress.ExpressApp;
using DevExpress.ExpressApp.SystemModule;
using Visa2026.Module.BusinessObjects.PersonIntake;

namespace Visa2026.Module.Controllers;

/// <summary>
/// The intake wizard owns save. Hide the standard XAF Modifications toolbar, same as the Application Profile wizard.
/// </summary>
public sealed class PersonIntakeWizardChromeController : ViewController
{
    private const string Reason = "PersonIntakeWizard";

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
        if (View is not DetailView detailView || detailView.Id != PersonIntakeWizardViewIds.DetailView)
            return;

        detailView.AllowDelete.SetItemValue(Reason, false);
        detailView.AllowNew.SetItemValue(Reason, false);

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
