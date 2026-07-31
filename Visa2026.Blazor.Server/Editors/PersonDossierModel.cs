#nullable enable
using System;
using DevExpress.ExpressApp.Blazor.Components.Models;
using Microsoft.AspNetCore.Components;
using Visa2026.Module.Services.PersonDossier;

namespace Visa2026.Blazor.Server.Editors;

public sealed class PersonDossierModel : ComponentModelBase
{
    public override Type ComponentType => typeof(PersonDossierComponent);

    public PersonDossierSnapshot? Snapshot
    {
        get => GetPropertyValue<PersonDossierSnapshot?>();
        set => SetPropertyValue(value);
    }

    public bool IsLoading
    {
        get => GetPropertyValue<bool>();
        set => SetPropertyValue(value);
    }

    /// <summary>0-100 while loading; negative means indeterminate bar.</summary>
    public int LoadingProgressPercent
    {
        get => GetPropertyValue<int>();
        set => SetPropertyValue(value);
    }

    public string LoadingMessage
    {
        get => GetPropertyValue<string>() ?? string.Empty;
        set => SetPropertyValue(value ?? string.Empty);
    }

    public EventCallback InitialLoadRequested
    {
        get => GetPropertyValue<EventCallback>();
        set => SetPropertyValue(value);
    }

    /// <summary>Opens person document copies in the preview slot beside the dossier.</summary>
    public EventCallback OpenCopiesRequested
    {
        get => GetPropertyValue<EventCallback>();
        set => SetPropertyValue(value);
    }

    /// <summary>Queues the director hand-over export; progress is shown by the global toast.</summary>
    public EventCallback ExportRequested
    {
        get => GetPropertyValue<EventCallback>();
        set => SetPropertyValue(value);
    }

    /// <summary>Set after a queue attempt so the button can report the outcome inline.</summary>
    public string? ExportMessage
    {
        get => GetPropertyValue<string?>();
        set => SetPropertyValue(value);
    }

    public bool IsExportQueued
    {
        get => GetPropertyValue<bool>();
        set => SetPropertyValue(value);
    }
}