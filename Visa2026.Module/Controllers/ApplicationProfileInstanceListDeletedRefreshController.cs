using System;
using DevExpress.ExpressApp;
using Microsoft.Extensions.DependencyInjection;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationWorkspace;

#nullable enable

namespace Visa2026.Module.Controllers;

/// <summary>
/// Refreshes open ApplicationProfileInstance ListViews after a case is deleted from its workspace tab,
/// so the list that opened the workspace no longer shows the deleted row.
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
        if (_notifier != null)
            _notifier.Deleted += OnInstanceDeleted;
    }

    protected override void OnDeactivated()
    {
        if (_notifier != null)
        {
            _notifier.Deleted -= OnInstanceDeleted;
            _notifier = null;
        }

        base.OnDeactivated();
    }

    private void OnInstanceDeleted(Guid instanceId)
    {
        if (View?.ObjectSpace == null || View.ObjectSpace.IsDisposed)
            return;

        View.ObjectSpace.Refresh();
    }
}
