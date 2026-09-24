using DevExpress.Blazor;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Editors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Applications via ministry: show Year, Month, Urgency, and Visa type only when
/// they fit in the list without a horizontal scrollbar. Progress date stays hidden.
/// </summary>
public sealed class ApplicationViaMinistriesOptionalColumnsController : ViewController<ListView>
{
    private static readonly (string Field, string Width, int MinWidth)[] OptionalColumns =
    [
        (nameof(ApplicationProfileInstance.Year), "4.5rem", 64),
        (nameof(ApplicationProfileInstance.MonthName), "6.5rem", 88),
        (nameof(ApplicationProfileInstance.Urgency), "7rem", 96),
        (nameof(ApplicationProfileInstance.VisaType), "7rem", 110),
    ];

    private DotNetObjectReference<ApplicationViaMinistriesOptionalColumnsController>? dotNetRef;
    private CancellationTokenSource? applyCts;
    private int applyGate;
    private int pendingRerun;

    public ApplicationViaMinistriesOptionalColumnsController()
    {
        TargetViewId = ApplicationProfileInstanceProgressRouteNavigation.ListViewViaMinistries;
    }

    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        SetOptionalColumnsVisible(false);
        applyCts?.Cancel();
        applyCts?.Dispose();
        applyCts = new CancellationTokenSource();
        _ = StartAsync(applyCts.Token);
    }

    protected override void OnDeactivated()
    {
        applyCts?.Cancel();
        applyCts?.Dispose();
        applyCts = null;
        _ = ReleaseJsAsync();
        dotNetRef?.Dispose();
        dotNetRef = null;
        base.OnDeactivated();
    }

    [JSInvokable]
    public Task OnListWidthChanged()
    {
        if (View is not { IsDisposed: false })
            return Task.CompletedTask;
        return ApplyAsync();
    }

    private async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(400, cancellationToken);
            await ApplyAsync();
            if (cancellationToken.IsCancellationRequested || View is not { IsDisposed: false })
                return;

            var js = GetJs();
            if (js == null)
                return;

            dotNetRef ??= DotNetObjectReference.Create(this);
            await js.InvokeVoidAsync("visaListOptionalColumns.observe", cancellationToken, dotNetRef);
        }
        catch (OperationCanceledException)
        {
        }
        catch (JSDisconnectedException)
        {
        }
        catch (JSException)
        {
        }
        catch (InvalidOperationException)
        {
        }
    }

    private async Task ApplyAsync()
    {
        if (Interlocked.CompareExchange(ref applyGate, 1, 0) != 0)
        {
            Interlocked.Exchange(ref pendingRerun, 1);
            return;
        }

        try
        {
            do
            {
                Interlocked.Exchange(ref pendingRerun, 0);
                await ApplyOnceAsync();
            }
            while (Interlocked.CompareExchange(ref pendingRerun, 0, 1) == 1 && View is { IsDisposed: false });
        }
        finally
        {
            Interlocked.Exchange(ref applyGate, 0);
            if (Interlocked.CompareExchange(ref pendingRerun, 0, 1) == 1 && View is { IsDisposed: false })
                _ = ApplyAsync();
        }
    }

    private async Task ApplyOnceAsync()
    {
        if (View is not { IsDisposed: false })
            return;

        SetOptionalColumnsVisible(false);

        var js = GetJs();
        if (js == null)
            return;

        try
        {
            await js.InvokeVoidAsync("visaListOptionalColumns.setNavCollapsed", true);
            await Task.Delay(350);
            if (View is not { IsDisposed: false })
                return;

            var overflows = await js.InvokeAsync<bool>("visaListOptionalColumns.overflows");

            if (!overflows)
            {
                SetOptionalColumnsVisible(true);
                await Task.Delay(300);
                if (View is not { IsDisposed: false })
                    return;
                overflows = await js.InvokeAsync<bool>("visaListOptionalColumns.overflows");
                if (overflows)
                    SetOptionalColumnsVisible(false);
            }

            if (overflows)
                await js.InvokeVoidAsync("visaListOptionalColumns.resetScroll");
        }
        catch (OperationCanceledException)
        {
        }
        catch (JSDisconnectedException)
        {
        }
        catch (JSException)
        {
            SetOptionalColumnsVisible(false);
        }
        catch (InvalidOperationException)
        {
            SetOptionalColumnsVisible(false);
        }
    }

    private void SetOptionalColumnsVisible(bool visible)
    {
        if (View?.Editor is not DxGridListEditor editor)
            return;

        foreach (var column in editor.GridDataColumnModels)
        {
            foreach (var optional in OptionalColumns)
            {
                if (!string.Equals(column.FieldName, optional.Field, StringComparison.Ordinal))
                    continue;

                column.Width = optional.Width;
                column.MinWidth = optional.MinWidth;
                column.Visible = visible;
                break;
            }
        }

        if (editor.GridModel.ComponentInstance is not IGrid grid)
            return;

        foreach (var column in grid.GetDataColumns())
        {
            if (OptionalColumns.Any(optional =>
                    string.Equals(column.FieldName, optional.Field, StringComparison.Ordinal)))
            {
                column.Visible = visible;
            }
        }
    }

    private IJSRuntime? GetJs() => Application?.ServiceProvider?.GetService<IJSRuntime>();

    private async Task ReleaseJsAsync()
    {
        var js = GetJs();
        if (js == null)
            return;

        try
        {
            await js.InvokeVoidAsync("visaListOptionalColumns.release");
        }
        catch (JSDisconnectedException)
        {
        }
        catch (JSException)
        {
        }
        catch (InvalidOperationException)
        {
        }
    }
}