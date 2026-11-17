using System;
using DevExpress.ExpressApp;
using Microsoft.Extensions.DependencyInjection;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationWorkspace;

#nullable enable

namespace Visa2026.Module.Controllers;

/// <summary>
/// Refreshes open ApplicationProfileInstance ListViews after a case is deleted or its
/// special-follow-up flag changes from a workspace tab, so list rows and color-tab counts stay current.
/// </summary>
public sealed class ApplicationProfileInstanceListDeletedRefreshController : ViewController<ListView>
{
    private IApplicationProfileInstanceDeletedNotifier? _notifier;

    public ApplicationProfileInstanceListDeletedRefreshController()
    {
        TargetObjectType = typeof(ApplicationProfileInstance);
        TargetViewNesting = Nesting.Root;
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        _notifier = Application?.ServiceProvider?.GetService<IApplicationProfileInstanceDeletedNotifier>();
        if (_notifier == null)
            return;

        _notifier.Deleted += OnInstanceDeleted;
        _notifier.SpecialFollowUpChanged += OnSpecialFollowUpChanged;
    }

    protected override void OnDeactivated()
    {
        if (_notifier != null)
        {
            _notifier.Deleted -= OnInstanceDeleted;
            _notifier.SpecialFollowUpChanged -= OnSpecialFollowUpChanged;
            _notifier = null;
        }

        base.OnDeactivated();
    }

    private void OnInstanceDeleted(Guid instanceId) => RefreshList();

    private void OnSpecialFollowUpChanged(Guid instanceId, bool enabled) => RefreshList();

    private void RefreshList()
    {
        if (View?.ObjectSpace == null || View.ObjectSpace.IsDisposed)
            return;

        View.ObjectSpace.Refresh();
    }
}