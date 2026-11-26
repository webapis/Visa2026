using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.SystemModule;
using Visa2026.Module.BusinessObjects.EmployeeSearch;

namespace Visa2026.Module.Controllers;

public sealed class EmployeeSearchNavigationController : WindowController
{
    private ShowNavigationItemController _navigationController;

    public EmployeeSearchNavigationController()
    {
        TargetWindowType = WindowType.Main;
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        _navigationController = Frame.GetController<ShowNavigationItemController>();
        if (_navigationController != null)
            _navigationController.CustomShowNavigationItem += OnCustomShowNavigationItem;
    }

    protected override void OnDeactivated()
    {
        if (_navigationController != null)
            _navigationController.CustomShowNavigationItem -= OnCustomShowNavigationItem;
        _navigationController = null;
        base.OnDeactivated();
    }

    private void OnCustomShowNavigationItem(object sender, CustomShowNavigationItemEventArgs e)
    {
        if (!IsFindEmployeesItem(e.ActionArguments.SelectedChoiceActionItem))
            return;

        if (Application.MainWindow?.View is DetailView open
            && open.ObjectTypeInfo?.Type == typeof(EmployeeSearchHost))
        {
            e.Handled = true;
            return;
        }

        var objectSpace = Application.CreateObjectSpace(typeof(EmployeeSearchHost));
        var host = objectSpace.CreateObject<EmployeeSearchHost>();
        e.ActionArguments.ShowViewParameters.CreatedView = Application.CreateDetailView(objectSpace, host);
        e.ActionArguments.ShowViewParameters.TargetWindow = TargetWindow.Current;
        e.Handled = true;
    }

    private static bool IsFindEmployeesItem(ChoiceActionItem item)
    {
        if (item == null)
            return false;

        if (string.Equals(item.Id, EmployeeSearchViewIds.NavItemId, StringComparison.Ordinal))
            return true;

        return item.Data is IModelNavigationItem model
            && string.Equals(model.Id, EmployeeSearchViewIds.NavItemId, StringComparison.Ordinal);
    }
}

public sealed class EmployeeSearchChromeController : ViewController
{
    private const string Reason = "EmployeeSearch";

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
        if (View is not DetailView detailView || detailView.Id != EmployeeSearchViewIds.DetailView)
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
