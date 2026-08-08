using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Disables Application Profile DetailView editing when <see cref="ApplicationProfile.IsConfigLocked"/>
/// and refreshes after linked Application progress changes.
/// </summary>
public sealed class ApplicationProfileDetailViewController : ObjectViewController<DetailView, ApplicationProfile>
{
    protected override void OnActivated()
    {
        base.OnActivated();
        ObjectSpace.ObjectChanged += ObjectSpace_ObjectChanged;
        ObjectSpace.Committed += ObjectSpace_Committed;
        UpdateEditState();
    }

    protected override void OnDeactivated()
    {
        ObjectSpace.ObjectChanged -= ObjectSpace_ObjectChanged;
        ObjectSpace.Committed -= ObjectSpace_Committed;
        base.OnDeactivated();
    }

    private void ObjectSpace_Committed(object? sender, EventArgs e) => UpdateEditState();

    private void ObjectSpace_ObjectChanged(object? sender, ObjectChangedEventArgs e)
    {
        if (e.Object is ApplicationProgress progress
            && progress.Application?.ApplicationProfile != null
            && ReferenceEquals(progress.Application.ApplicationProfile, ViewCurrentObject)
            && e.PropertyName is nameof(ApplicationProgress.State))
        {
            UpdateEditState();
        }
    }

    private void UpdateEditState()
    {
        if (View == null)
            return;

        var locked = ApplicationProfileLockHelper.IsProfileConfigLocked(ViewCurrentObject, ObjectSpace);
        View.AllowEdit["ApplicationProfileConfigLocked"] = !locked;
    }
}
