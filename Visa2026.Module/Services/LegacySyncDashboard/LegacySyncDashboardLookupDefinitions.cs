namespace Visa2026.Module.Services.LegacySyncDashboard;

/// <summary>
/// Prod lookup totals + duplicate-key checks for the legacy sync dashboard (ops only; not legacy sync waves).
/// </summary>
internal static class LegacySyncDashboardLookupDefinitions
{
    internal sealed record LookupRow(
        string BO,
        string TargetCountQuery,
        string GroupsQuery,
        string ExtraRowsQuery,
        string Note = "");

    private const string Alive = "(GCRecord IS NULL OR GCRecord = 0)";

    private static string NameTmKey(string column = "NameTm") =>
        $"LOWER(LTRIM(RTRIM({column})))";

    private static string CountAlive(string table) =>
        $"SELECT COUNT(*) FROM {table} WHERE {Alive}";

    private static LookupRow NameTmDup(string bo, string table, string note) =>
        new(
            bo,
            CountAlive(table),
            GroupsBy(table, NameTmKey(), "NULLIF(LTRIM(RTRIM(NameTm)), '') IS NOT NULL"),
            ExtraBy(table, NameTmKey(), "NULLIF(LTRIM(RTRIM(NameTm)), '') IS NOT NULL"),
            note);

    private static string GroupsBy(string table, string groupExpr, string nonEmptyPred) =>
        "SELECT COUNT(*) FROM (" +
        $"SELECT {groupExpr} FROM {table} WHERE {Alive} AND {nonEmptyPred} " +
        $"GROUP BY {groupExpr} HAVING COUNT(*) > 1) d";

    private static string ExtraBy(string table, string groupExpr, string nonEmptyPred) =>
        "SELECT ISNULL(SUM(cnt - 1), 0) FROM (" +
        $"SELECT COUNT(*) cnt FROM {table} WHERE {Alive} AND {nonEmptyPred} " +
        $"GROUP BY {groupExpr} HAVING COUNT(*) > 1) x";

    /// <summary>High-risk / officer-edited + geography catalogs (same set as OnPremSyncState Get-OnPremLookupRowDefinitions).</summary>
    internal static readonly LookupRow[] Rows =
    [
        NameTmDup("Position", "Positions", "NameTm"),
        NameTmDup("Department", "Departments", "NameTm"),
        NameTmDup("Specialty", "Specialties", "NameTm"),
        NameTmDup("EducationInstitution", "EducationInstitutions", "NameTm"),
        new(
            "Country",
            CountAlive("Countries"),
            GroupsBy(
                "Countries",
                "LOWER(LTRIM(RTRIM(COALESCE(NULLIF(LTRIM(RTRIM(Code)), ''), NameTm))))",
                "NULLIF(LTRIM(RTRIM(COALESCE(NULLIF(LTRIM(RTRIM(Code)), ''), NameTm))), '') IS NOT NULL"),
            ExtraBy(
                "Countries",
                "LOWER(LTRIM(RTRIM(COALESCE(NULLIF(LTRIM(RTRIM(Code)), ''), NameTm))))",
                "NULLIF(LTRIM(RTRIM(COALESCE(NULLIF(LTRIM(RTRIM(Code)), ''), NameTm))), '') IS NOT NULL"),
            "Code else NameTm"),
        NameTmDup("Gender", "Genders", "NameTm"),
        NameTmDup("MaritalStatus", "MaritalStatuses", "NameTm"),
        NameTmDup("Region", "Regions", "NameTm"),
        new(
            "City",
            CountAlive("Cities"),
            GroupsBy(
                "Cities",
                "RegionID, " + NameTmKey(),
                "RegionID IS NOT NULL AND NULLIF(LTRIM(RTRIM(NameTm)), '') IS NOT NULL"),
            ExtraBy(
                "Cities",
                "RegionID, " + NameTmKey(),
                "RegionID IS NOT NULL AND NULLIF(LTRIM(RTRIM(NameTm)), '') IS NOT NULL"),
            "Region+NameTm"),
        new(
            "Lodging",
            CountAlive("Lodgings"),
            GroupsBy(
                "Lodgings",
                "CityID, " + NameTmKey("FullAddress"),
                "CityID IS NOT NULL AND NULLIF(LTRIM(RTRIM(FullAddress)), '') IS NOT NULL"),
            ExtraBy(
                "Lodgings",
                "CityID, " + NameTmKey("FullAddress"),
                "CityID IS NOT NULL AND NULLIF(LTRIM(RTRIM(FullAddress)), '') IS NOT NULL"),
            "City+FullAddress"),
        new(
            "OtherSite",
            CountAlive("OtherSites"),
            GroupsBy(
                "OtherSites",
                "CityID, " + NameTmKey("FullAddress"),
                "CityID IS NOT NULL AND NULLIF(LTRIM(RTRIM(FullAddress)), '') IS NOT NULL"),
            ExtraBy(
                "OtherSites",
                "CityID, " + NameTmKey("FullAddress"),
                "CityID IS NOT NULL AND NULLIF(LTRIM(RTRIM(FullAddress)), '') IS NOT NULL"),
            "City+FullAddress"),
        new(
            "Hotel",
            CountAlive("Hotels"),
            GroupsBy(
                "Hotels",
                "CityID, " + NameTmKey("Name"),
                "CityID IS NOT NULL AND NULLIF(LTRIM(RTRIM(Name)), '') IS NOT NULL"),
            ExtraBy(
                "Hotels",
                "CityID, " + NameTmKey("Name"),
                "CityID IS NOT NULL AND NULLIF(LTRIM(RTRIM(Name)), '') IS NOT NULL"),
            "City+Name"),
        new(
            "Hospital",
            CountAlive("Hospitals"),
            GroupsBy(
                "Hospitals",
                "CityID, " + NameTmKey("Name"),
                "CityID IS NOT NULL AND NULLIF(LTRIM(RTRIM(Name)), '') IS NOT NULL"),
            ExtraBy(
                "Hospitals",
                "CityID, " + NameTmKey("Name"),
                "CityID IS NOT NULL AND NULLIF(LTRIM(RTRIM(Name)), '') IS NOT NULL"),
            "City+Name"),
        NameTmDup("BorderZoneName", "BorderZoneNames", "NameTm"),
        NameTmDup("WorkPermittedLocationName", "WorkPermittedLocationNames", "NameTm"),
        NameTmDup("ApplicationType", "ApplicationTypes", "NameTm"),
        new(
            "ApprovingMinistry",
            CountAlive("ApprovingMinistries"),
            GroupsBy(
                "ApprovingMinistries",
                NameTmKey("ShortNameTm"),
                "NULLIF(LTRIM(RTRIM(ShortNameTm)), '') IS NOT NULL"),
            ExtraBy(
                "ApprovingMinistries",
                NameTmKey("ShortNameTm"),
                "NULLIF(LTRIM(RTRIM(ShortNameTm)), '') IS NOT NULL"),
            "ShortNameTm"),
    ];
}