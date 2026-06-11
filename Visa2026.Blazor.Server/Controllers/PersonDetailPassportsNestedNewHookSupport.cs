using DevExpress.ExpressApp.Blazor;

using Microsoft.JSInterop;



namespace Visa2026.Blazor.Server.Controllers;



/// <summary>

/// Shared ensure / stop for nested Passports ListView <c>New</c> hooks on Person detail

/// (same re-nav pattern as Person ListView <c>person-list-*-new</c>).

/// </summary>

internal static class PersonDetailPassportsNestedNewHookSupport

{

    private static readonly int[] DelayedEnsureMs = [100, 300, 600, 1200, 2000, 3500, 5000, 8000];

    private static CancellationTokenSource? _ensureCts;

    private static readonly SemaphoreSlim EnsureGate = new(1, 1);



    internal static void ScheduleEnsure(BlazorApplication? blazorApplication, bool includeDelayedPasses = true)

    {

        IJSRuntime? jsRuntime = ResolveJsRuntime(blazorApplication);

        if (jsRuntime == null)

        {

            return;

        }



        _ensureCts?.Cancel();

        _ensureCts?.Dispose();

        _ensureCts = new CancellationTokenSource();

        CancellationToken cancellationToken = _ensureCts.Token;

        _ = RunEnsureAsync(jsRuntime, cancellationToken, includeDelayedPasses);

    }



    internal static async Task StopWatchAsync(BlazorApplication? blazorApplication)

    {

        _ensureCts?.Cancel();

        _ensureCts?.Dispose();

        _ensureCts = null;



        IJSRuntime? jsRuntime = ResolveJsRuntime(blazorApplication);

        if (jsRuntime == null)

        {

            return;

        }



        try

        {

            await jsRuntime.InvokeVoidAsync("visa2026E2eHooks.stopPersonDetailNestedPassportsToolbarWatch");

        }

        catch (JSDisconnectedException)

        {

            // Circuit torn down during navigation — safe to ignore.

        }

    }



    private static async Task RunEnsureAsync(

        IJSRuntime jsRuntime,

        CancellationToken cancellationToken,

        bool includeDelayedPasses)

    {

        await EnsureGate.WaitAsync(cancellationToken);

        try

        {

            await EnsureAsync(jsRuntime, cancellationToken);

            if (includeDelayedPasses && !cancellationToken.IsCancellationRequested)

            {

                await EnsureDelayedPassesAsync(jsRuntime, cancellationToken);

            }

        }

        catch (OperationCanceledException)

        {

            // Superseded by a newer ensure — expected.

        }

        finally

        {

            EnsureGate.Release();

        }

    }



    private static async Task EnsureAsync(IJSRuntime jsRuntime, CancellationToken cancellationToken)

    {

        for (int attempt = 0; attempt < 40; attempt++)

        {

            cancellationToken.ThrowIfCancellationRequested();

            bool applied = await jsRuntime.InvokeAsync<bool>(

                "visa2026E2eHooks.ensurePersonDetailPassportsListNewActionTestId",

                cancellationToken,

                PassportListViewE2eActionHooks.PersonDetailPassportsNewTestId);

            if (applied)

            {

                return;

            }



            await Task.Delay(250, cancellationToken);

        }

    }



    private static async Task EnsureDelayedPassesAsync(IJSRuntime jsRuntime, CancellationToken cancellationToken)

    {

        foreach (int delayMs in DelayedEnsureMs)

        {

            await Task.Delay(delayMs, cancellationToken);

            await jsRuntime.InvokeAsync<bool>(

                "visa2026E2eHooks.applyPersonDetailPassportsListNewActionTestId",

                cancellationToken,

                PassportListViewE2eActionHooks.PersonDetailPassportsNewTestId);

        }

    }



    private static IJSRuntime? ResolveJsRuntime(BlazorApplication? blazorApplication) =>

        blazorApplication?.ServiceProvider?.GetService<IJSRuntime>();

}

