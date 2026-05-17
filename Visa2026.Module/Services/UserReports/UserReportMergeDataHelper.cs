using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.UserReports;

/// <summary>Shared data shaping for Word and Excel user report merge.</summary>
public static class UserReportMergeDataHelper
{
    public static IList<ApplicationItem> GetActiveApplicationItems(Application application) =>
        (application.ApplicationItems ?? Enumerable.Empty<ApplicationItem>())
        .Where(i => i != null && !i.IsDeleted)
        .ToList();

    /// <summary>
    /// Loads every non-deleted item for the application from the database (avoids a partially loaded navigation collection).
    /// </summary>
    public static IList<ApplicationItem> GetActiveApplicationItems(IObjectSpace objectSpace, Application application)
    {
        if (objectSpace == null)
            throw new ArgumentNullException(nameof(objectSpace));
        if (application == null)
            throw new ArgumentNullException(nameof(application));

        var applicationId = application.ID;
        return objectSpace.GetObjectsQuery<ApplicationItem>()
            .Where(i => i.Application != null && i.Application.ID == applicationId && !i.IsDeleted)
            .OrderBy(i => i.ApplicationItemName)
            .ToList();
    }

    public static Dictionary<string, object> BuildApplicationHeaderDictionary(Application application)
    {
        var data = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["FullApplicationNumber"] = application.FullApplicationNumber ?? string.Empty,
            ["ApplicationDateText"] = application.ApplicationDateText ?? string.Empty,
            ["CompanyName"] = application.Company?.Name ?? string.Empty,
        };
        return data;
    }

    /// <summary>Row keys aligned with <c>App_Sanawy_Letter.docx</c> / <c>Sanaw_uzt.docx</c> (14-column sanawy).</summary>
    public static Dictionary<string, object> BuildSanawyRowDictionary(ApplicationItem item, int rowNo)
    {
        return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["RowNo"] = rowNo,
            ["Person_LastName"] = item.Person_LastName ?? string.Empty,
            ["Person_FirstName"] = item.Person_FirstName ?? string.Empty,
            ["Person_DateOfBirthText"] = item.Person_DateOfBirthText ?? string.Empty,
            ["Person_CountryOfBirthTm"] = item.Person_CountryOfBirthTm ?? string.Empty,
            ["Person_BirthPlace"] = item.Person_BirthPlace ?? string.Empty,
            ["Person_GenderTm"] = item.Person_GenderTm ?? string.Empty,
            ["Person_NationalityCode"] = item.Person_NationalityCode ?? string.Empty,
            ["Passport_Number"] = item.Passport_Number ?? string.Empty,
            ["Passport_ExpirationDateText"] = item.Passport_ExpirationDateText ?? string.Empty,
            ["Education_LevelTm"] = item.Education_LevelTm ?? string.Empty,
            ["Education_InstitutionName"] = item.Education_InstitutionName ?? string.Empty,
            ["Education_SpecialtyTm"] = item.Education_SpecialtyTm ?? string.Empty,
            ["Position_PositionTm"] = item.Position_PositionTm ?? string.Empty,
            ["Application_VisaPeriod_NameTm"] = item.Application_VisaPeriod_NameTm ?? string.Empty,
            ["Application_VisaCategory_NameTm"] = item.Application_VisaCategory_NameTm ?? string.Empty,
            ["Address_FullAddress"] = item.Address_FullAddress ?? string.Empty,
            ["Person_ForeignAddress"] = item.Person_ForeignAddress ?? string.Empty,
            ["Application_BorderZoneLocation_NameTm"] = item.Application_BorderZoneLocation_NameTm ?? string.Empty,
        };
    }

    public static List<Dictionary<string, object>> BuildSanawyStyleRows(
        Application application,
        IList<ApplicationItem>? applicationItems = null)
    {
        var items = applicationItems != null && applicationItems.Count > 0
            ? applicationItems.Where(i => i != null && !i.IsDeleted).ToList()
            : GetActiveApplicationItems(application);
        var rows = new List<Dictionary<string, object>>(items.Count);
        for (int i = 0; i < items.Count; i++)
            rows.Add(BuildSanawyRowDictionary(items[i], i + 1));
        return rows;
    }

    /// <summary>True when template row tokens use sanawy / ministry list shape (<c>Person_LastName</c>, <c>RowNo</c>).</summary>
    public static bool TemplateUsesPersonListRowPlaceholders(IEnumerable<UserReportPlaceholder>? placeholders) =>
        placeholders != null
        && placeholders.Any(p =>
            p.IsValid
            && p.PlaceholderKey.Contains("rows.", StringComparison.OrdinalIgnoreCase)
            && (p.PlaceholderKey.Contains("Person_LastName", StringComparison.OrdinalIgnoreCase)
                || p.PlaceholderKey.Contains("RowNo", StringComparison.OrdinalIgnoreCase)));

    /// <summary>True when template row tokens use <c>{{.Person_LastName}}</c> / list columns (Excel ministry seeds).</summary>
    public static bool TemplateUsesDotRowPlaceholders(IEnumerable<string> placeholders) =>
        placeholders.Any(p =>
            p.StartsWith(".", StringComparison.Ordinal)
            && (p.Contains("Person_LastName", StringComparison.OrdinalIgnoreCase)
                || p.Contains("RowNumber", StringComparison.OrdinalIgnoreCase)));

    /// <summary>
    /// Row keys for Excel list templates (<c>433_gurlusyk_uzt.xlsx</c>, <c>433-ek_uzt.xlsx</c>) and Word sanawy lists.
    /// Supports both <c>{{.Property}}</c> and <c>{{ds.rows.Property}}</c>.
    /// </summary>
    public static Dictionary<string, object> BuildExcelItemListRowDictionary(ApplicationItem item, int rowNumber)
    {
        var row = new Dictionary<string, object>(BuildSanawyRowDictionary(item, rowNumber), StringComparer.OrdinalIgnoreCase)
        {
            ["RowNumber"] = rowNumber,
            ["Education_LevelAndInstitutionTm"] = item.Education_LevelAndInstitutionTm ?? string.Empty,
            ["Visa_DurationFrequencyBlock"] = item.Visa_DurationFrequencyBlock ?? string.Empty,
            ["WorkDuty_Description"] = item.WorkDuty_Description ?? string.Empty,
            ["Application_SponsorName"] = item.Application_SponsorName ?? string.Empty,
            ["Person_ForeignAddressWithCountry"] = item.Person_ForeignAddressWithCountry ?? string.Empty,
            ["Visa_Number"] = item.Visa_Number ?? string.Empty,
            ["Visa_StartDateText"] = item.Visa_StartDateText ?? string.Empty,
            ["Visa_ExpirationDateText"] = item.Visa_ExpirationDateText ?? string.Empty,
            ["Visa_CategoryTm"] = item.Visa_CategoryTm ?? string.Empty,
            ["WorkPermit_WorkPermittedLocations"] = item.WorkPermit_WorkPermittedLocations ?? string.Empty,
        };

        return row;
    }

    /// <summary>Row keys aligned with labor-contract Word templates and Contract.docx.</summary>
    public static Dictionary<string, object> BuildItemRowDictionary(ApplicationItem item, int rowNumber)
    {
        return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["RowNumber"] = rowNumber,
            ["Person_FullName"] = item.Person_FullName ?? string.Empty,
            ["Position_PositionTm"] = item.Position_PositionTm ?? string.Empty,
            ["Passport_Number"] = item.Passport_Number ?? string.Empty,
            ["Visa_Number"] = item.Visa_Number ?? string.Empty,
            ["Visa_ExpirationDateText"] = item.Visa_ExpirationDateText ?? string.Empty,
            ["WorkPermit_Number"] = item.WorkPermit_Number ?? string.Empty,
            ["WorkPermit_ExpirationDateText"] = item.WorkPermit_ExpirationDateText ?? string.Empty,
            ["Application_SponsorName"] = item.Application_SponsorName ?? string.Empty,
            ["Application_SponsorSignatory"] = item.Application_SponsorSignatory ?? string.Empty,
            ["Application_CompanyAddress"] = item.Application_CompanyAddress ?? string.Empty,
            ["Contract_StartDateText"] = item.Contract_StartDateText ?? string.Empty,
            ["Contract_ExpirationDateText"] = item.Contract_ExpirationDateText ?? string.Empty,
            ["Contract_PeriodFallbackText"] = item.Contract_PeriodFallbackText ?? string.Empty,
            ["Contract_SalaryText"] = item.Contract_SalaryText ?? string.Empty,
            ["Salary_CurrencyCode"] = item.Salary_CurrencyCode ?? string.Empty,
        };
    }

    public static string StripDocxModelPrefix(string pathFromTemplate)
    {
        if (string.IsNullOrWhiteSpace(pathFromTemplate))
            return pathFromTemplate ?? string.Empty;

        var p = pathFromTemplate.Trim();
        if (p.StartsWith("ds.", StringComparison.OrdinalIgnoreCase) && p.Length > 3)
            return p.Substring(3);

        return p;
    }

    public static object? GetPropertyValue(object? obj, string propertyPath)
    {
        if (obj == null || string.IsNullOrEmpty(propertyPath))
            return null;

        var parts = propertyPath.Split('.');
        object? current = obj;

        foreach (var part in parts)
        {
            if (current == null)
                return null;

            var property = current.GetType().GetProperty(part, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (property == null)
                return null;

            current = property.GetValue(current);
        }

        return current;
    }
}
