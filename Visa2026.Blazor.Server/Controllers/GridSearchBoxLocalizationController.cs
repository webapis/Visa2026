using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Editors;
using Microsoft.AspNetCore.Components;
using Visa2026.Blazor.Server.Localization;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Localizes DevExpress Blazor grid chrome (search placeholder, empty-state text)
/// not driven by XAF Application Model captions.
/// </summary>
public sealed class GridSearchBoxLocalizationController : ViewController<ListView>
{
    private CancellationTokenSource? deferredLocalizationCts;

    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        ApplyGridLocalization();
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
            ApplyGridLocalization();
        }
    }

    private void ApplyGridLocalization()
    {
        if (View?.Editor is not DxGridListEditor { GridModel: { } gridModel })
        {
            return;
        }

        string searchPlaceholder = VisaLocalization.GetGridSearchBoxNullText();
        gridModel.SearchBoxNullText = searchPlaceholder;
        string emptyText = VisaLocalization.GetGridEmptyDataText();
        gridModel.EmptyDataAreaTemplate = _ => builder =>
        {
            builder.OpenElement(0, "span");
            builder.AddContent(1, emptyText);
            builder.CloseElement();
        };
    }
}
