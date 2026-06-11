using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.Blazor.Layout;
using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.SystemModule;
using Microsoft.AspNetCore.Components;
using Visa2026.Module;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Anchors <c>person-employee-tab-passports-new</c> on the Person detail <see cref="Person.Passports"/>
/// <see cref="ListPropertyEditor"/> nested frame (survives tab restore, detail re-open, and toolbar re-render).
/// JS <see cref="PersonDetailPassportsNestedNewHookSupport"/> remains the shadow-DOM fallback.
/// </summary>
public sealed class PersonDetailPassportsListNewE2eAnchorController : ViewController<DetailView>
{
    private const string PassportsMemberName = nameof(Person.Passports);
    private const string CollectionTabsGroupId = "Tabs";

    private readonly HashSet<Frame> _hookedFrames = new();
    private BlazorLayoutManager? _layoutManager;
    private DxFormLayoutTabPagesModel? _collectionTabs;
    private CancellationTokenSource? _burstCts;

    public PersonDetailPassportsListNewE2eAnchorController()
    {
        TargetObjectType = typeof(Person);
        TargetViewId =
            $"{PersonDetailViewIds.Default};{PersonDetailViewIds.Employee};{PersonDetailViewIds.FamilyMember};{PersonDetailViewIds.TemporaryVisitor}";
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        View.CustomizeViewItemControl<ListPropertyEditor>(this, ConfigurePassportsListEditor);
        ObjectSpace.Committed += OnObjectSpaceCommitted;
        AttachLayoutManager();
        RefreshPassportsNewHooks();
        SchedulePassportsBurstRefresh();
        _ = RetryHookPassportsWhenFramesReadyAsync();
    }

    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        RefreshPassportsNewHooks();
        SchedulePassportsBurstRefresh();
        _ = RetryHookPassportsWhenFramesReadyAsync();
    }

    protected override void OnDeactivated()
    {
        ObjectSpace.Committed -= OnObjectSpaceCommitted;
        _burstCts?.Cancel();
        _burstCts?.Dispose();
        _burstCts = null;
        DetachLayoutManager();
        DetachCollectionTabsHandler();
        UnhookPassportsListEditors();
        if (Application is BlazorApplication blazorApplication)
        {
            _ = PersonDetailPassportsNestedNewHookSupport.StopWatchAsync(blazorApplication);
        }

        base.OnDeactivated();
    }

    private void AttachLayoutManager()
    {
        if (View?.LayoutManager is not BlazorLayoutManager layoutManager)
        {
            return;
        }

        if (_layoutManager == layoutManager)
        {
            return;
        }

        DetachLayoutManager();
        _layoutManager = layoutManager;
        _layoutManager.ItemCreated += LayoutManager_ItemCreated;
    }

    private void DetachLayoutManager()
    {
        if (_layoutManager == null)
        {
            return;
        }

        _layoutManager.ItemCreated -= LayoutManager_ItemCreated;
        _layoutManager = null;
    }

    private void LayoutManager_ItemCreated(object? sender, BlazorLayoutManager.ItemCreatedEventArgs e)
    {
        if (e.ModelLayoutElement.Id == CollectionTabsGroupId
            && e.LayoutControlItem is DxFormLayoutTabPagesModel tabStrip)
        {
            AttachCollectionTabsHandler(tabStrip);
            return;
        }

        if (e.ModelLayoutElement.Id == "Passports")
        {
            RefreshPassportsNewHooks();
            SchedulePassportsBurstRefresh();
        }
    }

    private void AttachCollectionTabsHandler(DxFormLayoutTabPagesModel tabStrip)
    {
        if (_collectionTabs == tabStrip)
        {
            return;
        }

        _collectionTabs = tabStrip;
        EventCallback<int> previous = tabStrip.ActiveTabIndexChanged;
        tabStrip.ActiveTabIndexChanged = EventCallback.Factory.Create<int>(
            this,
            async index =>
            {
                if (previous.HasDelegate)
                {
                    await previous.InvokeAsync(index);
                }

                RebindPassportsListEditors();
                SchedulePassportsBurstRefresh();
                _ = RetryHookPassportsWhenFramesReadyAsync();
            });

        RefreshPassportsNewHooks();
        SchedulePassportsBurstRefresh();
    }

    private void OnObjectSpaceCommitted(object? sender, EventArgs e)
    {
        if (View?.CurrentObject is not Person person || ObjectSpace.IsNewObject(person))
        {
            return;
        }

        UnhookPassportsListEditors();
        RebindPassportsListEditors();
        SchedulePassportsBurstRefresh();
        _ = RetryHookPassportsWhenFramesReadyAsync();
        _ = DelayedPostSaveHookRefreshAsync();
    }

    private void DetachCollectionTabsHandler()
    {
        _collectionTabs = null;
    }

    private void ConfigurePassportsListEditor(ListPropertyEditor editor)
    {
        if (!IsPassportsEditor(editor))
        {
            return;
        }

        editor.ControlCreated += PassportsListEditor_ControlCreated;
        TryHookPassportsListEditor(editor);
    }

    private void PassportsListEditor_ControlCreated(object? sender, EventArgs e)
    {
        if (sender is ListPropertyEditor editor)
        {
            TryHookPassportsListEditor(editor);
        }
    }

    private void RefreshPassportsNewHooks() => RebindPassportsListEditors();

    private void RebindPassportsListEditors()
    {
        if (View == null)
        {
            return;
        }

        foreach (ListPropertyEditor editor in View.GetItems<ListPropertyEditor>())
        {
            if (!IsPassportsEditor(editor))
            {
                continue;
            }

            editor.ControlCreated -= PassportsListEditor_ControlCreated;
            editor.ControlCreated += PassportsListEditor_ControlCreated;
            TryHookPassportsListEditor(editor);
        }

        ScheduleJsEnsure();
    }

    private void TryHookPassportsListEditor(ListPropertyEditor editor)
    {
        Frame? frame = editor.Frame;
        if (frame == null)
        {
            return;
        }

        if (!_hookedFrames.Contains(frame))
        {
            _hookedFrames.Add(frame);
            frame.ViewChanged += NestedPassportsFrame_ViewChanged;
        }

        HookNewObjectAction(frame);
    }

    private void NestedPassportsFrame_ViewChanged(object? sender, ViewChangedEventArgs e)
    {
        if (sender is Frame frame)
        {
            HookNewObjectAction(frame);
        }
    }

    private void HookNewObjectAction(Frame frame)
    {
        NewObjectViewController? newObjectController = frame.GetController<NewObjectViewController>();
        if (newObjectController?.NewObjectAction == null)
        {
            return;
        }

        newObjectController.NewObjectAction.CustomizeControl -= NewObjectAction_CustomizeControl;
        newObjectController.NewObjectAction.CustomizeControl += NewObjectAction_CustomizeControl;
        E2eActionControlSelectorSupport.TryRaiseCustomizeControl(newObjectController.NewObjectAction);
    }

    private void NewObjectAction_CustomizeControl(object? sender, CustomizeControlEventArgs e)
    {
        E2eActionControlSelectorSupport.TryApplyFromActionControl(
            e.Control,
            PassportListViewE2eActionHooks.PersonDetailPassportsNewTestId,
            sender as ActionBase);
    }

    private void UnhookPassportsListEditors()
    {
        foreach (ListPropertyEditor editor in View.GetItems<ListPropertyEditor>())
        {
            if (!IsPassportsEditor(editor))
            {
                continue;
            }

            editor.ControlCreated -= PassportsListEditor_ControlCreated;
            Frame? frame = editor.Frame;
            if (frame == null)
            {
                continue;
            }

            frame.ViewChanged -= NestedPassportsFrame_ViewChanged;
            NewObjectViewController? newObjectController = frame.GetController<NewObjectViewController>();
            if (newObjectController?.NewObjectAction != null)
            {
                newObjectController.NewObjectAction.CustomizeControl -= NewObjectAction_CustomizeControl;
            }
        }

        _hookedFrames.Clear();
    }

    private static bool IsPassportsEditor(ListPropertyEditor editor)
    {
        if (string.Equals(editor.Id, PassportsMemberName, StringComparison.Ordinal))
        {
            return true;
        }

        string? memberName = editor.MemberInfo?.Name;
        return string.Equals(memberName, PassportsMemberName, StringComparison.Ordinal);
    }

    private void ScheduleJsEnsure(bool includeDelayedPasses = true)
    {
        if (Application is BlazorApplication blazorApplication)
        {
            PersonDetailPassportsNestedNewHookSupport.ScheduleEnsure(blazorApplication, includeDelayedPasses);
        }
    }

    private async Task RetryHookPassportsWhenFramesReadyAsync()
    {
        for (int attempt = 0; attempt < 48; attempt++)
        {
            if (Frame == null || View == null)
            {
                return;
            }

            bool missingFrame = false;
            foreach (ListPropertyEditor editor in View.GetItems<ListPropertyEditor>())
            {
                if (!IsPassportsEditor(editor))
                {
                    continue;
                }

                if (editor.Frame == null)
                {
                    missingFrame = true;
                    continue;
                }

                TryHookPassportsListEditor(editor);
            }

            ScheduleJsEnsure();
            if (!missingFrame)
            {
                return;
            }

            await Task.Delay(250);
        }
    }

    private void SchedulePassportsBurstRefresh()
    {
        _burstCts?.Cancel();
        _burstCts?.Dispose();
        _burstCts = new CancellationTokenSource();
        _ = PassportsBurstRefreshAsync(_burstCts.Token);
    }

    private async Task PassportsBurstRefreshAsync(CancellationToken cancellationToken)
    {
        int[] delaysMs = [150, 400, 800, 1500, 3000];
        foreach (int delayMs in delaysMs)
        {
            if (cancellationToken.IsCancellationRequested || Frame == null || View == null)
            {
                return;
            }

            await Task.Delay(delayMs, cancellationToken);
            RefreshPassportsNewHooks();
        }
    }

    private async Task DelayedPostSaveHookRefreshAsync()
    {
        int[] delaysMs = [500, 1200, 2500, 5000, 8000];
        foreach (int delayMs in delaysMs)
        {
            if (Frame == null || View?.CurrentObject is not Person person || ObjectSpace.IsNewObject(person))
            {
                return;
            }

            await Task.Delay(delayMs);
            if (Frame == null || View == null)
            {
                return;
            }

            RebindPassportsListEditors();
            ScheduleJsEnsure(includeDelayedPasses: false);
        }
    }
}
