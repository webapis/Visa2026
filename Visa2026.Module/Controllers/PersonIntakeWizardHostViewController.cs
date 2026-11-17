using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects.PersonIntake;

namespace Visa2026.Module.Controllers;

public sealed class PersonIntakeWizardHostViewController : ViewController<DetailView>
{
    public PersonIntakeWizardHostViewController() =>
        TargetViewId = PersonIntakeWizardViewIds.DetailView;

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
        if (View.CurrentObject is PersonIntakeWizardHost)
            return;

        View.CurrentObject = ObjectSpace.CreateObject<PersonIntakeWizardHost>();
    }
}
