using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects.ApplicationProfilePicker;

namespace Visa2026.Module.Controllers;

public sealed class ApplicationProfilePickerHostViewController : ViewController<DetailView>
{
    public ApplicationProfilePickerHostViewController() =>
        TargetViewId = ApplicationProfilePickerViewIds.DetailView;

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
        if (View.CurrentObject is ApplicationProfilePickerHost)
            return;

        View.CurrentObject = ObjectSpace.CreateObject<ApplicationProfilePickerHost>();
    }
}
