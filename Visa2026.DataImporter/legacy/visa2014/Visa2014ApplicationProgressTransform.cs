using Visa2026.Module.BusinessObjects;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed record Visa2014ApplicationProgressRawRow(
    Guid LegacyApplicationOid,
    string? ManualApplicationNumber,
    DateTime? ManualApplicationDate,
    bool IsLongProcess,
    bool ForEmployee,
    bool ForFamilyMember,
    int? EmployeeSubtypeId,
    int? FamilySubtypeId,
    bool HasInvitationWpFk,
    int? InvitationAndWorkPermitRequired,
    bool HasWizaWpFk,
    int? WizaAndWorkPermitRequired,
    int? ChangeInformation,
    DateTime? DateForwardedToMonistery,
    DateTime? MinisteriesDocumentDate,
    string? MinisteriesDocumentNumber,
    DateTime? DateForwardedToMinConstruction,
    string? DocNumberForwardedToMinConstruction,
    DateTime? ProcessDate,
    string? ProcessNumber,
    bool Cancelled,
    bool Rejected);

internal static class Visa2014ApplicationProgressTransform
{
    private static readonly DateTime LegacyDateThreshold = new(2000, 1, 1);

    internal static readonly string[] ApplicationProgressMainColumnOrder =
    [
        "_legacyRowId", "_legacyApplicationOid", "_syntheticStepKey", "_importAction", "_processKind",
        "Application", "State", "Order", "Date", "Description",
        "_legacy_ManualApplicationNumber", "_legacy_ApplicationTypeComposite",
        "_legacy_ProcessNumber", "_legacy_MinisteriesDocumentNumber",
    ];

    internal static string ExtractSql => """
        SELECT
            CAST(a.Oid AS varchar(36)) AS Oid,
            r.ManualApplicationNumber,
            CONVERT(varchar(10), r.ManualApplicationDate, 23) AS ManualApplicationDate,
            CASE WHEN lpa.Oid IS NOT NULL THEN '1' ELSE '0' END AS IsLongProcess,
            CASE WHEN ISNULL(a.ForEmployee, 0) = 1 THEN '1' ELSE '0' END AS ForEmployee,
            CASE WHEN ISNULL(a.ForFamilyMember, 0) = 1 THEN '1' ELSE '0' END AS ForFamilyMember,
            ate.TypeOfApplicationForEmployee AS EmployeeSubtypeId,
            atfm.TypeOfApplicationForFamilyMember AS FamilySubtypeId,
            CASE WHEN a.IsInvitationWithWorkPermit IS NULL THEN '0' ELSE '1' END AS HasInvitationWpFk,
            iwp.InvitationAndWorkPermitRequired,
            CASE WHEN a.IsWizaWithWorkPermit IS NULL THEN '0' ELSE '1' END AS HasWizaWpFk,
            wwp.WizaAndWorkPermitRequired,
            a.ChangeInformation,
            CONVERT(varchar(10), a.DateForwardedToMonistery, 23) AS DateForwardedToMonistery,
            CONVERT(varchar(10), a.MinisteriesDocumentDate, 23) AS MinisteriesDocumentDate,
            a.MinisteriesDocumentNumber,
            CONVERT(varchar(10), a.DateForwardedToMinConstruction, 23) AS DateForwardedToMinConstruction,
            a.DocNumberForwardedToMinConstruction,
            CONVERT(varchar(10), a.ProcessDate, 23) AS ProcessDate,
            a.ProcessNumber,
            CASE WHEN ISNULL(a.Cancelled, 0) = 1 THEN '1' ELSE '0' END AS Cancelled,
            CASE WHEN ISNULL(a.Rejected, 0) = 1 THEN '1' ELSE '0' END AS Rejected
        FROM dbo.Application a
        INNER JOIN dbo.IRegistration_Data r ON r.Oid = a.IRegistration_Data
        LEFT JOIN dbo.LongProcessApplication lpa ON lpa.Oid = a.Oid
        LEFT JOIN dbo.ApplicationTypeForEmployee ate ON ate.Oid = a.ApplicationTypeForEmployee
        LEFT JOIN dbo.ApplicationTypeForFamilyMember atfm ON atfm.Oid = a.ApplicationTypeForFamilyMember
        LEFT JOIN dbo.IsInvitationWithWorkPermit iwp ON iwp.Oid = a.IsInvitationWithWorkPermit
        LEFT JOIN dbo.IsWizaWithWorkPermit wwp ON wwp.Oid = a.IsWizaWithWorkPermit
        WHERE a.GCRecord IS NULL
        """;

