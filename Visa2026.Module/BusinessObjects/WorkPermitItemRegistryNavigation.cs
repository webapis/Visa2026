using System;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Top-level Work Permits nav + Employee registry ListView
/// (<see cref="docs/WORK_PERMIT_EMPLOYEE_LISTVIEW.md"/>).
/// </summary>
public static class WorkPermitItemRegistryNavigation
{
    public const string GroupNavId = "WorkPermits";
    public const string NavItemEmployee = "Employee";

    public const string ListViewEmployeesId = "WorkPermitItem_Registry_Employees_ListView";
    public const string SourceListViewId = "WorkPermitItem_ListView";

    public const string CaptionGroup = "Work Permits";
    public const string CaptionEmployee = "Work Permits (Employee)";

    public const string CaptionGroupTk = "Iş rugsatlary";
    public const string CaptionEmployeeTk = "Iş rugsatlary (Işgär)";

    public const string CaptionGroupTr = "İş İzinleri";
    public const string CaptionEmployeeTr = "İş İzinleri (Çalışan)";

    public const string CaptionGroupRu = "Разрешения на работу";
    public const string CaptionEmployeeRu = "Разрешения на работу (Сотрудник)";

    /// <summary>Issued work-permit lines on an issuing Application Profile Instance.</summary>
    public const string CriteriaIssued =
        "WorkPermit is not null And WorkPermit.ApplicationProfileInstance is not null";

    public const string CriteriaEmployees =
        CriteriaIssued
        + " And Person is not null And Person.PersonRole = ##Enum#Visa2026.Module.BusinessObjects.PersonRecordRole,Employee#";

    public const string ImageName = "BO_Employee";
    public const int GroupNavIndex = 3;

    public static readonly string[] AllListViewIds = [ListViewEmployeesId];

    public static string TargetViewIds => ListViewEmployeesId;

    public static bool IsRegistryListView(string? viewId) =>
        string.Equals(viewId, ListViewEmployeesId, StringComparison.Ordinal);
}