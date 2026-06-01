using System;
using System.ComponentModel;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Base;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Validates <see cref="ApplicationProgress"/> rows on commit (including nested saves from <see cref="Application"/>).
/// </summary>
public sealed class ApplicationProgressCommitValidationController : ViewController
{
    protected override void OnActivated()
    {
        base.OnActivated();
        ObjectSpace.Committing += ObjectSpace_Committing;
    }

    protected override void OnDeactivated()
    {
        ObjectSpace.Committing -= ObjectSpace_Committing;
        base.OnDeactivated();
    }

    private void ObjectSpace_Committing(object sender, CancelEventArgs e)
    {
        foreach (var progress in ObjectSpace.GetObjectsToSave(false).OfType<ApplicationProgress>())
        {
            if (ApplicationProgressTransitionHelper.TryValidateProgressStep(progress, ObjectSpace, out var errorMessage))
                continue;

            e.Cancel = true;
            Application.ShowViewStrategy.ShowMessage(
                errorMessage ?? VisaUiMessages.Get("ApplicationProgress.InvalidForRoute"),
                InformationType.Error,
                5000,
                InformationPosition.Top);
            return;
        }
    }
}

/// <summary>
/// Suggests state/location defaults and refreshes location choices on the <see cref="ApplicationProgress"/> detail view.
/// </summary>
public sealed class ApplicationProgressDetailViewController : ObjectViewController<DetailView, ApplicationProgress>
{
    protected override void OnActivated()
    {
        base.OnActivated();
        ObjectSpace.ObjectChanged += ObjectSpace_ObjectChanged;
    }

    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        ApplyDefaults(ViewCurrentObject);
    }

    protected override void OnDeactivated()
    {
        ObjectSpace.ObjectChanged -= ObjectSpace_ObjectChanged;
        base.OnDeactivated();
    }

    private void ObjectSpace_ObjectChanged(object sender, ObjectChangedEventArgs e)
    {
        if (e.Object is not ApplicationProgress progress || !ReferenceEquals(progress, ViewCurrentObject))
            return;

        if (e.PropertyName is nameof(ApplicationProgress.State) or nameof(ApplicationProgress.Application))
            ApplyDefaults(progress);
    }

    private void ApplyDefaults(ApplicationProgress? progress)
    {
        if (progress == null)
            return;

        ApplicationProgressTransitionHelper.TryApplySuggestedNextStep(progress);
        ApplicationProgressTransitionHelper.TryApplyDefaultLocationForState(progress);
        View.Refresh();
    }
}
