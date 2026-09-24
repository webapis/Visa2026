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

    /// <summary>Opens the Application Profile Instance workspace for one Applications-row id.</summary>
    public EventCallback<Guid> OpenApplicationRequested
    {
        get => GetPropertyValue<EventCallback<Guid>>();
        set => SetPropertyValue(value);
    }

    /// <summary>Opens a header document copy (e.g. invitation scan) preview-only in the slot.</summary>
    public EventCallback<PersonDossierRecord> PreviewRecordRequested
    {
        get => GetPropertyValue<EventCallback<PersonDossierRecord>>();
        set => SetPropertyValue(value);
    }

    /// <summary>Opens the upload surface for a header that has no copy on file yet.</summary>
    public EventCallback<PersonDossierRecord> UploadRecordRequested
    {
        get => GetPropertyValue<EventCallback<PersonDossierRecord>>();
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