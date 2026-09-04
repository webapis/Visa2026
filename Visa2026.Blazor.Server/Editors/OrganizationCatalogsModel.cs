#nullable enable
using System;
using System.Collections.Generic;
using DevExpress.ExpressApp.Blazor.Components.Models;
using Microsoft.AspNetCore.Components;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Blazor.Server.Editors;

public sealed class OrganizationCatalogsModel : ComponentModelBase
{
    public override Type ComponentType => typeof(OrganizationCatalogsComponent);

    public IReadOnlyList<OrganizationCatalogRow> CompanyRows
    {
        get => GetPropertyValue<IReadOnlyList<OrganizationCatalogRow>>()
            ?? Array.Empty<OrganizationCatalogRow>();
        set => SetPropertyValue(value);
    }

    public IReadOnlyList<OrganizationCatalogRow> SignatoryRows
    {
        get => GetPropertyValue<IReadOnlyList<OrganizationCatalogRow>>()
            ?? Array.Empty<OrganizationCatalogRow>();
        set => SetPropertyValue(value);
    }

    public IReadOnlyList<OrganizationCatalogRow> RepresentativeRows
    {
        get => GetPropertyValue<IReadOnlyList<OrganizationCatalogRow>>()
            ?? Array.Empty<OrganizationCatalogRow>();
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

    public EventCallback<string> NewRequested
    {
        get => GetPropertyValue<EventCallback<string>>();
        set => SetPropertyValue(value);
    }

    public EventCallback<(string Kind, Guid Id)> EditRequested
    {
        get => GetPropertyValue<EventCallback<(string Kind, Guid Id)>>();
        set => SetPropertyValue(value);
    }

    public EventCallback<(string Kind, Guid Id)> MakeDefaultRequested
    {
        get => GetPropertyValue<EventCallback<(string Kind, Guid Id)>>();
        set => SetPropertyValue(value);
    }
}
