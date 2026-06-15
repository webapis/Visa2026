using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Blazor.Layout;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Always opens <see cref="Application"/> detail on the Application tab (not the last-selected Progress tab).
/// </summary>
public sealed class ApplicationDetailViewDefaultTabController : ObjectViewController<DetailView, Application>
{
    private const string MainTabbedGroupId = "Item1";

    private DxFormLayoutTabPagesModel? _mainTabbedGroup;

    public ApplicationDetailViewDefaultTabController()
    {
        TargetViewId = "Application_DetailView";
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        if (View.LayoutManager is BlazorLayoutManager layoutManager)
            layoutManager.ItemCreated += OnLayoutItemCreated;

        View.CurrentObjectChanged += OnCurrentObjectChanged;
        ResetMainTab();
    }

    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        ResetMainTab();
    }

    protected override void OnDeactivated()
    {
        View.CurrentObjectChanged -= OnCurrentObjectChanged;
        if (View.LayoutManager is BlazorLayoutManager layoutManager)
            layoutManager.ItemCreated -= OnLayoutItemCreated;

        _mainTabbedGroup = null;
        base.OnDeactivated();
    }

    private void OnCurrentObjectChanged(object? sender, EventArgs e) => ResetMainTab();

    private void OnLayoutItemCreated(object? sender, BlazorLayoutManager.ItemCreatedEventArgs e)
    {
        if (e.ModelLayoutElement.Id != MainTabbedGroupId
            || e.LayoutControlItem is not DxFormLayoutTabPagesModel tabbedGroup)
        {
            return;
        }

        _mainTabbedGroup = tabbedGroup;
        tabbedGroup.ActiveTabIndex = 0;
    }

    private void ResetMainTab()
    {
        if (_mainTabbedGroup != null)
            _mainTabbedGroup.ActiveTabIndex = 0;
    }
}
