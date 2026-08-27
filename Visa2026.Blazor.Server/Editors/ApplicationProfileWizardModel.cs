#nullable enable
using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Components.Models;
using Microsoft.AspNetCore.Components;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationProfileWizard;

namespace Visa2026.Blazor.Server.Editors;

public sealed class ApplicationProfileWizardModel : ComponentModelBase
{
    public override Type ComponentType => typeof(ApplicationProfileWizardComponent);

    public bool IsLoading
    {
        get => GetPropertyValue<bool>();
        set => SetPropertyValue(value);
    }

    public bool IsReadOnly
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

    public EventCallback PublishRequested
    {
        get => GetPropertyValue<EventCallback>();
        set => SetPropertyValue(value);
    }

    public ApplicationProfileWizardOrganizationSnapshot OrganizationSnapshot
    {
        get => GetPropertyValue<ApplicationProfileWizardOrganizationSnapshot>()
            ?? ApplicationProfileWizardOrganizationSnapshot.Empty;
        set => SetPropertyValue(value);
    }

    public EventCallback OpenCompanyRequested
    {
        get => GetPropertyValue<EventCallback>();
        set => SetPropertyValue(value);
    }

    public EventCallback OpenSignatoryRequested
    {
        get => GetPropertyValue<EventCallback>();
        set => SetPropertyValue(value);
    }

    public EventCallback OpenRepresentativeRequested
    {
        get => GetPropertyValue<EventCallback>();
        set => SetPropertyValue(value);
    }

    public EventCallback RefreshOrganizationRequested
    {
        get => GetPropertyValue<EventCallback>();
        set => SetPropertyValue(value);
    }

    public EventCallback OpenSharedApprovalLegCatalogRequested
    {
        get => GetPropertyValue<EventCallback>();
        set => SetPropertyValue(value);
    }

    public EventCallback RefreshSharedApprovalLegsRequested
    {
        get => GetPropertyValue<EventCallback>();
        set => SetPropertyValue(value);
    }

    public int SharedApprovalLegsRevision
    {
        get => GetPropertyValue<int>();
        set => SetPropertyValue(value);
    }

    public ApplicationProfileWizardLookupData Lookups
    {
        get => GetPropertyValue<ApplicationProfileWizardLookupData>()
            ?? ApplicationProfileWizardLookupData.Empty;
        set => SetPropertyValue(value);
    }

    public ApplicationProfile? Profile
    {
        get => GetPropertyValue<ApplicationProfile>();
        set => SetPropertyValue(value);
    }

    public IObjectSpace? ObjectSpace
    {
        get => GetPropertyValue<IObjectSpace>();
        set => SetPropertyValue(value);
    }

    public string? OwnerViewId
    {
        get => GetPropertyValue<string?>();
        set => SetPropertyValue(value);
    }
}
