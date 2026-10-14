using DevExpress.Blazor;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Blazor.Components;
using DevExpress.ExpressApp.Blazor.Templates.Navigation;
using DevExpress.ExpressApp.Blazor.Templates.Navigation.ActionControls;
using DevExpress.ExpressApp.SystemModule;
using Microsoft.AspNetCore.Components;
using Visa2026.Module.Services.ReportDashboard;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Keeps the Report Dashboard folder expanded after login, and opens Overview when that folder is clicked.
/// Other navigation groups keep the accordion's normal expand-on-click behavior.
/// </summary>
public sealed class ReportDashboardNavigationExpandController : WindowController
{
    private EventCallback<AccordionItemClickEventArgs> _rootClick;
    private bool _openingOverview;

    public ReportDashboardNavigationExpandController()
    {
        TargetWindowType = WindowType.Main;
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        var action = Frame.GetController<ShowNavigationItemController>()?.ShowNavigationItemAction;
        if (action != null)
            action.CustomizeControl += OnCustomizeControl;
    }

    protected override void OnDeactivated()
    {
        var action = Frame.GetController<ShowNavigationItemController>()?.ShowNavigationItemAction;
        if (action != null)
            action.CustomizeControl -= OnCustomizeControl;
        _rootClick = default;
        base.OnDeactivated();
    }

    private void OnCustomizeControl(object? sender, CustomizeControlEventArgs e)
    {
        if ((e.Control as ShowNavigationItemActionControl)?.NavigationComponentAdapter is not DxAccordionAdapter accordion)
            return;

        HookRootClick(accordion);
        accordion.ComponentCaptured -= OnAccordionCaptured;
        accordion.ComponentCaptured += OnAccordionCaptured;
    }

    private void HookRootClick(DxAccordionAdapter accordion)
    {
        var model = accordion.ComponentModel;
        if (model == null)
            return;

        if (_rootClick.HasDelegate && model.ItemClick.Equals(_rootClick))
            return;

        var previous = model.ItemClick;
        _rootClick = EventCallback.Factory.Create<AccordionItemClickEventArgs>(
            this,
            (AccordionItemClickEventArgs args) => OnAccordionItemClick(args, previous));
        model.ItemClick = _rootClick;
    }

    private async Task OnAccordionItemClick(AccordionItemClickEventArgs args, EventCallback<AccordionItemClickEventArgs> previous)
    {
        // The folder click opens Overview. Leaf items stay on XAF's own handler.
        // Skipping the previous handler for the folder avoids opening a second dashboard tab.
        if (IsReportDashboardGroup(args.ItemInfo))
        {
            ExecuteOverview();
            return;
        }

        if (previous.HasDelegate)
            await previous.InvokeAsync(args);
    }

    private void ExecuteOverview()
    {
        if (_openingOverview)
            return;

        var showNav = Frame.GetController<ShowNavigationItemController>();
        var root = showNav?.ShowNavigationItemAction.Items.Find(ReportDashboardNavigationItems.RootId);
        if (showNav == null || root == null)
            return;

        _openingOverview = true;
        try
        {
            showNav.ShowNavigationItemAction.DoExecute(root);
        }
        finally
        {
            _openingOverview = false;
        }
    }

    private void OnAccordionCaptured(object? sender, ComponentCapturedEventArgs<DxAccordion> e)
    {
        if (sender is DxAccordionAdapter accordion)
            HookRootClick(accordion);

        e.Component.SetItemExpanded(IsReportDashboardGroup, true);
    }

    private static bool IsReportDashboardGroup(IAccordionItemInfo info) =>
        string.Equals(info.Name, ReportDashboardNavigationItems.RootId, StringComparison.Ordinal);
}
