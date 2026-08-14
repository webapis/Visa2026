#nullable enable
using System;
using System.Collections.Generic;
using DevExpress.ExpressApp.Blazor.Components.Models;
using Microsoft.AspNetCore.Components;
using Visa2026.Module.Services.ApplicationProfileCatalog;
using Visa2026.Module.Services.ApplicationProfileOverview;

namespace Visa2026.Blazor.Server.Editors;

public sealed class ApplicationProfileCatalogModel : ComponentModelBase
{
    public override Type ComponentType => typeof(ApplicationProfileCatalogComponent);

    public IReadOnlyList<ApplicationProfileCatalogRow> Rows
    {
        get => GetPropertyValue<IReadOnlyList<ApplicationProfileCatalogRow>>()
            ?? Array.Empty<ApplicationProfileCatalogRow>();
        set => SetPropertyValue(value);
    }

    public Guid SelectedProfileId
    {
        get => GetPropertyValue<Guid>();
        set => SetPropertyValue(value);
    }

    public ApplicationProfileOverviewSnapshot? OverviewSnapshot
    {
        get => GetPropertyValue<ApplicationProfileOverviewSnapshot?>();
        set => SetPropertyValue(value);
    }

    public bool IsOverviewLoading
    {
        get => GetPropertyValue<bool>();
        set => SetPropertyValue(value);
    }

    public string SearchText
    {
        get => GetPropertyValue<string>() ?? string.Empty;
        set => SetPropertyValue(value);
    }

    public bool IsLoading
    {
        get => GetPropertyValue<bool>();
        set => SetPropertyValue(value);
    }

    public string? StatusMessage
    {
        get => GetPropertyValue<string?>();
        set => SetPropertyValue(value);
    }

    public bool IsStatusError
    {
        get => GetPropertyValue<bool>();
        set => SetPropertyValue(value);
    }

    public EventCallback InitialLoadRequested
    {
        get => GetPropertyValue<EventCallback>();
        set => SetPropertyValue(value);
    }

    public EventCallback NewProfileRequested
    {
        get => GetPropertyValue<EventCallback>();
        set => SetPropertyValue(value);
    }

    public EventCallback<Guid> SelectProfileRequested
    {
        get => GetPropertyValue<EventCallback<Guid>>();
        set => SetPropertyValue(value);
    }

    public EventCallback ConfigureRequested
    {
        get => GetPropertyValue<EventCallback>();
        set => SetPropertyValue(value);
    }

    public EventCallback<string> SearchTextChanged
    {
        get => GetPropertyValue<EventCallback<string>>();
        set => SetPropertyValue(value);
    }

    public EventCallback CloseProfileRequested
    {
        get => GetPropertyValue<EventCallback>();
        set => SetPropertyValue(value);
    }

    public EventCallback<Guid> OpenInstanceRequested
    {
        get => GetPropertyValue<EventCallback<Guid>>();
        set => SetPropertyValue(value);
    }
}