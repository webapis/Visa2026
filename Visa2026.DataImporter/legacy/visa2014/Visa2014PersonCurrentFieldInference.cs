namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// Resolves legacy keys for person-scoped ApplicationItem current fields at import time.
/// Education still uses latest completed diploma when PIA has no education FK (ApplicationItem path).
/// WorkPermitItem must come from PersonInApplication.WorkPermit only — no latest-N at import.
/// </summary>
internal static class Visa2014PersonCurrentFieldInference
{
    private const string CurrentEducationSql = """
        WITH ranked AS (
            SELECT
                CAST(e.Person AS varchar(36)) AS PersonOid,
                CAST(e.Oid AS varchar(36)) AS EducationOid,
                ROW_NUMBER() OVER (
                    PARTITION BY e.Person
                    ORDER BY
                        ISNULL(YEAR(e.EducationEndDate), -2147483648) DESC,
                        e.Oid DESC
                ) AS rn
            FROM dbo.Education e
            INNER JOIN dbo.Person p ON e.Person = p.Oid AND p.GCRecord IS NULL
            WHERE e.GCRecord IS NULL
        )
        SELECT PersonOid, EducationOid
        FROM ranked
        WHERE rn = 1
        """;

    public static IReadOnlyDictionary<Guid, Guid> BuildCurrentEducationByPerson(
        string connectionString,
        bool verbose)
    {
        var rows = Visa2014SqlCmdReader.Query(connectionString, CurrentEducationSql, verbose);
        var result = new Dictionary<Guid, Guid>();
        foreach (var row in rows)
        {
            if (!Guid.TryParse(row.GetValueOrDefault("PersonOid"), out var personOid) ||
                !Guid.TryParse(row.GetValueOrDefault("EducationOid"), out var educationOid))
                continue;

            result[personOid] = educationOid;
        }

        if (verbose)
            Console.WriteLine($"INF Canonical Education per person: {result.Count}");

        return result;
    }

    private const string CurrentWorkPermitSql = """
        WITH ranked AS (
            SELECT
                CAST(wp.Employee AS varchar(36)) AS PersonOid,
                CAST(wp.Oid AS varchar(36)) AS WorkPermitOid,
                ROW_NUMBER() OVER (
                    PARTITION BY wp.Employee
                    ORDER BY
                        wp.StartDateOfWorkPermit DESC,
                        wp.Oid DESC
                ) AS rn
            FROM dbo.WorkPermit wp
            INNER JOIN dbo.Person p ON wp.Employee = p.Oid AND p.GCRecord IS NULL
            WHERE wp.GCRecord IS NULL
              AND wp.StartDateOfWorkPermit IS NOT NULL
        )
        SELECT PersonOid, WorkPermitOid
        FROM ranked
        WHERE rn = 1
        """;

    public static IReadOnlyDictionary<Guid, Guid> BuildCurrentWorkPermitByPerson(
        string connectionString,
        bool verbose)
    {
        var rows = Visa2014SqlCmdReader.Query(connectionString, CurrentWorkPermitSql, verbose);
        var result = new Dictionary<Guid, Guid>();
        foreach (var row in rows)
        {
            if (!Guid.TryParse(row.GetValueOrDefault("PersonOid"), out var personOid) ||
                !Guid.TryParse(row.GetValueOrDefault("WorkPermitOid"), out var workPermitOid))
                continue;

            result[personOid] = workPermitOid;
        }

        if (verbose)
            Console.WriteLine($"INF Canonical WorkPermit per employee: {result.Count}");

        return result;
    }

    internal static Guid? SelectCurrentWorkPermitOid(IEnumerable<(Guid Oid, DateTime? StartDate)> permits) =>
        permits
            .Where(p => p.StartDate.HasValue && p.StartDate.Value != default)
            .OrderByDescending(p => p.StartDate!.Value.Date)
            .ThenByDescending(p => p.Oid)
            .Select(p => (Guid?)p.Oid)
            .FirstOrDefault();

    public static void TrySetApplicationItemPersonCurrentFields(
        Visa2014ApplicationItemRawRow raw,
        string? applicationTypeName,
        ApplicationTypeVisibilityCatalog visibility,
        IReadOnlyDictionary<Guid, Guid> currentEducationByPerson,
        IReadOnlyDictionary<Guid, Guid> currentWorkPermitByPerson,
        Dictionary<string, object?> row)
    {
        if (!raw.ForEmployee || string.IsNullOrWhiteSpace(applicationTypeName))
            return;

        if (!visibility.TryGetFlags(applicationTypeName, out var flags))
            return;

        if (!raw.LegacyEmployeeOid.HasValue)
            return;

        var personOid = raw.LegacyEmployeeOid.Value;

        if (flags.TryGetValue("ShowCurrentEducation", out var showEducation) && showEducation &&
            flags.TryGetValue("ShowRegistrations", out var showRegistrations) && !showRegistrations &&
            currentEducationByPerson.TryGetValue(personOid, out var educationOid))
        {
            row["CurrentEducation"] = educationOid.ToString("D");
        }

        if (flags.TryGetValue("ShowCurrentSalary", out var showSalary) && showSalary)
            row["CurrentSalary"] = personOid.ToString("D");

        // Import is historical: do not fill CurrentWorkPermitItem from latest-N / PersonCurrentItems
        // when PersonInApplication.WorkPermit is null.
        _ = currentWorkPermitByPerson;
    }
}