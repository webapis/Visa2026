using DevExpress.ExpressApp;
using DevExpress.ExpressApp.SystemModule;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationProfilePicker;
using Visa2026.Module.Services.MigrationImport;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Replaces standard New on ApplicationProfileInstance ListViews with the profile picker (slice 9).
/// </summary>
public sealed class ApplicationProfilePickerNewController : ViewController<ListView>
{
    public ApplicationProfilePickerNewController()
    {
        TargetObjectType = typeof(ApplicationProfileInstance);
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        var newObjectController = Frame.GetController<NewObjectViewController>();
        if (newObjectController?.NewObjectAction != null)
            newObjectController.NewObjectAction.Executing += NewObjectAction_Executing;
    }

    protected override void OnDeactivated()
    {
        var newObjectController = Frame.GetController<NewObjectViewController>();
        if (newObjectController?.NewObjectAction != null)
            newObjectController.NewObjectAction.Executing -= NewObjectAction_Executing;
        base.OnDeactivated();
    }

    private void NewObjectAction_Executing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (MigrationImportContext.IsDataImport)
            return;

        e.Cancel = true;

        var context = new ApplicationProfilePickerOpenContext
        {
            SourceListViewId = View.Id,
            CreationProgressRoute = ApplicationProfilePickerOpenHelper.ResolveRouteFromListView(View.Id),
        };

        var pickerView = ApplicationProfilePickerOpenHelper.CreatePickerView(Application, context, Frame);
        if (pickerView == null)
            return;

        Application.ShowViewStrategy.ShowView(
            new ShowViewParameters(pickerView) { TargetWindow = TargetWindow.Current },
            new ShowViewSource(Frame, null));
    }
}
