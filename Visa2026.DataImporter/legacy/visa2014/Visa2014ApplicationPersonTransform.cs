using System.Data;
using Microsoft.Data.SqlClient;

namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// Thin PersonInApplication → ApplicationProfileInstancePerson roster rows (Wave 2b).
/// Reuses ApplicationItem skip/dedupe rules for parent ApplicationType + Application+Person uniqueness.
/// </summary>
internal sealed record Visa2014ApplicationProfileInstancePersonRawRow(
    Guid LegacyOid,
    Guid LegacyApplicationProfileInstanceOid,
    Guid? LegacyEmployeeOid,
    Guid? LegacyFamilyMemberOid,
    bool ForEmployee,
    bool ForFamilyMember,
    int? EmployeeSubtypeId,
    int? FamilySubtypeId,
    bool HasInvitationWpFk,
    int? InvitationAndWorkPermitRequired,
    bool HasWizaWpFk,
    int? WizaAndWorkPermitRequired,
    int? ChangeInformation);

internal static class Visa2014ApplicationProfileInstancePersonTransform
{
    internal static readonly string ExtractSql = """
        SELECT
            CAST(pia.Oid AS varchar(36)) AS Oid,
            CAST(pia.Application AS varchar(36)) AS ApplicationProfileInstanceOid,
            CAST(pia.Employee AS varchar(36)) AS EmployeeOid,
            CAST(pia.FamilyMember AS varchar(36)) AS FamilyMemberOid,
            CASE WHEN ISNULL(a.ForEmployee, 0) = 1 THEN '1' ELSE '0' END AS ForEmployee,
            CASE WHEN ISNULL(a.ForFamilyMember, 0) = 1 THEN '1' ELSE '0' END AS ForFamilyMember,
            ate.TypeOfApplicationForEmployee AS EmployeeSubtypeId,
            atfm.TypeOfApplicationForFamilyMember AS FamilySubtypeId,
            CASE WHEN a.IsInvitationWithWorkPermit IS NULL THEN '0' ELSE '1' END AS HasInvitationWpFk,
            iwp.InvitationAndWorkPermitRequired,
            CASE WHEN a.IsWizaWithWorkPermit IS NULL THEN '0' ELSE '1' END AS HasWizaWpFk,
            wwp.WizaAndWorkPermitRequired,
            a.ChangeInformation
        FROM dbo.PersonInApplication pia
        INNER JOIN dbo.Application a ON a.Oid = pia.Application AND a.GCRecord IS NULL
        LEFT JOIN dbo.ApplicationTypeForEmployee ate ON ate.Oid = a.ApplicationTypeForEmployee
        LEFT JOIN dbo.ApplicationTypeForFamilyMember atfm ON atfm.Oid = a.ApplicationTypeForFamilyMember
        LEFT JOIN dbo.IsInvitationWithWorkPermit iwp ON iwp.Oid = a.IsInvitationWithWorkPermit
        LEFT JOIN dbo.IsWizaWithWorkPermit wwp ON wwp.Oid = a.IsWizaWithWorkPermit
        WHERE pia.GCRecord IS NULL
        ORDER BY pia.Oid
        """;

    public static IReadOnlyList<Visa2014ApplicationProfileInstancePersonRawRow> LoadRawRows(
        string legacyConnectionString,
        int? maxRows,
        bool verbose)
    {
        var rows = new List<Visa2014ApplicationProfileInstancePersonRawRow>();
        using var connection = new SqlConnection(legacyConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = ExtractSql;
        command.CommandTimeout = 0;
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (!TryParseRaw(reader, out var raw) || raw == null)
                continue;
            rows.Add(raw);
            if (maxRows is int max && rows.Count >= max)
                break;
        }

        if (verbose)
            Console.WriteLine($"INF ApplicationProfileInstancePerson extract: {rows.Count} PersonInApplication row(s)");
        return rows;
    }

    public static Visa2014PersonImportBatch Transform(
        IReadOnlyList<Visa2014ApplicationProfileInstancePersonRawRow> rawRows,
        out List<Dictionary<string, object?>> skipped,
        out List<Dictionary<string, object?>> dedupeSummary)
    {
        skipped = [];
        dedupeSummary = [];
        var working = rawRows.Select(r => new WorkingRow(r)).ToList();
        ApplyDedupe(working, dedupeSummary);

        var importRows = new List<Dictionary<string, object?>>();
        foreach (var row in working)
        {
            var export = BuildExportRow(row, out var skipReason);
            if (skipReason != null)
            {
                export["_reason"] = skipReason;
                skipped.Add(export);
                continue;
            }

            importRows.Add(export);
        }

        return new Visa2014PersonImportBatch
        {
            ImportRows = importRows,
            Skipped = skipped,
            UnmappedLookups = [],
            DedupeSummary = dedupeSummary,
            LegacyRowCount = rawRows.Count,
            DedupeMergedCount = 0,
        };
    }

    public static Guid? ResolvePersonOid(Visa2014ApplicationProfileInstancePersonRawRow raw) =>
        raw.ForEmployee ? raw.LegacyEmployeeOid
        : raw.ForFamilyMember ? raw.LegacyFamilyMemberOid
        : raw.LegacyEmployeeOid ?? raw.LegacyFamilyMemberOid;

