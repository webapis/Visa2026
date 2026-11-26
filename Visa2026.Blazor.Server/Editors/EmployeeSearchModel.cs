#nullable enable
using System;
using System.Collections.Generic;
using DevExpress.ExpressApp.Blazor.Components.Models;
using Microsoft.AspNetCore.Components;
using Visa2026.Module.Services.EmployeeSearch;

namespace Visa2026.Blazor.Server.Editors;

public sealed class EmployeeSearchModel : ComponentModelBase
{
    public override Type ComponentType => typeof(EmployeeSearchComponent);

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

    public bool ShowResults
    {
        get => GetPropertyValue<bool>();
        set => SetPropertyValue(value);
    }

    public int Total
    {
        get => GetPropertyValue<int>();
        set => SetPropertyValue(value);
    }

    public bool Truncated
    {
        get => GetPropertyValue<bool>();
        set => SetPropertyValue(value);
    }

    public IReadOnlyList<EmployeeSearchRow> Rows
    {
        get => GetPropertyValue<IReadOnlyList<EmployeeSearchRow>>() ?? Array.Empty<EmployeeSearchRow>();
        set => SetPropertyValue(value);
    }

    public EmployeeSearchOptions? Options
    {
        get => GetPropertyValue<EmployeeSearchOptions>();
        set => SetPropertyValue(value);
    }

    public EventCallback InitialLoadRequested
    {
        get => GetPropertyValue<EventCallback>();
        set => SetPropertyValue(value);
    }

    public EventCallback<EmployeeSearchCriteria> SearchRequested
    {
        get => GetPropertyValue<EventCallback<EmployeeSearchCriteria>>();
        set => SetPropertyValue(value);
    }

    public EventCallback EditCriteriaRequested
    {
        get => GetPropertyValue<EventCallback>();
        set => SetPropertyValue(value);
    }

    public EventCallback<Guid> OpenDossierRequested
    {
        get => GetPropertyValue<EventCallback<Guid>>();
        set => SetPropertyValue(value);
    }
}
