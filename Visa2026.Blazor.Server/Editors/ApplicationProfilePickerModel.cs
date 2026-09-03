#nullable enable
using System;
using System.Collections.Generic;
using DevExpress.ExpressApp.Blazor.Components.Models;
using Microsoft.AspNetCore.Components;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationWorkspace;

namespace Visa2026.Blazor.Server.Editors;

public sealed class ApplicationProfilePickerModel : ComponentModelBase
{
    public override Type ComponentType => typeof(ApplicationProfilePickerComponent);

    public bool IsLoading
    {
        get => GetPropertyValue<bool>();
        set => SetPropertyValue(value);
    }

    public int Step
    {
        get => GetPropertyValue<int>();
        set => SetPropertyValue(value);
    }

    public IReadOnlyList<PickerRowModel> Rows
    {
        get => GetPropertyValue<IReadOnlyList<PickerRowModel>>() ?? Array.Empty<PickerRowModel>();
        set => SetPropertyValue(value);
    }

    public Guid SelectedProfileId
    {
        get => GetPropertyValue<Guid>();
        set => SetPropertyValue(value);
    }

    public Guid SelectedVersionId
    {
        get => GetPropertyValue<Guid>();
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

    public bool IsStatusWarning
    {
        get => GetPropertyValue<bool>();
        set => SetPropertyValue(value);
    }

    public string? RouteHint
    {
        get => GetPropertyValue<string?>();
        set => SetPropertyValue(value);
    }

    public EventCallback InitialLoadRequested
    {
        get => GetPropertyValue<EventCallback>();
        set => SetPropertyValue(value);
    }

    public EventCallback UseProfileRequested
    {
        get => GetPropertyValue<EventCallback>();
        set => SetPropertyValue(value);
    }

    public EventCallback NextStepRequested
    {
        get => GetPropertyValue<EventCallback>();
        set => SetPropertyValue(value);
    }

    public EventCallback BackStepRequested
    {
        get => GetPropertyValue<EventCallback>();
        set => SetPropertyValue(value);
    }

    public EventCallback<Guid> SelectProfileRequested
    {
        get => GetPropertyValue<EventCallback<Guid>>();
        set => SetPropertyValue(value);
    }

    public EventCallback<Guid> SelectVersionRequested
    {
        get => GetPropertyValue<EventCallback<Guid>>();
        set => SetPropertyValue(value);
    }

    public EventCallback NewApprovalLegRequested
    {
        get => GetPropertyValue<EventCallback>();
        set => SetPropertyValue(value);
    }

    public EventCallback<Guid> OpenApprovalLegRequested
    {
        get => GetPropertyValue<EventCallback<Guid>>();
        set => SetPropertyValue(value);
    }

    public EventCallback OpenApprovalLegCatalogRequested
    {
        get => GetPropertyValue<EventCallback>();
        set => SetPropertyValue(value);
    }

    public EventCallback<Guid> MakeDefaultApprovalLegRequested
    {
        get => GetPropertyValue<EventCallback<Guid>>();
        set => SetPropertyValue(value);
    }

    public IReadOnlyList<OrganizationCatalogOption> Companies
    {
        get => GetPropertyValue<IReadOnlyList<OrganizationCatalogOption>>() ?? Array.Empty<OrganizationCatalogOption>();
        set => SetPropertyValue(value);
    }

    public IReadOnlyList<OrganizationCatalogOption> Signatories
    {
        get => GetPropertyValue<IReadOnlyList<OrganizationCatalogOption>>() ?? Array.Empty<OrganizationCatalogOption>();
        set => SetPropertyValue(value);
    }

    public IReadOnlyList<OrganizationCatalogOption> Representatives
    {
        get => GetPropertyValue<IReadOnlyList<OrganizationCatalogOption>>() ?? Array.Empty<OrganizationCatalogOption>();
        set => SetPropertyValue(value);
    }

    public Guid SelectedCompanyId
    {
        get => GetPropertyValue<Guid>();
        set => SetPropertyValue(value);
    }

    public Guid SelectedSignatoryId
    {
        get => GetPropertyValue<Guid>();
        set => SetPropertyValue(value);
    }

    public Guid SelectedRepresentativeId
    {
        get => GetPropertyValue<Guid>();
        set => SetPropertyValue(value);
    }

    public EventCallback<ApplicationWorkspaceOrganizationLetterheadUpdate> OrganizationChanged
    {
        get => GetPropertyValue<EventCallback<ApplicationWorkspaceOrganizationLetterheadUpdate>>();
        set => SetPropertyValue(value);
    }

    public EventCallback<ApplicationWorkspaceOrganizationLetterheadUpdate> MakeDefaultOrganizationRequested
    {
        get => GetPropertyValue<EventCallback<ApplicationWorkspaceOrganizationLetterheadUpdate>>();
        set => SetPropertyValue(value);
    }

    public EventCallback<(string Kind, Guid Id)> OrganizationCatalogEditorRequested
    {
        get => GetPropertyValue<EventCallback<(string Kind, Guid Id)>>();
        set => SetPropertyValue(value);
    }

    public sealed class PickerRowModel
    {
        public Guid ProfileId { get; init; }

        public string Name { get; init; } = string.Empty;

        public string MetaLine { get; init; } = string.Empty;

        public string SeedUsageLine { get; init; } = string.Empty;

        public bool IsConfigLocked { get; init; }

        public bool HasOpenApplicationForSeedPerson { get; init; }

        public bool RequiresApprovalLegVersion { get; init; }

        public bool MissingApprovalLegVersions { get; init; }

        public IReadOnlyList<VersionOptionModel> ApprovalLegVersions { get; init; }
            = Array.Empty<VersionOptionModel>();
    }

    public sealed class VersionOptionModel
    {
        public Guid VersionId { get; init; }

        public string Name { get; init; } = string.Empty;

        public bool IsDefault { get; init; }

        public IReadOnlyList<string> MinistryNames { get; init; } = Array.Empty<string>();
    }
}
