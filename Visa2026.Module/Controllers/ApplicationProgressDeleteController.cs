using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.SystemModule;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;

namespace Visa2026.Module.Controllers;

/// <summary>Only the tail <see cref="ApplicationProgress"/> row on an application may be deleted.</summary>
public sealed class ApplicationProgressDeleteController : ObjectViewController<ObjectView, ApplicationProgress>
{
    private DeleteObjectsViewController? _deleteController;

    protected override void OnActivated()
    {
        base.OnActivated();
        ObjectSpace.ObjectDeleting += ObjectSpace_ObjectDeleting;
        _deleteController = Frame.GetController<DeleteObjectsViewController>();
        View.CurrentObjectChanged += View_SelectionChanged;
        if (View is ListView listView)
            listView.SelectionChanged += View_SelectionChanged;
        UpdateDeleteActionState();
    }

    protected override void OnDeactivated()
    {
        ObjectSpace.ObjectDeleting -= ObjectSpace_ObjectDeleting;
        View.CurrentObjectChanged -= View_SelectionChanged;
        if (View is ListView listView)
            listView.SelectionChanged -= View_SelectionChanged;
        _deleteController?.DeleteAction.Enabled.RemoveItem(nameof(ApplicationProgressDeleteController));
        base.OnDeactivated();
    }

    private void View_SelectionChanged(object? sender, System.EventArgs e) => UpdateDeleteActionState();

    private void UpdateDeleteActionState()
    {
        _deleteController?.DeleteAction.Enabled.SetItemValue(
            nameof(ApplicationProgressDeleteController),
            CanDeleteCurrentSelection());
    }

    private bool CanDeleteCurrentSelection()
    {
        var selected = View.SelectedObjects.Cast<ApplicationProgress>().ToList();
        if (selected.Count != 1)
            return false;

        return ApplicationProgressOrderHelper.IsLastTimelineStep(selected[0], ObjectSpace);
    }

    private void ObjectSpace_ObjectDeleting(object sender, ObjectsManipulatingEventArgs e)
    {
        foreach (var progress in e.Objects.OfType<ApplicationProgress>())
        {
            if (ApplicationProgressOrderHelper.IsLastTimelineStep(progress, ObjectSpace))
                continue;

            throw new UserFriendlyException(VisaUiMessages.Get("ApplicationProgress.OnlyLastStepDeletable"));
        }
    }
}