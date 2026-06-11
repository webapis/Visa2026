using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.SystemModule;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Stable <c>person-employee-tab-passports-new</c> on nested <c>Passport_ListView</c> toolbar
/// <c>New</c>. Primary path: <see cref="ActionBase.CustomizeControl"/> on
/// <see cref="NewObjectViewController.NewObjectAction"/> (framework toolbar model — same as Logon).
/// JS fallback via <see cref="PersonDetailPassportsNestedNewHookSupport"/> when Blazor re-renders toolbar DOM.
/// </summary>
public sealed class PassportListViewE2eActionSelectorsController : ViewController<ListView>
{
    private NewObjectViewController? _newObjectViewController;
    private object? _newActionControl;

    public PassportListViewE2eActionSelectorsController()
    {
        TargetViewId = PassportListViewE2eActionHooks.ListViewId;
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        if (!IsNestedOnPersonDetail())
        {
            return;
        }

        HookNewObjectAction();
        ApplyNestedNewHook();
        ScheduleJsFallback();
    }

    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        if (!IsNestedOnPersonDetail())
        {
            return;
        }

        ApplyNestedNewHook();
        E2eActionControlSelectorSupport.TryRaiseCustomizeControl(_newObjectViewController?.NewObjectAction);
        ScheduleJsFallback();
    }

    protected override void OnDeactivated()
    {
        UnhookNewObjectAction();
        _newActionControl = null;
        base.OnDeactivated();
    }

    private void HookNewObjectAction()
    {
        _newObjectViewController = Frame.GetController<NewObjectViewController>();
        if (_newObjectViewController?.NewObjectAction == null)
        {
            return;
        }

        _newObjectViewController.NewObjectAction.CustomizeControl -= NewObjectAction_CustomizeControl;
        _newObjectViewController.NewObjectAction.CustomizeControl += NewObjectAction_CustomizeControl;
        Frame.ViewChanged -= Frame_ViewChanged;
        Frame.ViewChanged += Frame_ViewChanged;
        E2eActionControlSelectorSupport.TryRaiseCustomizeControl(_newObjectViewController.NewObjectAction);
    }

    private void UnhookNewObjectAction()
    {
        Frame.ViewChanged -= Frame_ViewChanged;
        if (_newObjectViewController?.NewObjectAction != null)
        {
            _newObjectViewController.NewObjectAction.CustomizeControl -= NewObjectAction_CustomizeControl;
        }

        _newObjectViewController = null;
    }

    private void NewObjectAction_CustomizeControl(object? sender, CustomizeControlEventArgs e)
    {
        _newActionControl = e.Control;
        ApplyNestedNewHook();
    }

    private void Frame_ViewChanged(object? sender, ViewChangedEventArgs e) => ApplyNestedNewHook();

    private void ApplyNestedNewHook()
    {
        if (_newActionControl == null)
        {
            return;
        }

        E2eActionControlSelectorSupport.TryApplyFromActionControl(
            _newActionControl,
            PassportListViewE2eActionHooks.PersonDetailPassportsNewTestId,
            _newObjectViewController?.NewObjectAction);
    }

    private bool IsNestedOnPersonDetail()
    {
        if (View?.CollectionSource is not PropertyCollectionSource propertySource)
        {
            return false;
        }

        return propertySource.MasterObject is Person;
    }

    private void ScheduleJsFallback(bool includeDelayedPasses = true)
    {
        if (Application is BlazorApplication blazorApplication)
        {
            PersonDetailPassportsNestedNewHookSupport.ScheduleEnsure(blazorApplication, includeDelayedPasses);
        }
    }
}
