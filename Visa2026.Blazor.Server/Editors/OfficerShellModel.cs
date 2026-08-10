#nullable enable
using System;
using System.Collections.Generic;
using DevExpress.ExpressApp.Blazor.Components.Models;
using Microsoft.AspNetCore.Components;
using Visa2026.Module.Services.ApplicationProfileCatalog;
using Visa2026.Module.Services.ApplicationProfileOverview;
using Visa2026.Module.Services.ApplicationWorkspace;
using Visa2026.Module.Services.OfficerShell;

namespace Visa2026.Blazor.Server.Editors;

public sealed class OfficerShellModel : ComponentModelBase
{
    public override Type ComponentType => typeof(OfficerShellComponent);

    public OfficerShellPage CurrentPage
    {
        get => GetPropertyValue<OfficerShellPage>();
        set => SetPropertyValue(value);
    }

    public OfficerShellNavCounts NavCounts
    {
        get => GetPropertyValue<OfficerShellNavCounts>() ?? new OfficerShellNavCounts();
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

    public IReadOnlyList<OfficerShellStagedRow> StagedRows
    {
        get => GetPropertyValue<IReadOnlyList<OfficerShellStagedRow>>() ?? Array.Empty<OfficerShellStagedRow>();
        set => SetPropertyValue(value);
    }

    public IReadOnlyList<OfficerShellInProcessRow> InProcessRows
    {
        get => GetPropertyValue<IReadOnlyList<OfficerShellInProcessRow>>() ?? Array.Empty<OfficerShellInProcessRow>();
        set => SetPropertyValue(value);
    }

    public HashSet<Guid> SelectedStagedIds
    {
        get => GetPropertyValue<HashSet<Guid>>() ?? new HashSet<Guid>();
        set => SetPropertyValue(value);
    }

    public string StagedViewMode
    {
        get => GetPropertyValue<string>() ?? "list";
        set => SetPropertyValue(value);
    }

    public string InProcessViewMode
    {
        get => GetPropertyValue<string>() ?? "list";
        set => SetPropertyValue(value);
    }

    public string SearchText
    {
        get => GetPropertyValue<string>() ?? string.Empty;
        set => SetPropertyValue(value);
    }

    public Guid CaseApplicationId
    {
        get => GetPropertyValue<Guid>();
        set => SetPropertyValue(value);
    }

    public ApplicationWorkspaceSnapshot? WorkspaceSnapshot
    {
        get => GetPropertyValue<ApplicationWorkspaceSnapshot?>();
        set => SetPropertyValue(value);
    }

    public bool WorkspaceLoading
    {
        get => GetPropertyValue<bool>();
        set => SetPropertyValue(value);
    }

    public IReadOnlyList<ApplicationProfileCatalogRow> CatalogRows
    {
        get => GetPropertyValue<IReadOnlyList<ApplicationProfileCatalogRow>>() ?? Array.Empty<ApplicationProfileCatalogRow>();
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

    public string CatalogSearchText
    {
        get => GetPropertyValue<string>() ?? string.Empty;
        set => SetPropertyValue(value);
    }

    public string TemplatesViewMode
    {
        get => GetPropertyValue<string>() ?? "list";
        set => SetPropertyValue(value);
    }

    public string TemplatesFamilyFilter
    {
        get => GetPropertyValue<string>() ?? OfficerShellTemplateFamily.All;
        set => SetPropertyValue(value);
    }

    public int TemplatesPage
    {
        get => GetPropertyValue<int>() is > 0 ? GetPropertyValue<int>() : 1;
        set => SetPropertyValue(value);
    }

    public int TemplatesPageSize
    {
        get => GetPropertyValue<int>() is > 0 ? GetPropertyValue<int>() : 25;
        set => SetPropertyValue(value);
    }

    public bool TemplatesDetailOpen
    {
        get => GetPropertyValue<bool>();
        set => SetPropertyValue(value);
    }

    public string CaseTab
    {
        get => GetPropertyValue<string>() ?? "overview";
        set => SetPropertyValue(value);
    }

    public int SelectedPersonRowIndex
    {
        get => GetPropertyValue<int>();
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

    public string StagedFamilyFilter
    {
        get => GetPropertyValue<string>() ?? OfficerShellTemplateFamily.All;
        set => SetPropertyValue(value);
    }

    public string InProcessFamilyFilter
    {
        get => GetPropertyValue<string>() ?? OfficerShellTemplateFamily.All;
        set => SetPropertyValue(value);
    }

    public int StagedPage
    {
        get => GetPropertyValue<int>() is > 0 ? GetPropertyValue<int>() : 1;
        set => SetPropertyValue(value);
    }

    public int StagedPageSize
    {
        get => GetPropertyValue<int>() is > 0 ? GetPropertyValue<int>() : 25;
        set => SetPropertyValue(value);
    }

    public int InProcessPage
    {
        get => GetPropertyValue<int>() is > 0 ? GetPropertyValue<int>() : 1;
        set => SetPropertyValue(value);
    }

    public int InProcessPageSize
    {
        get => GetPropertyValue<int>() is > 0 ? GetPropertyValue<int>() : 25;
        set => SetPropertyValue(value);
    }

    public HashSet<string> StagedGroupCollapsed
    {
        get => GetPropertyValue<HashSet<string>>() ?? new HashSet<string>();
        set => SetPropertyValue(value);
    }

    public EventCallback InitialLoadRequested
    {
        get => GetPropertyValue<EventCallback>();
        set => SetPropertyValue(value);
    }

    public EventCallback<OfficerShellPage> NavigateRequested
    {
        get => GetPropertyValue<EventCallback<OfficerShellPage>>();
        set => SetPropertyValue(value);
    }

    public EventCallback<Guid> OpenCaseRequested
    {
        get => GetPropertyValue<EventCallback<Guid>>();
        set => SetPropertyValue(value);
    }

    public EventCallback StartProcessRequested
    {
        get => GetPropertyValue<EventCallback>();
        set => SetPropertyValue(value);
    }

    public EventCallback<Guid> ToggleStagedSelectionRequested
    {
        get => GetPropertyValue<EventCallback<Guid>>();
        set => SetPropertyValue(value);
    }

    public EventCallback<string> StagedViewModeChanged
    {
        get => GetPropertyValue<EventCallback<string>>();
        set => SetPropertyValue(value);
    }

    public EventCallback<string> InProcessViewModeChanged
    {
        get => GetPropertyValue<EventCallback<string>>();
        set => SetPropertyValue(value);
    }

    public EventCallback<string> SearchTextChanged
    {
        get => GetPropertyValue<EventCallback<string>>();
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

    public EventCallback ConfigureProfileRequested
    {
        get => GetPropertyValue<EventCallback>();
        set => SetPropertyValue(value);
    }

    public EventCallback<string> CatalogSearchTextChanged
    {
        get => GetPropertyValue<EventCallback<string>>();
        set => SetPropertyValue(value);
    }

    public EventCallback<string> StagedFamilyFilterChanged
    {
        get => GetPropertyValue<EventCallback<string>>();
        set => SetPropertyValue(value);
    }

    public EventCallback<string> InProcessFamilyFilterChanged
    {
        get => GetPropertyValue<EventCallback<string>>();
        set => SetPropertyValue(value);
    }

    public EventCallback<int> StagedPageChanged
    {
        get => GetPropertyValue<EventCallback<int>>();
        set => SetPropertyValue(value);
    }

    public EventCallback<int> StagedPageSizeChanged
    {
        get => GetPropertyValue<EventCallback<int>>();
        set => SetPropertyValue(value);
    }

    public EventCallback<int> InProcessPageChanged
    {
        get => GetPropertyValue<EventCallback<int>>();
        set => SetPropertyValue(value);
    }

    public EventCallback<int> InProcessPageSizeChanged
    {
        get => GetPropertyValue<EventCallback<int>>();
        set => SetPropertyValue(value);
    }

    public EventCallback<string> ToggleStagedGroupCollapsed
    {
        get => GetPropertyValue<EventCallback<string>>();
        set => SetPropertyValue(value);
    }

    public EventCallback<string> TemplatesViewModeChanged
    {
        get => GetPropertyValue<EventCallback<string>>();
        set => SetPropertyValue(value);
    }

    public EventCallback<string> TemplatesFamilyFilterChanged
    {
        get => GetPropertyValue<EventCallback<string>>();
        set => SetPropertyValue(value);
    }

    public EventCallback<int> TemplatesPageChanged
    {
        get => GetPropertyValue<EventCallback<int>>();
        set => SetPropertyValue(value);
    }

    public EventCallback<int> TemplatesPageSizeChanged
    {
        get => GetPropertyValue<EventCallback<int>>();
        set => SetPropertyValue(value);
    }

    public EventCallback<Guid> OpenTemplateDetailRequested
    {
        get => GetPropertyValue<EventCallback<Guid>>();
        set => SetPropertyValue(value);
    }

    public EventCallback BackToTemplateCatalogRequested
    {
        get => GetPropertyValue<EventCallback>();
        set => SetPropertyValue(value);
    }

    public EventCallback<Guid> ConfigureTemplateRequested
    {
        get => GetPropertyValue<EventCallback<Guid>>();
        set => SetPropertyValue(value);
    }

    public EventCallback<string> CaseTabChanged
    {
        get => GetPropertyValue<EventCallback<string>>();
        set => SetPropertyValue(value);
    }

    public EventCallback BackToInProcessRequested
    {
        get => GetPropertyValue<EventCallback>();
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

    public EventCallback OpenResminamalarRequested
    {
        get => GetPropertyValue<EventCallback>();
        set => SetPropertyValue(value);
    }

    public EventCallback<int> SelectPersonRowRequested
    {
        get => GetPropertyValue<EventCallback<int>>();
        set => SetPropertyValue(value);
    }

    public EventCallback<int> OpenPersonDetailByIndexRequested
    {
        get => GetPropertyValue<EventCallback<int>>();
        set => SetPropertyValue(value);
    }

    public EventCallback AdvanceProgressRequested
    {
        get => GetPropertyValue<EventCallback>();
        set => SetPropertyValue(value);
    }
}
