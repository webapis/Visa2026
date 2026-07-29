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
        get => GetPropertyValue<int>() is > 0 and var m ? m : ReportDashboardCatalog.DefaultCategoryDateRangeMonths;
        set => SetPropertyValue(value);
    }
    /// <summary>Passport-local Last N months (Application.ApplicationDate on ApplicationItem.CurrentPassport).</summary>
    public int PassportDateRangeMonths
    {
        get => GetPropertyValue<int>() is > 0 and var m ? m : ReportDashboardCatalog.DefaultCategoryDateRangeMonths;
        set => SetPropertyValue(value);
    }
    /// <summary>Position History-local Last N months (Application.ApplicationDate on ApplicationItem.CurrentPositionHistory).</summary>
    public int PositionHistoryDateRangeMonths
    {
        get => GetPropertyValue<int>() is > 0 and var m ? m : ReportDashboardCatalog.DefaultCategoryDateRangeMonths;
        set => SetPropertyValue(value);
    }
    /// <summary>Address of Residence-local Last N months (Application.ApplicationDate on ApplicationItem.CurrentAddressOfResidence).</summary>
    public int AddressOfResidenceDateRangeMonths
    {
        get => GetPropertyValue<int>() is > 0 and var m ? m : ReportDashboardCatalog.DefaultCategoryDateRangeMonths;
        set => SetPropertyValue(value);
    }
    /// <summary>Medical Records-local Last N months (Application.ApplicationDate on ApplicationItem.CurrentMedicalRecord).</summary>
    public int MedicalRecordDateRangeMonths
    {
        get => GetPropertyValue<int>() is > 0 and var m ? m : ReportDashboardCatalog.DefaultCategoryDateRangeMonths;
        set => SetPropertyValue(value);
    }
    public bool ShowAllView
    {
        get => GetPropertyValue<bool>();
        set => SetPropertyValue(value);
    }
    /// <summary>
    /// Work Permit / Education / etc.: when false (default), exclude Person.IsArchived.
    /// </summary>
    public bool IncludeArchivedPersons
    {
        get => GetPropertyValue<bool>();
        set => SetPropertyValue(value);
    }
    /// <summary>
    /// Visa: when true, count one last valid visa per person (latest expiry) on by-* sub-reports.
    /// </summary>
    public bool OneLastValidVisaPerPerson
    {
        get => GetPropertyValue<bool>();
        set => SetPropertyValue(value);
    }
    /// <summary>
    /// Work Permit: when true, count one last valid work permit per person (latest expiry) on By Days Remaining.
    /// </summary>
    public bool OneLastValidWorkPermitPerPerson
    {
        get => GetPropertyValue<bool>();
        set => SetPropertyValue(value);
    }
    /// <summary>
    /// When true (default), include only persons with at least one valid visa.
    /// </summary>
    public bool ValidVisaPersonsOnly
    {
        get => GetPropertyValue<bool>();
        set => SetPropertyValue(value);
    }
    /// <summary>
    /// Application: when false (default), exclude latest progress PROCESS_ISSUED.
    /// </summary>
    public bool IncludeCompletedApplicationProcesses
    {
        get => GetPropertyValue<bool>();
        set => SetPropertyValue(value);
    }
    /// <summary>
    /// Application: when false (default), exclude latest progress PROCESS_CANCELLED.
    /// </summary>
    public bool IncludeCancelledApplicationProcesses
    {
        get => GetPropertyValue<bool>();
        set => SetPropertyValue(value);
    }
    public ReportDashboardPanelData? Panel
    {
        get => GetPropertyValue<ReportDashboardPanelData?>();
        set => SetPropertyValue(value);
    }
    /// <summary>TotalCount per sub-report key for the active category (sub-tab badges).</summary>
    public IReadOnlyDictionary<string, int>? SubReportCounts
    {
        get => GetPropertyValue<IReadOnlyDictionary<string, int>?>();
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
    /// <summary>True while panels/snapshot are loading after a tab or filter change.</summary>
    public bool IsLoading
    {
        get => GetPropertyValue<bool>();
        set => SetPropertyValue(value);
    }
    /// <summary>
    /// Progress while <see cref="IsLoading"/>: 0–100 determinate, or negative for an indeterminate bar
    /// (e.g. opening a ListView).
    /// </summary>
    public int LoadingProgressPercent
    {
        get => GetPropertyValue<int>();
        set => SetPropertyValue(value);
    }
    /// <summary>Short status text shown with the progress bar.</summary>
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
    public EventCallback<int> PassportDateRangeChanged
    {
        get => GetPropertyValue<EventCallback<int>>();
        set => SetPropertyValue(value);
    }
    public EventCallback<int> PositionHistoryDateRangeChanged
    {
        get => GetPropertyValue<EventCallback<int>>();
        set => SetPropertyValue(value);
    }
    public EventCallback<int> AddressOfResidenceDateRangeChanged
    {
        get => GetPropertyValue<EventCallback<int>>();
        set => SetPropertyValue(value);
    }
    public EventCallback<int> MedicalRecordDateRangeChanged
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
    public EventCallback<bool> OneLastValidVisaPerPersonChanged
    {
        get => GetPropertyValue<EventCallback<bool>>();
        set => SetPropertyValue(value);
    }
    public EventCallback<bool> OneLastValidWorkPermitPerPersonChanged
    {
        get => GetPropertyValue<EventCallback<bool>>();
        set => SetPropertyValue(value);
    }
    public EventCallback<bool> ValidVisaPersonsOnlyChanged
    {
        get => GetPropertyValue<EventCallback<bool>>();
        set => SetPropertyValue(value);
    }
    public EventCallback<bool> IncludeCompletedApplicationProcessesChanged
    {
        get => GetPropertyValue<EventCallback<bool>>();
        set => SetPropertyValue(value);
    }
    public EventCallback<bool> IncludeCancelledApplicationProcessesChanged
    {
        get => GetPropertyValue<EventCallback<bool>>();
        set => SetPropertyValue(value);
    }
}