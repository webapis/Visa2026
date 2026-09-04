#nullable enable
using System;
using DevExpress.ExpressApp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Orange / blue / green field borders on new issued-record DetailViews
/// (Invitation, WorkPermit, BorderZone, Rejection, Visa and their items).
/// </summary>
public sealed class IssuedObjectNewDetailCueController : ViewController<DetailView>
{
    private bool _attached;

    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        AttachIfNeeded();
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        View.CurrentObjectChanged += OnCurrentObjectChanged;
    }

    protected override void OnDeactivated()
    {
        View.CurrentObjectChanged -= OnCurrentObjectChanged;
        Detach();
        base.OnDeactivated();
    }

    private void OnCurrentObjectChanged(object? sender, EventArgs e)
    {
        Detach();
        AttachIfNeeded();
    }

    private void AttachIfNeeded()
    {
        if (_attached || !ShouldCue())
            return;

        var js = Application.ServiceProvider?.GetService<IJSRuntime>();
        if (js == null)
            return;

        _attached = true;
        _ = js.InvokeVoidAsync("visaIssuedFieldCue.attach");
    }

    private void Detach()
    {
        if (!_attached)
            return;

        _attached = false;
        var js = Application.ServiceProvider?.GetService<IJSRuntime>();
        if (js == null)
            return;

        _ = js.InvokeVoidAsync("visaIssuedFieldCue.detach");
    }

    private bool ShouldCue()
    {
        var current = View?.CurrentObject;
        if (current == null || View?.ObjectSpace == null)
            return false;
        if (!View.ObjectSpace.IsNewObject(current))
            return false;
        return current is Invitation or InvitationItem
            or WorkPermit or WorkPermitItem
            or BorderZone or BorderZoneItem
            or Rejection or RejectionItem
            or Visa;
    }
}