using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.SystemModule;
using Visa2026.Module.BusinessObjects.EmployeeSearch;
using Visa2026.Module.Localization;

namespace Visa2026.Module.Controllers;

public sealed class EmployeeSearchNavigationController : WindowController
{
    private ShowNavigationItemController _navigationController;
    private bool _localizing;

    public EmployeeSearchNavigationController()
    {
        TargetWindowType = WindowType.Main;
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        _navigationController = Frame.GetController<ShowNavigationItemController>();
        if (_navigationController == null)
            return;

        _navigationController.CustomShowNavigationItem += OnCustomShowNavigationItem;
        _navigationController.ItemsInitialized += OnItemsInitialized;
        if (_navigationController.ShowNavigationItemAction != null)
            _navigationController.ShowNavigationItemAction.ItemsChanged += OnItemsChanged;

        Localize(_navigationController.ShowNavigationItemAction?.Items);
    }

    protected override void OnDeactivated()
    {
        if (_navigationController != null)
        {
            _navigationController.CustomShowNavigationItem -= OnCustomShowNavigationItem;
            _navigationController.ItemsInitialized -= OnItemsInitialized;
            if (_navigationController.ShowNavigationItemAction != null)
                _navigationController.ShowNavigationItemAction.ItemsChanged -= OnItemsChanged;
        }

        _navigationController = null;
        base.OnDeactivated();
    }

    private void OnItemsInitialized(object sender, EventArgs e) =>
        Localize((sender as ShowNavigationItemController)?.ShowNavigationItemAction?.Items);

    private void OnItemsChanged(object sender, EventArgs e)
    {
        if (_localizing)
            return;

        Localize((sender as ChoiceActionBase)?.Items);
    }

    private void Localize(ChoiceActionItemCollection items)
    {
        var item = Find(items);
        if (item == null || _localizing)
            return;

        _localizing = true;
        try
        {
            item.Caption = EmployeeSearchText.Get("Title");
        }
        finally
        {
            _localizing = false;
        }
    }

    private static ChoiceActionItem Find(ChoiceActionItemCollection items)
    {
        if (items == null)
            return null;

        foreach (ChoiceActionItem item in items)
        {
            if (IsFindEmployeesItem(item))
                return item;

            var nested = Find(item.Items);
            if (nested != null)
                return nested;
        }

        return null;
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

        detailView.Caption = EmployeeSearchText.Get("Title");
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
