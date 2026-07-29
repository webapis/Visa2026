using System;
using System.Collections.Generic;
using Visa2026.Module.Services.ReportDashboard;

namespace Visa2026.Module.Localization;

/// <summary>
/// Layer A UI strings for the Report Dashboard (chrome, catalog labels, fixed status buckets).
/// Status localization is display-only; English keys remain for ListView criteria.
/// </summary>
public static class ReportDashboardLocalization
{
    private static readonly Dictionary<string, string> StatusExactKeys =
        new(StringComparer.Ordinal)
        {
            ["Valid"] = "ReportDashboard.Status.Valid",
            ["Valid (>90 days)"] = "ReportDashboard.Status.ValidGt90",
            ["Valid (31-90 days)"] = "ReportDashboard.Status.Valid3190",
            ["Expiring"] = "ReportDashboard.Status.Expiring",
            ["Expiring Soon"] = "ReportDashboard.Status.ExpiringSoon",
            ["Expiring (<15 days)"] = "ReportDashboard.Status.ExpiringLt15",
            ["Expiring (<30 days)"] = "ReportDashboard.Status.ExpiringLt30",
            ["Expiring (<60 days)"] = "ReportDashboard.Status.ExpiringLt60",
            ["Expiring (<90 days)"] = "ReportDashboard.Status.ExpiringLt90",
            ["Expired"] = "ReportDashboard.Status.Expired",
            ["Pending"] = "ReportDashboard.Status.Pending",
            ["Unknown"] = "ReportDashboard.Status.Unknown",
            ["Unknown city"] = "ReportDashboard.Status.UnknownCity",
            ["(No project)"] = "ReportDashboard.Status.NoProject",
            ["(No category)"] = "ReportDashboard.Status.NoCategory",
            ["Unassigned"] = "ReportDashboard.Status.Unassigned",
            ["Being Prepared"] = "ReportDashboard.Status.BeingPrepared",
            ["Ended"] = "ReportDashboard.Status.Ended",
            ["Current"] = "ReportDashboard.Status.Current",
            ["< 7 days"] = "ReportDashboard.Status.Lt7Days",
            ["< 14 days"] = "ReportDashboard.Status.Lt14Days",
            ["< 1 month"] = "ReportDashboard.Status.Lt1Month",
            ["< 2 months"] = "ReportDashboard.Status.Lt2Months",
            ["< 3 months"] = "ReportDashboard.Status.Lt3Months",
            ["< 4 months"] = "ReportDashboard.Status.Lt4Months",
            ["< 5 months"] = "ReportDashboard.Status.Lt5Months",
            ["< 6 months"] = "ReportDashboard.Status.Lt6Months",
            ["≥ 6 months"] = "ReportDashboard.Status.Gte6Months",
            ["≥ 3 months"] = "ReportDashboard.Status.Gte3Months",
            ["≥ 1 month"] = "ReportDashboard.Status.Gte1Month",
            ["< 1 week"] = "ReportDashboard.Status.Lt1Week",
            ["< 2 weeks"] = "ReportDashboard.Status.Lt2Weeks",
            ["< 3 weeks"] = "ReportDashboard.Status.Lt3Weeks",
            ["< 4 weeks"] = "ReportDashboard.Status.Lt4Weeks",
            ["< 1 day"] = "ReportDashboard.Status.Lt1Day",
            ["< 2 days"] = "ReportDashboard.Status.Lt2Days",
            ["< 3 days"] = "ReportDashboard.Status.Lt3Days",
            ["< 4 days"] = "ReportDashboard.Status.Lt4Days",
            ["< 5 days"] = "ReportDashboard.Status.Lt5Days",
            ["< 6 days"] = "ReportDashboard.Status.Lt6Days",
            ["< 10 days"] = "ReportDashboard.Status.Lt10Days",
        };

    public static string Get(string key) => VisaUiMessages.Get(key);

    public static string Format(string key, params object[] args) => VisaUiMessages.Format(key, args);

