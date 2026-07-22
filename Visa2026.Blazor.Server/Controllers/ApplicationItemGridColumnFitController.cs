using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.SystemModule;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Nested Application Items list must show all rows (no inner scrollbar) over global virtual scrolling.
/// Column auto-fit for all ListViews (including Application Item) is handled by
/// <see cref="ListViewGridColumnFitController"/>.
/// </summary>
public sealed class ApplicationItemGridColumnFitController : ViewController<ListView>
{
    private const string NestedApplicationItemsListViewId = "Application_ApplicationItems_ListView";

    public ApplicationItemGridColumnFitController()
    {
        TargetObjectType = typeof(ApplicationItem);
        TargetViewId = NestedApplicationItemsListViewId;
    }

    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        if (View.Model is IModelListViewBlazor blazorModel)
        {
            blazorModel.ShowAllRows = true;
            blazorModel.VirtualScrollingEnabled = false;
        }
    }
}