#nullable enable
using System;
using DevExpress.ExpressApp.Blazor.Components.Models;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using Visa2026.Module.Services.ApplicationPersonLink;
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

    public EventCallback<Guid> UnlinkPersonRequested
    {
        get => GetPropertyValue<EventCallback<Guid>>();
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

    public string CaseTab
    {
        get => GetPropertyValue<string>() ?? "overview";
        set => SetPropertyValue(value);
    }

    public string? PeopleLinkedRecordFocusKey
    {
        get => GetPropertyValue<string?>();
        set => SetPropertyValue(value);
    }

    public EventCallback<string> CaseTabChanged
    {
        get => GetPropertyValue<EventCallback<string>>();
        set => SetPropertyValue(value);
    }

    public EventCallback<string> LinkedRecordTileClicked
    {
        get => GetPropertyValue<EventCallback<string>>();
        set => SetPropertyValue(value);
    }

    public EventCallback<string> IssuedHeaderNewRequested
    {
        get => GetPropertyValue<EventCallback<string>>();
        set => SetPropertyValue(value);
    }

    public EventCallback<ApplicationWorkspaceIssuedHeaderOpenRequest> IssuedHeaderOpenRequested
    {
        get => GetPropertyValue<EventCallback<ApplicationWorkspaceIssuedHeaderOpenRequest>>();
        set => SetPropertyValue(value);
    }

    public EventCallback BackToListRequested
    {
        get => GetPropertyValue<EventCallback>();
        set => SetPropertyValue(value);
    }

    public EventCallback OpenResminamalarRequested
    {
        get => GetPropertyValue<EventCallback>();
        set => SetPropertyValue(value);
    }

    public EventCallback<int> OpenPersonDetailByIndexRequested
    {
        get => GetPropertyValue<EventCallback<int>>();
        set => SetPropertyValue(value);
    }

    public EventCallback<Guid> RelinkPersonRequested
    {
        get => GetPropertyValue<EventCallback<Guid>>();
        set => SetPropertyValue(value);
    }

    public string? ProgressStatusMessage
    {
        get => GetPropertyValue<string?>();
        set => SetPropertyValue(value);
    }

    public bool ProgressStatusIsError
    {
        get => GetPropertyValue<bool>();
        set => SetPropertyValue(value);
    }

    public bool ShowProgressRevertToHere
    {
        get => GetPropertyValue<bool>();
        set => SetPropertyValue(value);
    }

    public EventCallback<string> SaveProgressNotesRequested
    {
        get => GetPropertyValue<EventCallback<string>>();
        set => SetPropertyValue(value);
    }

    public EventCallback<OfficerShellCaseProgressFileUpload> UploadMinistryLetterRequested
    {
        get => GetPropertyValue<EventCallback<OfficerShellCaseProgressFileUpload>>();
        set => SetPropertyValue(value);
    }

    public EventCallback<OfficerShellCaseProgressAdvanceRequest> AdvanceProgressRequested
    {
        get => GetPropertyValue<EventCallback<OfficerShellCaseProgressAdvanceRequest>>();
        set => SetPropertyValue(value);
    }

    public EventCallback<OfficerShellCaseProgressRevertRequest> RevertProgressRequested
    {
        get => GetPropertyValue<EventCallback<OfficerShellCaseProgressRevertRequest>>();
        set => SetPropertyValue(value);
    }

    public bool ShowPersonLinkPicker
    {
        get => GetPropertyValue<bool>();
        set => SetPropertyValue(value);
    }

    public IReadOnlyList<ApplicationProfileInstancePersonLinkCandidateRow> PersonLinkCandidates
    {
        get => GetPropertyValue<IReadOnlyList<ApplicationProfileInstancePersonLinkCandidateRow>>()
            ?? Array.Empty<ApplicationProfileInstancePersonLinkCandidateRow>();
        set => SetPropertyValue(value);
    }

    public bool PersonLinkIsSearching
    {
        get => GetPropertyValue<bool>();
        set => SetPropertyValue(value);
    }

    public bool PersonLinkIsLinking
    {
        get => GetPropertyValue<bool>();
        set => SetPropertyValue(value);
    }

    public string? PersonLinkStatusMessage
    {
        get => GetPropertyValue<string?>();
        set => SetPropertyValue(value);
    }

    public bool PersonLinkStatusIsError
    {
        get => GetPropertyValue<bool>();
        set => SetPropertyValue(value);
    }

    public EventCallback<string> PersonLinkSearchRequested
    {
        get => GetPropertyValue<EventCallback<string>>();
        set => SetPropertyValue(value);
    }

    public EventCallback<Guid> LinkPersonFromPickerRequested
    {
        get => GetPropertyValue<EventCallback<Guid>>();
        set => SetPropertyValue(value);
    }

    public EventCallback ClosePersonLinkPickerRequested
    {
        get => GetPropertyValue<EventCallback>();
        set => SetPropertyValue(value);
    }
}