    public static Visa2014PersonImportBatch PrepareImportBatch(
        string connectionString,
        IReadOnlyList<string> lookupTranslationPaths,
        int? maxRows,
        bool verbose,
        IReadOnlyDictionary<Guid, int>? ministryLegCountByLegacyApplicationOid = null)
    {
        _ = lookupTranslationPaths;
        var sql = maxRows is > 0
            ? $"SELECT TOP ({maxRows.Value}) * FROM ({ExtractSql}) AS q"
            : ExtractSql;

        var dictRows = Visa2014SqlCmdReader.Query(connectionString, sql, verbose);
        var rawRows = new List<Visa2014ApplicationProgressRawRow>();
        var parseSkipped = 0;
        foreach (var dict in dictRows)
        {
            if (TryParseRawRow(dict, out var parsed))
                rawRows.Add(parsed);
            else
                parseSkipped++;
        }

        if (verbose && parseSkipped > 0)
            Console.WriteLine($"  Skipped {parseSkipped} sqlcmd row(s) with invalid shape.");

        return TransformRows(rawRows, ministryLegCountByLegacyApplicationOid, out var skipped, out var dedupeSummary);
    }

    internal static bool TryParseRawRow(IReadOnlyDictionary<string, string?> row, out Visa2014ApplicationProgressRawRow parsed)
    {
        parsed = null!;
        if (!row.TryGetValue("Oid", out var oidText) ||
            !Guid.TryParse(oidText?.Trim(), out var legacyOid))
            return false;

        parsed = new Visa2014ApplicationProgressRawRow(
            LegacyApplicationOid: legacyOid,
            ManualApplicationNumber: row.GetValueOrDefault("ManualApplicationNumber"),
            ManualApplicationDate: TryParseDate(row.GetValueOrDefault("ManualApplicationDate")),
            IsLongProcess: row.GetValueOrDefault("IsLongProcess") == "1",
            ForEmployee: row.GetValueOrDefault("ForEmployee") == "1",
            ForFamilyMember: row.GetValueOrDefault("ForFamilyMember") == "1",
            EmployeeSubtypeId: ParseNullableInt(row.GetValueOrDefault("EmployeeSubtypeId")),
            FamilySubtypeId: ParseNullableInt(row.GetValueOrDefault("FamilySubtypeId")),
            HasInvitationWpFk: row.GetValueOrDefault("HasInvitationWpFk") == "1",
            InvitationAndWorkPermitRequired: ParseNullableInt(row.GetValueOrDefault("InvitationAndWorkPermitRequired")),
            HasWizaWpFk: row.GetValueOrDefault("HasWizaWpFk") == "1",
            WizaAndWorkPermitRequired: ParseNullableInt(row.GetValueOrDefault("WizaAndWorkPermitRequired")),
            ChangeInformation: ParseNullableInt(row.GetValueOrDefault("ChangeInformation")),
            DateForwardedToMonistery: TryParseDate(row.GetValueOrDefault("DateForwardedToMonistery")),
            MinisteriesDocumentDate: TryParseDate(row.GetValueOrDefault("MinisteriesDocumentDate")),
            MinisteriesDocumentNumber: row.GetValueOrDefault("MinisteriesDocumentNumber"),
            DateForwardedToMinConstruction: TryParseDate(row.GetValueOrDefault("DateForwardedToMinConstruction")),
            DocNumberForwardedToMinConstruction: row.GetValueOrDefault("DocNumberForwardedToMinConstruction"),
            ProcessDate: TryParseDate(row.GetValueOrDefault("ProcessDate")),
            ProcessNumber: row.GetValueOrDefault("ProcessNumber"),
            Cancelled: row.GetValueOrDefault("Cancelled") == "1",
            Rejected: row.GetValueOrDefault("Rejected") == "1");
        return true;
    }

