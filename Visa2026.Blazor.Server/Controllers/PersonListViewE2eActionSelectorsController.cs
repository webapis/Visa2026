using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor;
using Microsoft.JSInterop;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Applies stable <c>data-testid</c> / <c>.e2e-*</c> on typed Person ListView toolbar
/// <c>New</c> and <c>Delete</c> buttons. DevExpress view toolbars render inside nested open
/// shadow roots; re-applies on activate and when the toolbar re-renders after tab / nav switches
/// (<c>visa2026-e2e-hooks.js</c> MutationObserver + URL sync).
/// </summary>
public sealed class PersonListViewE2eActionSelectorsController : ViewController<ListView>
{
    public PersonListViewE2eActionSelectorsController()
    {
        TargetViewId =
            "Person_ListView_Employees;Person_ListView_FamilyMembers;Person_ListView_TemporaryVisitors";
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
        string? listViewId = View?.Id;
        if (string.IsNullOrEmpty(listViewId))
        {
            return;
        }

        if (PersonListViewE2eActionHooks.TryGetNewActionTestId(listViewId, out string newTestId))
        {
            _ = EnsureToolbarActionHookAsync("visa2026E2eHooks.ensureNewActionTestId", newTestId);
        }

        if (PersonListViewE2eActionHooks.TryGetDeleteActionTestId(listViewId, out string deleteTestId))
        {
            _ = EnsureToolbarActionHookAsync("visa2026E2eHooks.ensureDeleteActionTestId", deleteTestId);
        }
    }

    private async Task EnsureToolbarActionHookAsync(string jsMethod, string testId)
    {
        IJSRuntime? jsRuntime = ResolveJsRuntime();
        if (jsRuntime == null)
        {
            return;
        }

        for (int attempt = 0; attempt < 12; attempt++)
        {
            bool applied = await jsRuntime.InvokeAsync<bool>(jsMethod, testId);
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
            await jsRuntime.InvokeVoidAsync("visa2026E2eHooks.stopPersonListToolbarWatch");
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
