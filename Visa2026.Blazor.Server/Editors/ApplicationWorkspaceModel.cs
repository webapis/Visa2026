#nullable enable
using System;
using DevExpress.ExpressApp.Blazor.Components.Models;
using Microsoft.AspNetCore.Components;
using Visa2026.Module.Services.ApplicationWorkspace;

namespace Visa2026.Blazor.Server.Editors;

public sealed class ApplicationWorkspaceModel : ComponentModelBase
{
    public override Type ComponentType => typeof(ApplicationWorkspaceComponent);

    public ApplicationWorkspaceSnapshot? Snapshot
    {
        get => GetPropertyValue<ApplicationWorkspaceSnapshot?>();
        set => SetPropertyValue(value);
    }

    public bool IsLoading
    {
        get => GetPropertyValue<bool>();
        set => SetPropertyValue(value);
    }

    public EventCallback InitialLoadRequested
    {
        get => GetPropertyValue<EventCallback>();
        set => SetPropertyValue(value);
    }

    public bool CanLinkPerson
    {
        get => GetPropertyValue<bool>();
        set => SetPropertyValue(value);
    }

    public bool CanUnlinkPerson
    {
        get => GetPropertyValue<bool>();
        set => SetPropertyValue(value);
    }

    public bool CanOpenPersonDetail
    {
        get => GetPropertyValue<bool>();
        set => SetPropertyValue(value);
    }

    public bool CanOpenDocumentCopies
    {
        get => GetPropertyValue<bool>();
        set => SetPropertyValue(value);
    }

    public int SelectedPersonRowIndex
    {
        get => GetPropertyValue<int>();
        set => SetPropertyValue(value);
    }

    public EventCallback LinkPersonRequested
    {
        get => GetPropertyValue<EventCallback>();
        set => SetPropertyValue(value);
    }

    public EventCallback UnlinkPersonRequested
    {
        get => GetPropertyValue<EventCallback>();
        set => SetPropertyValue(value);
    }

    public EventCallback OpenPersonDetailRequested
    {
        get => GetPropertyValue<EventCallback>();
        set => SetPropertyValue(value);
    }

    public EventCallback OpenDocumentCopiesRequested
    {
        get => GetPropertyValue<EventCallback>();
        set => SetPropertyValue(value);
    }

    public EventCallback<int> SelectPersonRowRequested
    {
        get => GetPropertyValue<EventCallback<int>>();
        set => SetPropertyValue(value);
    }

    public EventCallback<Guid> NewApplicationFromProfileRequested
    {
        get => GetPropertyValue<EventCallback<Guid>>();
        set => SetPropertyValue(value);
    }

    public EventCallback<Guid> OpenProfileConfigRequested
    {
        get => GetPropertyValue<EventCallback<Guid>>();
        set => SetPropertyValue(value);
    }
}