    public static string Category(ReportDashboardCategory category) => category switch
    {
        ReportDashboardCategory.ApplicationViaMinistry =>
            Get("ReportDashboard.Category.ApplicationViaMinistry"),
        ReportDashboardCategory.ApplicationDirectMigration =>
            Get("ReportDashboard.Category.ApplicationDirectMigration"),
        ReportDashboardCategory.VisaExtension => Get("ReportDashboard.Category.Visa"),
        ReportDashboardCategory.Invitation => Get("ReportDashboard.Category.Invitation"),
        ReportDashboardCategory.Registration => Get("ReportDashboard.Category.Registration"),
        ReportDashboardCategory.WorkPermit => Get("ReportDashboard.Category.WorkPermit"),
        ReportDashboardCategory.Travel => Get("ReportDashboard.Category.Travel"),
        ReportDashboardCategory.AddressOfResidence => Get("ReportDashboard.Category.AddressOfResidence"),
        ReportDashboardCategory.BorderZone => Get("ReportDashboard.Category.BorderZone"),
        ReportDashboardCategory.Passport => Get("ReportDashboard.Category.Passport"),
        ReportDashboardCategory.Education => Get("ReportDashboard.Category.Education"),
        ReportDashboardCategory.PositionHistory => Get("ReportDashboard.Category.PositionHistory"),
        ReportDashboardCategory.Subcontractor => Get("ReportDashboard.Category.Subcontractor"),
        ReportDashboardCategory.MedicalRecord => Get("ReportDashboard.Category.MedicalRecords"),
        ReportDashboardCategory.IncompletePersons => Get("ReportDashboard.Category.IncompletePersons"),
        _ => category.ToString()
    };

    public static string PersonType(ReportDashboardPersonType personType) => personType switch
    {
        ReportDashboardPersonType.All => Get("ReportDashboard.PersonType.All"),
        ReportDashboardPersonType.Employees => Get("ReportDashboard.PersonType.Employees"),
        ReportDashboardPersonType.FamilyMembers => Get("ReportDashboard.PersonType.FamilyMembers"),
        ReportDashboardPersonType.TemporaryVisitors => Get("ReportDashboard.PersonType.TemporaryVisitors"),
        _ => personType.ToString()
    };

    public static string SubReport(ReportDashboardCategory category, string? key, string? englishFallback = null)
    {
        if (string.IsNullOrWhiteSpace(key))
            return englishFallback ?? string.Empty;

        if (category == ReportDashboardCategory.AddressOfResidence
            && string.Equals(key, "by-validity", StringComparison.OrdinalIgnoreCase))
        {
            return Get("ReportDashboard.SubReport.by-validity.private-house");
        }

        var specificKey = "ReportDashboard.SubReport." + category + "." + key;
        var localized = Get(specificKey);
        if (!string.Equals(localized, specificKey, StringComparison.Ordinal))
            return localized;

        var messageKey = "ReportDashboard.SubReport." + key;
        localized = Get(messageKey);
        if (!string.Equals(localized, messageKey, StringComparison.Ordinal))
            return localized;

        return englishFallback ?? key;
    }

