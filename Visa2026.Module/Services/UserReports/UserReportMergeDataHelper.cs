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
