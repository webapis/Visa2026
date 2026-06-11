using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor;
using Microsoft.JSInterop;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Applies stable <c>data-testid</c> / <c>.e2e-*</c> on <c>Passport_DetailView</c> toolbar Save.
/// </summary>
public sealed class PassportDetailViewE2eActionSelectorsController : ViewController<DetailView>
{
    public PassportDetailViewE2eActionSelectorsController()
    {
        TargetObjectType = typeof(Passport);
        TargetViewId = PassportDetailViewE2eActionHooks.DetailViewId;
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        ScheduleEnsureToolbarActionHooks();
    }

    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        ScheduleEnsureToolbarActionHooks();
    }

    protected override void OnDeactivated()
    {
        _ = StopToolbarActionWatchAsync();
        base.OnDeactivated();
    }

    private void ScheduleEnsureToolbarActionHooks()
    {
        string? detailViewId = View?.Id;
        if (string.IsNullOrEmpty(detailViewId))
        {
            return;
        }

        foreach ((string actionId, IReadOnlyDictionary<string, string> testIdsByDetailViewId) in
                 PassportDetailViewE2eActionHooks.ToolbarActions)
        {
            if (!testIdsByDetailViewId.TryGetValue(detailViewId, out string testId))
            {
                continue;
            }

            _ = EnsureToolbarActionHookAsync(actionId, testId);
        }
    }

    private async Task EnsureToolbarActionHookAsync(string actionId, string testId)
    {
        IJSRuntime? jsRuntime = ResolveJsRuntime();
        if (jsRuntime == null)
        {
            return;
        }

        for (int attempt = 0; attempt < 12; attempt++)
        {
            bool applied = await jsRuntime.InvokeAsync<bool>(
                "visa2026E2eHooks.ensurePassportDetailActionTestId",
                actionId,
                testId);
            if (applied)
            {
                return;
            }

            await Task.Delay(250);
        }
    }

    private async Task StopToolbarActionWatchAsync()
    {
        IJSRuntime? jsRuntime = ResolveJsRuntime();
        if (jsRuntime == null)
        {
            return;
        }

        try
        {
            await jsRuntime.InvokeVoidAsync("visa2026E2eHooks.stopPassportDetailToolbarWatch");
        }
        catch (JSDisconnectedException)
        {
            // Circuit torn down during navigation — safe to ignore.
        }
    }

    private IJSRuntime? ResolveJsRuntime()
    {
        if (Application is not BlazorApplication blazorApplication)
        {
            return null;
        }

        return blazorApplication.ServiceProvider?.GetService<IJSRuntime>();
    }
}
