using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ConditionalAppearance;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Refreshes <see cref="Application"/> ListView row colors when <see cref="ApplicationProgress"/> changes.
/// </summary>
public sealed class ApplicationProgressRowStateRefreshController : ViewController
{
    protected override void OnActivated()
    {
        base.OnActivated();
        ObjectSpace.ObjectChanged += ObjectSpace_ObjectChanged;
        ObjectSpace.Committed += ObjectSpace_Committed;
    }

    protected override void OnDeactivated()
    {
        ObjectSpace.ObjectChanged -= ObjectSpace_ObjectChanged;
        ObjectSpace.Committed -= ObjectSpace_Committed;
        base.OnDeactivated();
    }

    private void ObjectSpace_Committed(object sender, EventArgs e) => RefreshAppearance();

    private void ObjectSpace_ObjectChanged(object sender, ObjectChangedEventArgs e)
    {
        if (e.Object is ApplicationProgress progress
            && (e.PropertyName is nameof(ApplicationProgress.State)
                or nameof(ApplicationProgress.Location)
                or nameof(ApplicationProgress.Date)
                or nameof(ApplicationProgress.Application)))
        {
            RefreshAppearance();
            return;
        }

        if (e.Object is Application)
            RefreshAppearance();
    }

    private void RefreshAppearance() =>
        Frame.GetController<AppearanceController>()?.Refresh();
}
