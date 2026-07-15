using System;
using System.Collections.Generic;
using DevExpress.ExpressApp.Blazor.Components.Models;
using Microsoft.AspNetCore.Components;
using Visa2026.Module.Services.ReportDashboard;

namespace Visa2026.Blazor.Server.Editors;

public sealed class ReportDashboardModel : ComponentModelBase
{
    public override Type ComponentType => typeof(ReportDashboardComponent);

    public ReportDashboardPersonType PersonType
    {
        get => GetPropertyValue<ReportDashboardPersonType>();
        set => SetPropertyValue(value);
    }
    public ReportDashboardCategory Category
    {
        get => GetPropertyValue<ReportDashboardCategory>();
        set => SetPropertyValue(value);
    }
    public string SubReport
    {
        get => GetPropertyValue<string>() is { Length: > 0 } s ? s : "default";
        set => SetPropertyValue(value);
    }
    public string ProjectKey
    {
        get => GetPropertyValue<string>() is { Length: > 0 } s ? s : "All";
        set => SetPropertyValue(value);
    }
    public string ChartView
    {
        get => GetPropertyValue<string>() is { Length: > 0 } s ? s : "pie";
        set => SetPropertyValue(value);
    }
    public int DateRangeMonths
    {
        get => GetPropertyValue<int>() is > 0 and var m ? m : 6;
        set => SetPropertyValue(value);
    }
    public bool ShowAllView
    {
        get => GetPropertyValue<bool>();
        set => SetPropertyValue(value);
    }
    /// <summary>Passport only: when false (default), exclude Person.IsArchived.</summary>
    public bool IncludeArchivedPersons
    {
        get => GetPropertyValue<bool>();
        set => SetPropertyValue(value);
    }
    public ReportDashboardPanelData? Panel
    {
        get => GetPropertyValue<ReportDashboardPanelData?>();
        set => SetPropertyValue(value);
    }
    public IReadOnlyDictionary<ReportDashboardCategory, ReportDashboardPanelData>? AllPanels
    {
        get => GetPropertyValue<IReadOnlyDictionary<ReportDashboardCategory, ReportDashboardPanelData>?>();
        set => SetPropertyValue(value);
    }
    public ReportDashboardSnapshot? Snapshot
    {
        get => GetPropertyValue<ReportDashboardSnapshot?>();
        set => SetPropertyValue(value);
    }
    public EventCallback OpenExcelRequested
    {
        get => GetPropertyValue<EventCallback>();
        set => SetPropertyValue(value);
    }
    public EventCallback<string?> OpenListViewRequested
    {
        get => GetPropertyValue<EventCallback<string?>>();
        set => SetPropertyValue(value);
    }
    public EventCallback<int> DateRangeChanged
    {
        get => GetPropertyValue<EventCallback<int>>();
        set => SetPropertyValue(value);
    }
    public EventCallback<ReportDashboardPersonType> PersonTypeChanged
    {
        get => GetPropertyValue<EventCallback<ReportDashboardPersonType>>();
        set => SetPropertyValue(value);
    }
    public EventCallback<ReportDashboardCategory> CategoryChanged
    {
        get => GetPropertyValue<EventCallback<ReportDashboardCategory>>();
        set => SetPropertyValue(value);
    }
    public EventCallback<string> SubReportChanged
    {
        get => GetPropertyValue<EventCallback<string>>();
        set => SetPropertyValue(value);
    }
    public EventCallback<string> ProjectKeyChanged
    {
        get => GetPropertyValue<EventCallback<string>>();
        set => SetPropertyValue(value);
    }
    public EventCallback<string> ChartViewChanged
    {
        get => GetPropertyValue<EventCallback<string>>();
        set => SetPropertyValue(value);
    }
    public EventCallback<bool> ShowAllViewChanged
    {
        get => GetPropertyValue<EventCallback<bool>>();
        set => SetPropertyValue(value);
    }
    public EventCallback<bool> IncludeArchivedPersonsChanged
    {
        get => GetPropertyValue<EventCallback<bool>>();
        set => SetPropertyValue(value);
    }
}