#nullable enable
using System;
using DevExpress.ExpressApp.Blazor.Components.Models;
using Microsoft.AspNetCore.Components;
using Visa2026.Module.Services.ApplicationResultsDesk;

namespace Visa2026.Blazor.Server.Editors;

public sealed class ApplicationResultsDeskModel : ComponentModelBase
{
    public override Type ComponentType => typeof(ApplicationResultsDeskComponent);

    public bool IsLoading
    {
        get => GetPropertyValue<bool>();
        set => SetPropertyValue(value);
    }

    public bool IsBusy
    {
        get => GetPropertyValue<bool>();
        set => SetPropertyValue(value);
    }

    public bool IsStatusError
    {
        get => GetPropertyValue<bool>();
        set => SetPropertyValue(value);
    }

    public string? StatusMessage
    {
        get => GetPropertyValue<string?>();
        set => SetPropertyValue(value);
    }

    public ApplicationResultsDeskPage? Page
    {
        get => GetPropertyValue<ApplicationResultsDeskPage?>();
        set => SetPropertyValue(value);
    }

    public EventCallback InitialLoadRequested
    {
        get => GetPropertyValue<EventCallback>();
        set => SetPropertyValue(value);
    }

    public EventCallback<ApplicationResultsDeskRequest> LoadRequested
    {
        get => GetPropertyValue<EventCallback<ApplicationResultsDeskRequest>>();
        set => SetPropertyValue(value);
    }

    public EventCallback<ApplicationResultsDeskHeaderOpen> OpenHeaderRequested
    {
        get => GetPropertyValue<EventCallback<ApplicationResultsDeskHeaderOpen>>();
        set => SetPropertyValue(value);
    }

    public EventCallback<Guid> OpenCaseRequested
    {
        get => GetPropertyValue<EventCallback<Guid>>();
        set => SetPropertyValue(value);
    }

    public EventCallback<ApplicationResultsDeskLetterSave> SaveLetterRequested
    {
        get => GetPropertyValue<EventCallback<ApplicationResultsDeskLetterSave>>();
        set => SetPropertyValue(value);
    }

    public EventCallback<ApplicationResultsDeskPersonOpen> OpenPersonRequested
    {
        get => GetPropertyValue<EventCallback<ApplicationResultsDeskPersonOpen>>();
        set => SetPropertyValue(value);
    }

    public EventCallback<ApplicationResultsDeskDetailSave> SaveDetailRequested
    {
        get => GetPropertyValue<EventCallback<ApplicationResultsDeskDetailSave>>();
        set => SetPropertyValue(value);
    }

    public EventCallback<ApplicationResultsDeskHeaderSave> SaveHeaderRequested
    {
        get => GetPropertyValue<EventCallback<ApplicationResultsDeskHeaderSave>>();
        set => SetPropertyValue(value);
    }
}