    private static bool TryParseRaw(IDataRecord reader, out Visa2014ApplicationProfileInstancePersonRawRow? raw)
    {
        raw = null;
        if (!Guid.TryParse(GetString(reader, "Oid"), out var oid))
            return false;
        if (!Guid.TryParse(GetString(reader, "ApplicationProfileInstanceOid"), out var applicationOid))
            return false;

        raw = new Visa2014ApplicationProfileInstancePersonRawRow(
            LegacyOid: oid,
            LegacyApplicationProfileInstanceOid: applicationOid,
            LegacyEmployeeOid: ParseGuid(GetString(reader, "EmployeeOid")),
            LegacyFamilyMemberOid: ParseGuid(GetString(reader, "FamilyMemberOid")),
            ForEmployee: GetString(reader, "ForEmployee") == "1",
            ForFamilyMember: GetString(reader, "ForFamilyMember") == "1",
            EmployeeSubtypeId: ParseNullableInt(GetString(reader, "EmployeeSubtypeId")),
            FamilySubtypeId: ParseNullableInt(GetString(reader, "FamilySubtypeId")),
            HasInvitationWpFk: GetString(reader, "HasInvitationWpFk") == "1",
            InvitationAndWorkPermitRequired: ParseNullableInt(GetString(reader, "InvitationAndWorkPermitRequired")),
            HasWizaWpFk: GetString(reader, "HasWizaWpFk") == "1",
            WizaAndWorkPermitRequired: ParseNullableInt(GetString(reader, "WizaAndWorkPermitRequired")),
            ChangeInformation: ParseNullableInt(GetString(reader, "ChangeInformation")));
        return true;
    }

    private sealed class WorkingRow(Visa2014ApplicationProfileInstancePersonRawRow Raw)
    {
        public Visa2014ApplicationProfileInstancePersonRawRow Raw { get; } = Raw;
        public string ImportAction { get; set; } = "import";
        public string? DedupeGroupId { get; set; }
    }

    private static void ApplyDedupe(List<WorkingRow> rows, List<Dictionary<string, object?>> dedupeSummary)
    {
        var groups = rows
            .Select(r => new { Row = r, PersonOid = ResolvePersonOid(r.Raw) })
            .Where(x => x.PersonOid.HasValue)
            .GroupBy(x => (x.Row.Raw.LegacyApplicationProfileInstanceOid, x.PersonOid!.Value))
            .Where(g => g.Count() > 1);

        foreach (var group in groups)
        {
            var members = group.ToList();
            var canonical = members.OrderBy(x => x.Row.Raw.LegacyOid).First();
            var groupId = $"APP:{group.Key.LegacyApplicationProfileInstanceOid:D}:PERSON:{group.Key.Item2:D}";
            foreach (var member in members)
            {
                member.Row.DedupeGroupId = groupId;
                if (!ReferenceEquals(member.Row, canonical.Row))
                    member.Row.ImportAction = "dedupe_skip";
            }

            dedupeSummary.Add(new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["_dedupeGroupId"] = groupId,
                ["key"] = "Application+Person",
                ["normalizedValue"] = $"{group.Key.LegacyApplicationProfileInstanceOid:D}|{group.Key.Item2:D}",
                ["memberCount"] = members.Count,
                ["canonical_legacyRowId"] = canonical.Row.Raw.LegacyOid,
                ["canonicalRule"] = "lowest_legacy_oid",
            });
        }
    }

    private static Dictionary<string, object?> BuildExportRow(WorkingRow working, out string? skipReason)
    {
        skipReason = null;
        var raw = working.Raw;
        var applicationTypeComposite = Visa2014ApplicationTransform.BuildApplicationTypeComposite(
            raw.ForEmployee,
            raw.ForFamilyMember,
            raw.EmployeeSubtypeId,
            raw.FamilySubtypeId,
            raw.HasInvitationWpFk,
            raw.InvitationAndWorkPermitRequired,
            raw.HasWizaWpFk,
            raw.WizaAndWorkPermitRequired,
            raw.ChangeInformation);

        var personOid = ResolvePersonOid(raw);
        var row = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["_legacyRowId"] = raw.LegacyOid,
            ["_legacyTable"] = "PersonInApplication",
            ["_dedupeGroupId"] = working.DedupeGroupId ?? "",
            ["_importAction"] = working.ImportAction == "dedupe_skip" ? "skip" : "import",
            ["_legacy_ApplicationTypeComposite"] = applicationTypeComposite,
            ["Application"] = raw.LegacyApplicationProfileInstanceOid.ToString("D"),
            ["_personLegacyOid"] = personOid?.ToString("D"),
        };

        if (working.ImportAction == "dedupe_skip")
        {
            skipReason = "dedupe_duplicate";
            return row;
        }

        if (Visa2014ApplicationTransform.IsSkippedApplicationTypeComposite(applicationTypeComposite))
        {
            skipReason = $"skip_row:parent_ApplicationType:{applicationTypeComposite}";
            return row;
        }

        if (!personOid.HasValue)
        {
            skipReason = "missing_person";
            return row;
        }

        row["Person"] = personOid.Value.ToString("D");
        return row;
    }

    private static string? GetString(IDataRecord reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : Convert.ToString(reader.GetValue(ordinal));
    }

    private static Guid? ParseGuid(string? text) =>
        Guid.TryParse(text, out var g) ? g : null;

    private static int? ParseNullableInt(string? text) =>
        int.TryParse(text, out var n) ? n : null;
}