using DevExpress.Blazor;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Editors;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Applies work-permit-life row background on the Work Permits registry ListView (matches color-tab swatches).
/// </summary>
public sealed class WorkPermitItemRegistryRowAppearanceController : ViewController<ListView>
{
    private Action<GridCustomizeElementEventArgs>? customizeElementHandler;
    private Action<GridCustomizeElementEventArgs>? previousCustomizeElement;
    private CancellationTokenSource? deferredApplyCts;

    public WorkPermitItemRegistryRowAppearanceController()
    {
        TargetObjectType = typeof(WorkPermitItem);
        TargetViewId = WorkPermitItemRegistryNavigation.TargetViewIds;
    }

    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        ApplyRowAppearance();
        // Re-wrap after other Çakylyklar CustomizeElement handlers (links) so the row class stays last.
        ScheduleDeferredApply();
    }

    private void ScheduleDeferredApply()
    {
        deferredApplyCts?.Cancel();
        deferredApplyCts?.Dispose();
        deferredApplyCts = new CancellationTokenSource();
        var token = deferredApplyCts.Token;
        _ = ApplyDeferredAsync(token);
    }

    private async Task ApplyDeferredAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(200, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (View is { IsDisposed: false })
            ApplyRowAppearance();
    }

    private void ApplyRowAppearance()
    {
        if (View?.Editor is not DxGridListEditor { GridModel: { } gridModel })
            return;

        if (customizeElementHandler != null)
        {
            gridModel.CustomizeElement = previousCustomizeElement;
            customizeElementHandler = null;
            previousCustomizeElement = null;
        }

        previousCustomizeElement = gridModel.CustomizeElement;
        customizeElementHandler = e =>
        {
            // Keep cell-link handlers; still paint DataRow after they run.
            previousCustomizeElement?.Invoke(e);
            if (e.ElementType != GridElementType.DataRow || e.VisibleIndex < 0)
                return;

            if (e.Grid.GetDataItem(e.VisibleIndex) is not WorkPermitItem item)
                return;

            var rowCssClass = item.ListRowCssClass;
            if (string.IsNullOrEmpty(rowCssClass))
                return;

            e.CssClass = string.IsNullOrEmpty(e.CssClass)
                ? rowCssClass
                : $"{e.CssClass} {rowCssClass}";
        };
        gridModel.CustomizeElement = customizeElementHandler;
    }

    protected override void OnDeactivated()
    {
        deferredApplyCts?.Cancel();
        deferredApplyCts?.Dispose();
        deferredApplyCts = null;

        if (View?.Editor is DxGridListEditor { GridModel: { } gridModel }
            && customizeElementHandler != null)
        {
            gridModel.CustomizeElement = previousCustomizeElement;
        }

        customizeElementHandler = null;
        previousCustomizeElement = null;
        base.OnDeactivated();
    }
}