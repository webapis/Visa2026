namespace Visa2026.Module.Services.LegacySyncDashboard;

/// <summary>
/// Target-side duplicate business-key checks for the legacy sync dashboard.
/// <c>GroupsQuery</c> counts duplicate-key groups; <c>ExtraRowsQuery</c> counts rows beyond the canonical one per group.
/// </summary>
internal static class LegacySyncDashboardDuplicateDefinitions
{
    internal sealed record DuplicateRow(string BO, string? GroupsQuery, string? ExtraRowsQuery, string Note = "");

    internal static readonly DuplicateRow[] Rows =
    [
        new("ApplicationItem",
            """
            SELECT COUNT(*) FROM (
                SELECT ApplicationID, PersonID FROM ApplicationItems
                WHERE (GCRecord IS NULL OR GCRecord = 0) AND PersonID IS NOT NULL AND ApplicationID IS NOT NULL
                GROUP BY ApplicationID, PersonID HAVING COUNT(*) > 1
            ) d
            """,
            """
            SELECT ISNULL(SUM(cnt - 1), 0) FROM (
                SELECT COUNT(*) cnt FROM ApplicationItems
                WHERE (GCRecord IS NULL OR GCRecord = 0) AND PersonID IS NOT NULL AND ApplicationID IS NOT NULL
                GROUP BY ApplicationID, PersonID HAVING COUNT(*) > 1
            ) x
            """),
        new("AddressOfResidence",
            """
            SELECT COUNT(*) FROM (
                SELECT PersonID, Type, CityID, FullAddress FROM AddressesOfResidence
                WHERE (GCRecord IS NULL OR GCRecord = 0) AND PersonID IS NOT NULL
                GROUP BY PersonID, Type, CityID, FullAddress HAVING COUNT(*) > 1
            ) d
            """,
            """
            SELECT ISNULL(SUM(cnt - 1), 0) FROM (
                SELECT COUNT(*) cnt FROM AddressesOfResidence
                WHERE (GCRecord IS NULL OR GCRecord = 0) AND PersonID IS NOT NULL
                GROUP BY PersonID, Type, CityID, FullAddress HAVING COUNT(*) > 1
            ) x
            """,
            "Person+Type+City+Address"),
        new("Passport",
            """
            SELECT COUNT(*) FROM (
                SELECT PersonID, PassportNumber FROM Passports
                WHERE (GCRecord IS NULL OR GCRecord = 0) AND PersonID IS NOT NULL
                  AND NULLIF(LTRIM(RTRIM(PassportNumber)), '') IS NOT NULL
                GROUP BY PersonID, PassportNumber HAVING COUNT(*) > 1
            ) d
            """,
            """
            SELECT ISNULL(SUM(cnt - 1), 0) FROM (
                SELECT COUNT(*) cnt FROM Passports
                WHERE (GCRecord IS NULL OR GCRecord = 0) AND PersonID IS NOT NULL
                  AND NULLIF(LTRIM(RTRIM(PassportNumber)), '') IS NOT NULL
                GROUP BY PersonID, PassportNumber HAVING COUNT(*) > 1
            ) x
            """,
            "Person+PassportNumber"),
        new("WorkPermitItem",
            """
            SELECT COUNT(*) FROM (
                SELECT WorkPermitID, PersonID FROM WorkPermitItems
                WHERE (GCRecord IS NULL OR GCRecord = 0) AND WorkPermitID IS NOT NULL AND PersonID IS NOT NULL
                GROUP BY WorkPermitID, PersonID HAVING COUNT(*) > 1
            ) d
            """,
            """
            SELECT ISNULL(SUM(cnt - 1), 0) FROM (
                SELECT COUNT(*) cnt FROM WorkPermitItems
                WHERE (GCRecord IS NULL OR GCRecord = 0) AND WorkPermitID IS NOT NULL AND PersonID IS NOT NULL
                GROUP BY WorkPermitID, PersonID HAVING COUNT(*) > 1
            ) x
            """,
            "WorkPermit+Person"),
        new("Person",
            """
            SELECT COUNT(*) FROM (
                SELECT PersonalNumber FROM People
                WHERE (GCRecord IS NULL OR GCRecord = 0)
                  AND NULLIF(LTRIM(RTRIM(PersonalNumber)), '') IS NOT NULL
                  AND PersonalNumber <> '0'
                GROUP BY PersonalNumber HAVING COUNT(*) > 1
            ) d
            """,
            """
            SELECT ISNULL(SUM(cnt - 1), 0) FROM (
                SELECT COUNT(*) cnt FROM People
                WHERE (GCRecord IS NULL OR GCRecord = 0)
                  AND NULLIF(LTRIM(RTRIM(PersonalNumber)), '') IS NOT NULL
                  AND PersonalNumber <> '0'
                GROUP BY PersonalNumber HAVING COUNT(*) > 1
            ) x
            """,
            "excl. PN=0"),
        new("Visa",
            """
            SELECT COUNT(*) FROM (
                SELECT PassportID, VisaNumber FROM Visas
                WHERE (GCRecord IS NULL OR GCRecord = 0) AND PassportID IS NOT NULL
                  AND NULLIF(LTRIM(RTRIM(VisaNumber)), '') IS NOT NULL
                GROUP BY PassportID, VisaNumber HAVING COUNT(*) > 1
            ) d
            """,
            """
            SELECT ISNULL(SUM(cnt - 1), 0) FROM (
                SELECT COUNT(*) cnt FROM Visas
                WHERE (GCRecord IS NULL OR GCRecord = 0) AND PassportID IS NOT NULL
                  AND NULLIF(LTRIM(RTRIM(VisaNumber)), '') IS NOT NULL
                GROUP BY PassportID, VisaNumber HAVING COUNT(*) > 1
            ) x
            """,
            "Passport+VisaNumber"),
        new("EmployeePositionHistory",
            """
            SELECT COUNT(*) FROM (
                SELECT PersonID, StartDate, PositionID FROM EmployeePositionHistories
                WHERE (GCRecord IS NULL OR GCRecord = 0) AND PersonID IS NOT NULL
                GROUP BY PersonID, StartDate, PositionID HAVING COUNT(*) > 1
            ) d
            """,
            """
            SELECT ISNULL(SUM(cnt - 1), 0) FROM (
                SELECT COUNT(*) cnt FROM EmployeePositionHistories
                WHERE (GCRecord IS NULL OR GCRecord = 0) AND PersonID IS NOT NULL
                GROUP BY PersonID, StartDate, PositionID HAVING COUNT(*) > 1
            ) x
            """,
            "Person+StartDate+Position"),
    ];

    internal static DuplicateRow? TryGet(string bo) =>
        Rows.FirstOrDefault(r => string.Equals(r.BO, bo, StringComparison.OrdinalIgnoreCase));
}