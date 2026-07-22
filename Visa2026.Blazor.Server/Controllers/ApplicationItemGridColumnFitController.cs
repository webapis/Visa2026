using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.SystemModule;
using DevExpress.ExpressApp.Model;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Nested Application Items list must show all rows (no inner scrollbar) over global virtual scrolling.
/// Also applies <see cref="ApplicationType.ShowCurrentVisa"/> to the <c>CurrentVisa</c> column from the
/// parent <see cref="Application"/> (Appearance ListView rules cannot resolve nested ApplicationType).
/// Column auto-fit for all ListViews (including Application Item) is handled by
/// <see cref="ListViewGridColumnFitController"/>.
/// </summary>
public sealed class ApplicationItemGridColumnFitController : ViewController<ListView>
{
    private const string NestedApplicationItemsListViewId = "Application_ApplicationItems_ListView";
    private const int CurrentVisaColumnIndex = 7;

    private PropertyCollectionSource? propertyCollectionSource;

    public ApplicationItemGridColumnFitController()
    {
        TargetObjectType = typeof(ApplicationItem);
        TargetViewId = NestedApplicationItemsListViewId;
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        propertyCollectionSource = View.CollectionSource as PropertyCollectionSource;
        if (propertyCollectionSource != null)
            propertyCollectionSource.MasterObjectChanged += OnMasterObjectChanged;

        ApplyCurrentVisaColumnVisibility();
    }

    protected override void OnDeactivated()
    {
        if (propertyCollectionSource != null)
        {
            propertyCollectionSource.MasterObjectChanged -= OnMasterObjectChanged;
            propertyCollectionSource = null;
        }

        base.OnDeactivated();
    }

    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        if (View.Model is IModelListViewBlazor blazorModel)
        {
            blazorModel.ShowAllRows = true;
            blazorModel.VirtualScrollingEnabled = false;
        }

        ApplyCurrentVisaColumnVisibility();
    }

    private void OnMasterObjectChanged(object? sender, EventArgs e) =>
        ApplyCurrentVisaColumnVisibility();

    private void ApplyCurrentVisaColumnVisibility()
    {
        if (View?.Model?.Columns["CurrentVisa"] is not IModelColumn column)
            return;

        var showCurrentVisa =
            propertyCollectionSource?.MasterObject is Application { ApplicationType: { } appType }
            && appType.ShowCurrentVisa;

        column.Index = showCurrentVisa ? CurrentVisaColumnIndex : -1;
    }
}
