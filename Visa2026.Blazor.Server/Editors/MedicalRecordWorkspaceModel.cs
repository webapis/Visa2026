#nullable enable
using System;
using System.Collections.Generic;
using DevExpress.ExpressApp.Blazor.Components.Models;
using Microsoft.AspNetCore.Components;
using Visa2026.Module.Services.MedicalRecordWorkspace;

namespace Visa2026.Blazor.Server.Editors;

public sealed class MedicalRecordWorkspaceModel : ComponentModelBase
{
    public override Type ComponentType => typeof(MedicalRecordWorkspaceComponent);

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

    public MedicalRecordWorkspaceMode Mode
    {
        get => GetPropertyValue<MedicalRecordWorkspaceMode>();
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

    public bool ListTruncated
    {
        get => GetPropertyValue<bool>();
        set => SetPropertyValue(value);
    }

    public IReadOnlyList<MedicalRecordListRow> Rows
    {
        get => GetPropertyValue<IReadOnlyList<MedicalRecordListRow>>() ?? Array.Empty<MedicalRecordListRow>();
        set => SetPropertyValue(value);
    }

    public IReadOnlyList<MedicalRecordValidityOption> ValidityOptions
    {
        get => GetPropertyValue<IReadOnlyList<MedicalRecordValidityOption>>() ?? Array.Empty<MedicalRecordValidityOption>();
        set => SetPropertyValue(value);
    }

    public IReadOnlyList<MedicalRecordPersonHit> PersonHits
    {
        get => GetPropertyValue<IReadOnlyList<MedicalRecordPersonHit>>() ?? Array.Empty<MedicalRecordPersonHit>();
        set => SetPropertyValue(value);
    }

    public MedicalRecordDetailDto? Detail
    {
        get => GetPropertyValue<MedicalRecordDetailDto?>();
        set => SetPropertyValue(value);
    }

    public int DetailRevision
    {
        get => GetPropertyValue<int>();
        set => SetPropertyValue(value);
    }

    public EventCallback InitialLoadRequested
    {
        get => GetPropertyValue<EventCallback>();
        set => SetPropertyValue(value);
    }

    public EventCallback<string> SearchRequested
    {
        get => GetPropertyValue<EventCallback<string>>();
        set => SetPropertyValue(value);
    }

    public EventCallback<Guid> OpenRequested
    {
        get => GetPropertyValue<EventCallback<Guid>>();
        set => SetPropertyValue(value);
    }

    public EventCallback NewRequested
    {
        get => GetPropertyValue<EventCallback>();
        set => SetPropertyValue(value);
    }

    public EventCallback<string> PersonSearchRequested
    {
        get => GetPropertyValue<EventCallback<string>>();
        set => SetPropertyValue(value);
    }

    public EventCallback<MedicalRecordSaveRequest> SaveRequested
    {
        get => GetPropertyValue<EventCallback<MedicalRecordSaveRequest>>();
        set => SetPropertyValue(value);
    }

    public EventCallback<MedicalRecordPreviewRequest> PreviewRequested
    {
        get => GetPropertyValue<EventCallback<MedicalRecordPreviewRequest>>();
        set => SetPropertyValue(value);
    }

    public EventCallback CancelRequested
    {
        get => GetPropertyValue<EventCallback>();
        set => SetPropertyValue(value);
    }
}
