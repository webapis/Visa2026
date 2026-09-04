using DevExpress.Blazor;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Editors;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Visa2026.Blazor.Server.Localization;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Localizes DevExpress Blazor grid chrome (search placeholder, empty-state text)
/// not driven by XAF ApplicationProfileInstance Model captions, enables the filter panel when
/// column filters are active, and offers Clear filters in the empty state.
/// </summary>
public sealed class GridSearchBoxLocalizationController : ViewController<ListView>
{
    private CancellationTokenSource? deferredLocalizationCts;

    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        ApplyGridChrome();
        ScheduleDeferredLocalization();
    }

    protected override void OnDeactivated()
    {
        deferredLocalizationCts?.Cancel();
        deferredLocalizationCts?.Dispose();
        deferredLocalizationCts = null;
        base.OnDeactivated();
    }

    private void ScheduleDeferredLocalization()
    {
        deferredLocalizationCts?.Cancel();
        deferredLocalizationCts?.Dispose();
        deferredLocalizationCts = new CancellationTokenSource();
        CancellationToken token = deferredLocalizationCts.Token;
        _ = ApplyGridLocalizationDeferredAsync(token);
    }

    private async Task ApplyGridLocalizationDeferredAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(150, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (View is { IsDisposed: false })
        {
            ApplyGridChrome();
        }
    }

    private void ApplyGridChrome()
    {
        if (View?.Editor is not DxGridListEditor { GridModel: { } gridModel })
        {
            return;
        }

        // Show criteria + Clear when any column filter is active (hidden otherwise).
        gridModel.FilterPanelDisplayMode = GridFilterPanelDisplayMode.Auto;

        gridModel.SearchBoxNullText = VisaLocalization.GetGridSearchBoxNullText();
        gridModel.EmptyDataAreaTemplate = _ => builder => RenderEmptyDataArea(builder, () => gridModel.ComponentInstance);
    }

    private void RenderEmptyDataArea(RenderTreeBuilder builder, Func<IGrid?> getGrid)
    {
        IGrid? grid = getGrid();
        bool filtered = ListViewGridFilterState.HasActiveFilter(grid);

        if (!filtered)
        {
            builder.OpenElement(0, "span");
            builder.AddContent(1, VisaLocalization.GetGridEmptyDataText());
            builder.CloseElement();
            return;
        }

        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "visa-grid-empty-filtered");

        builder.OpenElement(2, "div");
        builder.AddAttribute(3, "class", "visa-grid-empty-filtered__message");
        builder.AddContent(4, VisaLocalization.GetGridEmptyFilteredDataText());
        builder.CloseElement();

        builder.OpenElement(5, "button");
        builder.AddAttribute(6, "type", "button");
        builder.AddAttribute(7, "class", "visa-grid-empty-filtered__clear");
        builder.AddAttribute(8, "onclick", EventCallback.Factory.Create(this, () => ListViewGridFilterState.Clear(getGrid())));
        builder.AddContent(9, VisaLocalization.GetGridClearFiltersText());
        builder.CloseElement();

        builder.CloseElement();
    }
}