    public static string Header(string englishHeader) => englishHeader switch
    {
        "Name" => Get("ReportDashboard.Header.Name"),
        "Project" => Get("ReportDashboard.Header.Project"),
        "App #" => Get("ReportDashboard.Header.AppNumber"),
        "App Type" => Get("ReportDashboard.Header.AppType"),
        "Visa Period" => Get("ReportDashboard.Header.VisaPeriod"),
        "App Date" => Get("ReportDashboard.Header.AppDate"),
        "State" => Get("ReportDashboard.Header.State"),
        "Passport #" => Get("ReportDashboard.Header.PassportNumber"),
        "Expiry" => Get("ReportDashboard.Header.Expiry"),
        "Type" => Get("ReportDashboard.Header.Type"),
        "Citizenship" => Get("ReportDashboard.Header.Citizenship"),
        "Visa #" => Get("ReportDashboard.Header.VisaNumber"),
        "Days Remaining" => Get("ReportDashboard.Header.DaysRemaining"),
        "Entry" => Get("ReportDashboard.Header.Entry"),
        "Days Since Entry" => Get("ReportDashboard.Header.DaysSinceEntry"),
        "Process State" => Get("ReportDashboard.Header.ProcessState"),
        "BZ Number" => Get("ReportDashboard.Header.BzNumber"),
        "Valid Until" => Get("ReportDashboard.Header.ValidUntil"),
        "Zone" => Get("ReportDashboard.Header.Zone"),
        "Visa State" => Get("ReportDashboard.Header.VisaState"),
        "Visa Category" => Get("ReportDashboard.Header.VisaCategory"),
        "Visa Type" => Get("ReportDashboard.Header.VisaType"),
        "Visa on extension" => Get("ReportDashboard.Header.VisaOnExtension"),
        "Issued Visa" => Get("ReportDashboard.Header.IssuedVisa"),
        "Period" => Get("ReportDashboard.Header.Period"),
        "Travel Date" => Get("ReportDashboard.Header.TravelDate"),
        "Month" => Get("ReportDashboard.Header.Month"),
        "Address" => Get("ReportDashboard.Header.Address"),
        "Validity" => Get("ReportDashboard.Header.Validity"),
        "Region" => Get("ReportDashboard.Header.Region"),
        "City" => Get("ReportDashboard.Header.City"),
        "Address Type" => Get("ReportDashboard.Header.AddressType"),
        "Region · City" => Get("ReportDashboard.Header.RegionCity"),
        "WP Number" => Get("ReportDashboard.Header.WpNumber"),
        "Status" => Get("ReportDashboard.Header.Status"),
        "Invitation #" => Get("ReportDashboard.Header.InvitationNumber"),
        "Period · Category · Type" => Get("ReportDashboard.Header.PeriodCategoryType"),
        "Period · Category · Type · State" => Get("ReportDashboard.Header.PeriodCategoryTypeState"),
        "Project · Period · Category · Type · State" => Get("ReportDashboard.Header.ProjectPeriodCategoryTypeState"),
        "Project · State" => Get("ReportDashboard.Header.ProjectState"),
        "Application Type · Process State" => Get("ReportDashboard.Header.ApplicationTypeProcessState"),
        "Rejection #" => Get("ReportDashboard.Header.RejectionNumber"),
        "Date" => Get("ReportDashboard.Header.Date"),
        "Issued" => Get("ReportDashboard.Header.Issued"),
        "Institution" => Get("ReportDashboard.Header.Institution"),
        "Grad Year" => Get("ReportDashboard.Header.GradYear"),
        "Level" => Get("ReportDashboard.Header.Level"),
        "Country" => Get("ReportDashboard.Header.Country"),
        "Speciality" => Get("ReportDashboard.Header.Speciality"),
        "Visa Position" => Get("ReportDashboard.Header.VisaPosition"),
        "Start" => Get("ReportDashboard.Header.Start"),
        "Actual Position" => Get("ReportDashboard.Header.ActualPosition"),
        "Role" => Get("ReportDashboard.Header.Role"),
        "Hire Date" => Get("ReportDashboard.Header.HireDate"),
        "Company" => Get("ReportDashboard.Header.Company"),
        "Document #" => Get("ReportDashboard.Header.DocumentNumber"),
        "Person" => Get("ReportDashboard.Header.Person"),
        "Person type" => Get("ReportDashboard.Header.PersonType"),
        "Missing areas" => Get("ReportDashboard.Header.MissingAreas"),
        "Notes" => Get("ReportDashboard.Header.Notes"),
        "Marked" => Get("ReportDashboard.Header.Marked"),
        "Current Expiry" => Get("ReportDashboard.Header.CurrentExpiry"),
        "Requested Until" => Get("ReportDashboard.Header.RequestedUntil"),
        "Issue Date" => Get("ReportDashboard.Header.IssueDate"),
        "Position" => Get("ReportDashboard.Header.Position"),
        "Info" => Get("ReportDashboard.Header.Info"),
        _ => englishHeader
    };

    public static string[] Headers(params string[] englishHeaders)
    {
        var result = new string[englishHeaders.Length];
        for (var i = 0; i < englishHeaders.Length; i++)
            result[i] = Header(englishHeaders[i]);
        return result;
    }

    /// <summary>
    /// Localize a fixed English status/bucket label for display. Unknown labels pass through.
    /// Combined Application Status labels localize only the leading segment when recognized.
    /// </summary>
    public static string Status(string? englishLabel)
    {
        if (string.IsNullOrWhiteSpace(englishLabel))
            return string.Empty;

        var label = englishLabel.Trim();
        if (StatusExactKeys.TryGetValue(label, out var key))
            return Get(key);

        const string sep = " · ";
        var sepIndex = label.IndexOf(sep, StringComparison.Ordinal);
        if (sepIndex > 0)
        {
            var head = label[..sepIndex];
            if (StatusExactKeys.TryGetValue(head, out var headKey))
                return Get(headKey) + label[sepIndex..];
        }

        return label;
    }

    public static string PeriodMonthLabel(int months) => months switch
    {
        12 => Get("ReportDashboard.Chrome.Year1"),
        24 => Get("ReportDashboard.Chrome.Years2"),
        36 => Get("ReportDashboard.Chrome.Years3"),
        _ => Format("ReportDashboard.Chrome.Months", months)
    };

    public static string CategoryDateRangeTitle(ReportDashboardCategory category) => category switch
    {
        ReportDashboardCategory.Passport => Get("ReportDashboard.Chrome.Title.Date.Passport"),
        ReportDashboardCategory.PositionHistory => Get("ReportDashboard.Chrome.Title.Date.Position"),
        ReportDashboardCategory.AddressOfResidence => Get("ReportDashboard.Chrome.Title.Date.Address"),
        ReportDashboardCategory.MedicalRecord => Get("ReportDashboard.Chrome.Title.Date.Medical"),
        _ => Get("ReportDashboard.Chrome.Title.Date.Education")
    };
}