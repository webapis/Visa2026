using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor;
using Microsoft.JSInterop;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Applies stable <c>data-testid</c> / <c>.e2e-*</c> on typed Person detail view toolbar
/// modification actions (<c>Save</c>, <c>SaveAndClose</c>, <c>SaveAndNew</c>). DevExpress view
/// toolbars render inside nested open shadow roots; re-applies on activate and when the toolbar
/// re-renders after tab / nav switches (<c>visa2026-e2e-hooks.js</c> MutationObserver + URL sync).
/// </summary>
public sealed class PersonDetailViewE2eActionSelectorsController : ViewController<DetailView>
{
    public PersonDetailViewE2eActionSelectorsController()
    {
        TargetObjectType = typeof(Person);
        TargetViewId =
            "Person_DetailView_Employee;Person_DetailView_FamilyMember;Person_DetailView_TemporaryVisitor";
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
                 PersonDetailViewE2eActionHooks.ToolbarActions)
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
                "visa2026E2eHooks.ensurePersonDetailActionTestId",
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
            await jsRuntime.InvokeVoidAsync("visa2026E2eHooks.stopPersonDetailToolbarWatch");
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