    private static Visa2014PersonImportBatch TransformRows(
        IReadOnlyList<Visa2014ApplicationProgressRawRow> rawRows,
        IReadOnlyDictionary<Guid, int>? ministryLegCountByLegacyApplicationOid,
        out List<Dictionary<string, object?>> skipped,
        out List<Dictionary<string, object?>> dedupeSummary)
    {
        skipped = [];
        dedupeSummary = [];
        var importRows = new List<Dictionary<string, object?>>();

        foreach (var raw in rawRows)
        {
            var composite = Visa2014ApplicationTransform.BuildApplicationTypeComposite(
                raw.ForEmployee,
                raw.ForFamilyMember,
                raw.EmployeeSubtypeId,
                raw.FamilySubtypeId,
                raw.HasInvitationWpFk,
                raw.InvitationAndWorkPermitRequired,
                raw.HasWizaWpFk,
                raw.WizaAndWorkPermitRequired,
                raw.ChangeInformation);

            if (Visa2014ApplicationTransform.IsSkippedApplicationTypeComposite(composite))
            {
                skipped.Add(BuildParentSkippedRow(raw, composite, "parent_application_type_skipped"));
                continue;
            }

            if (!raw.ManualApplicationDate.HasValue)
            {
                skipped.Add(BuildParentSkippedRow(raw, composite, "required_null:ApplicationDate"));
                continue;
            }

            var ministryLegCount = ResolveMinistryLegCount(raw, ministryLegCountByLegacyApplicationOid);
            var steps = SynthesizeSteps(raw, ministryLegCount);
            if (steps.Count == 0)
            {
                skipped.Add(BuildParentSkippedRow(raw, composite, "no_synthesized_steps"));
                continue;
            }

            dedupeSummary.Add(new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["_legacyApplicationOid"] = raw.LegacyApplicationOid,
                ["_processKind"] = raw.IsLongProcess ? "long" : "simple",
                ["stepCount"] = steps.Count,
                ["ministryLegCount"] = ministryLegCount,
                ["_legacy_ApplicationTypeComposite"] = composite,
                ["_legacy_ManualApplicationNumber"] = raw.ManualApplicationNumber,
            });

            for (var stepIndex = 0; stepIndex < steps.Count; stepIndex++)
            {
                var step = steps[stepIndex];
                importRows.Add(new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["_legacyRowId"] = $"{raw.LegacyApplicationOid:D}:{step.StepCode}",
                    ["_legacyApplicationOid"] = raw.LegacyApplicationOid.ToString("D"),
                    ["_syntheticStepKey"] = $"{raw.LegacyApplicationOid:D}:{step.StepCode}",
                    ["_importAction"] = "import",
                    ["_processKind"] = raw.IsLongProcess ? "long" : "simple",
                    ["Application"] = raw.LegacyApplicationOid.ToString("D"),
                    ["State"] = step.StateCode,
                    ["Order"] = stepIndex + 1,
                    ["Date"] = step.Date.ToString("yyyy-MM-dd"),
                    ["Description"] = step.Description,
                    ["_legacy_ManualApplicationNumber"] = raw.ManualApplicationNumber,
                    ["_legacy_ApplicationTypeComposite"] = composite,
                    ["_legacy_ProcessNumber"] = raw.ProcessNumber,
                    ["_legacy_MinisteriesDocumentNumber"] = raw.MinisteriesDocumentNumber,
                });
            }
        }

        return new Visa2014PersonImportBatch
        {
            LegacyRowCount = rawRows.Count,
            ImportRows = importRows,
            Skipped = skipped,
            UnmappedLookups = [],
            DedupeMergedCount = 0,
            DedupeSummary = dedupeSummary,
        };
    }

    internal sealed record SynthesisStep(string StepCode, string StateCode, DateTime Date, string? Description);

    internal static List<SynthesisStep> SynthesizeSteps(Visa2014ApplicationProgressRawRow raw, int ministryLegCount)
    {
        var steps = new List<SynthesisStep>();
        var appDate = raw.ManualApplicationDate!.Value;

        ministryLegCount = Math.Clamp(ministryLegCount, 0, 5);
        if (ministryLegCount > 0)
        {
            var endDate = ResolveTimelineEndDate(raw, appDate);
            var slotDates = BuildMinistrySlotDates(raw, appDate, endDate, ministryLegCount);
            for (var leg = 1; leg <= ministryLegCount; leg++)
            {
                var approvedDate = slotDates[leg - 1];

                if (leg == 1)
                {
                    var startedDate = IsLegacyDateSet(raw.DateForwardedToMonistery)
                        ? raw.DateForwardedToMonistery!.Value
                        : appDate;
                    if (startedDate > approvedDate)
                        startedDate = approvedDate;

                    steps.Add(new SynthesisStep(
                        "leg_1_started",
                        "1_REVIEW_STARTED",
                        startedDate,
                        null));
                }

                steps.Add(new SynthesisStep(
                    $"leg_{leg}_approved",
                    $"{leg}_REVIEW_APPROVED",
                    approvedDate,
                    BuildLegApprovedDescription(raw, leg)));
            }
        }

        var hasProcessCompletion = IsLegacyDateSet(raw.ProcessDate)
            || !string.IsNullOrWhiteSpace(raw.ProcessNumber);
        var ministryRouteComplete = ministryLegCount > 0 && !raw.Cancelled && !raw.Rejected;
        var shouldAddMigrationStarted = hasProcessCompletion || ministryRouteComplete;
        var shouldAddMigrationIssued = hasProcessCompletion;
        if (shouldAddMigrationStarted)
        {
            var priorDate = steps.Count > 0 ? steps[^1].Date : appDate;
            var issuedDate = raw.ProcessDate ?? priorDate;
            var startedDate = shouldAddMigrationIssued
                ? ResolveMigrationStartedDate(issuedDate, priorDate)
                : ResolveMigrationInProgressDate(priorDate);
            steps.Add(new SynthesisStep(
                "migration_started",
                "PROCESS_STARTED",
                startedDate,
                null));

            if (shouldAddMigrationIssued)
            {
                steps.Add(new SynthesisStep(
                    "migration_issued",
                    "PROCESS_ISSUED",
                    issuedDate,
                    FormatLegacyRef("ProcessNumber", raw.ProcessNumber)));
            }
        }

        if (raw.Cancelled)
        {
            var date = raw.ProcessDate ?? (steps.Count > 0 ? steps[^1].Date : appDate);
            steps.Add(new SynthesisStep(
                "cancelled",
                "PROCESS_CANCELLED",
                date,
                "Legacy Cancelled=1"));
        }

        if (raw.Rejected)
        {
            var date = raw.ProcessDate ?? (steps.Count > 0 ? steps[^1].Date : appDate);
            steps.Add(new SynthesisStep(
                "rejected",
                "PROCESS_REJECTED",
                date,
                "Legacy Rejected=1"));
        }

        return steps
            .OrderBy(s => ApplicationProgressOrderHelper.GetWorkflowSortKey(s.StateCode))
            .ThenBy(s => s.Date)
            .ThenBy(s => StepOrder(s.StepCode))
            .ToList();
    }

    private static int ResolveMinistryLegCount(
        Visa2014ApplicationProgressRawRow raw,
        IReadOnlyDictionary<Guid, int>? ministryLegCountByLegacyApplicationOid)
    {
        if (ministryLegCountByLegacyApplicationOid != null
            && ministryLegCountByLegacyApplicationOid.TryGetValue(raw.LegacyApplicationOid, out var resolved))
            return resolved;

        return raw.IsLongProcess ? 2 : 0;
    }

    private static DateTime ResolveTimelineEndDate(Visa2014ApplicationProgressRawRow raw, DateTime appDate)
    {
        if (IsLegacyDateSet(raw.ProcessDate))
            return raw.ProcessDate!.Value;
        if (IsLegacyDateSet(raw.DateForwardedToMinConstruction))
            return raw.DateForwardedToMinConstruction!.Value;
        if (IsLegacyDateSet(raw.MinisteriesDocumentDate))
            return raw.MinisteriesDocumentDate!.Value;
        if (IsLegacyDateSet(raw.DateForwardedToMonistery))
            return raw.DateForwardedToMonistery!.Value;
        return appDate.AddDays(Math.Max(30, raw.IsLongProcess ? 60 : 30));
    }

    private static DateTime[] BuildMinistrySlotDates(
        Visa2014ApplicationProgressRawRow raw,
        DateTime appDate,
        DateTime endDate,
        int ministryLegCount)
    {
        var slotCount = ministryLegCount;
        var slots = new DateTime?[slotCount];
        AssignKnownLegDates(raw, slots);
        FillInterpolatedSlotDates(appDate, endDate, slots);
        return slots.Select(s => s ?? appDate).ToArray();
    }

    private static void AssignKnownLegDates(Visa2014ApplicationProgressRawRow raw, DateTime?[] slots)
    {
        if (slots.Length == 0)
            return;

        if (IsLegacyDateSet(raw.DateForwardedToMonistery))
            slots[0] = raw.DateForwardedToMonistery;

        if (slots.Length > 1
            && (IsLegacyDateSet(raw.MinisteriesDocumentDate) || !string.IsNullOrWhiteSpace(raw.MinisteriesDocumentNumber)))
        {
            slots[1] = raw.MinisteriesDocumentDate ?? raw.DateForwardedToMonistery;
        }

        if (slots.Length > 2 && IsLegacyDateSet(raw.DateForwardedToMinConstruction))
            slots[2] = raw.DateForwardedToMinConstruction;
    }

    private static void FillInterpolatedSlotDates(DateTime appDate, DateTime endDate, DateTime?[] slots)
    {
        if (slots.Length == 0)
            return;

        if (endDate < appDate)
            endDate = appDate.AddDays(1);

        for (var i = 0; i < slots.Length; i++)
        {
            if (slots[i].HasValue)
                continue;

            var prev = i - 1;
            while (prev >= 0 && !slots[prev].HasValue)
                prev--;

            var next = i + 1;
            while (next < slots.Length && !slots[next].HasValue)
                next++;

            var from = prev >= 0 ? slots[prev]!.Value : appDate;
            var to = next < slots.Length ? slots[next]!.Value : endDate;
            var gapCount = next - prev;
            var position = i - prev;
            slots[i] = InterpolateDate(from, to, position, gapCount);
        }
    }

    private static DateTime InterpolateDate(DateTime from, DateTime to, int position, int gapCount)
    {
        if (gapCount <= 0 || position <= 0)
            return from;
        if (to <= from)
            return from.AddDays(position);

        var fraction = (double)position / gapCount;
        var ticks = from.Ticks + (long)((to.Ticks - from.Ticks) * fraction);
        return new DateTime(ticks);
    }

    private static string? BuildLegApprovedDescription(Visa2014ApplicationProgressRawRow raw, int leg) =>
        leg switch
        {
            1 => FormatLegacyRef("MinisteriesDocumentNumber", raw.MinisteriesDocumentNumber),
            2 => FormatLegacyRef("DocNumberForwardedToMinConstruction", raw.DocNumberForwardedToMinConstruction),
            _ => null,
        };

    private static int StepOrder(string stepCode)
    {
        if (stepCode == "prepare")
            return 0;
        if (stepCode.Equals("leg_1_started", StringComparison.Ordinal))
            return 11;
        if (stepCode.StartsWith("leg_", StringComparison.Ordinal) && stepCode.EndsWith("_approved", StringComparison.Ordinal))
        {
            if (TryParseLegStepCode(stepCode, out var leg))
                return 10 + leg * 2 + (leg == 1 ? 1 : 0);
        }

        return stepCode switch
        {
            "ministry_forward" => 12,
            "ministry_return" => 13,
            "ministry_2_forward" => 14,
            "migration_started" => 999,
            "migration_issued" => 1000,
            "migration_process" => 1000,
            "cancelled" => 1001,
            "rejected" => 1002,
            _ => 99,
        };
    }

    private static bool TryParseLegStepCode(string stepCode, out int leg)
    {
        leg = 0;
        if (!stepCode.StartsWith("leg_", StringComparison.Ordinal))
            return false;

        var underscore = stepCode.IndexOf('_', 4);
        if (underscore <= 4)
            return false;

        return int.TryParse(stepCode.AsSpan(4, underscore - 4), out leg) && leg is >= 1 and <= 5;
    }

    private static Dictionary<string, object?> BuildParentSkippedRow(
        Visa2014ApplicationProgressRawRow raw,
        string composite,
        string reason) =>
        new(StringComparer.Ordinal)
        {
            ["_legacyApplicationOid"] = raw.LegacyApplicationOid.ToString("D"),
            ["_legacy_ManualApplicationNumber"] = raw.ManualApplicationNumber,
            ["_legacy_ApplicationTypeComposite"] = composite,
            ["_reason"] = reason,
            ["_processKind"] = raw.IsLongProcess ? "long" : "simple",
        };

    private static DateTime ResolveMigrationStartedDate(DateTime issuedDate, DateTime priorDate) =>
        issuedDate > priorDate ? priorDate.AddDays(1) : priorDate;

    private static DateTime ResolveMigrationInProgressDate(DateTime priorDate) =>
        priorDate.AddDays(1);

    private static bool IsLegacyDateSet(DateTime? date) =>
        date.HasValue && date.Value >= LegacyDateThreshold;

    private static string? FormatLegacyRef(string label, string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : $"{label}: {value.Trim()}";

    private static DateTime? TryParseDate(string? text) =>
        DateTime.TryParse(text, out var parsed) ? parsed : null;

    private static int? ParseNullableInt(string? text) =>
        int.TryParse(text, out var value) ? value : null;
}
