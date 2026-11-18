using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Security;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.DatabaseUpdate;

namespace Visa2026.Module.Services.ReportDashboard;

/// <summary>
/// Saves and compares Excel-time roster membership for person-row Employees / Family Members sheets.
/// Classifies moves into data changes vs date/filter changes using as-of dates + stored visa window.
/// </summary>
public static class ReportDashboardRosterDiffService
{
    private const int KeepPerFingerprint = 30;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static bool Supports(ReportDashboardCategory category, string? subReport) =>
        ReportDashboardCatalog.SupportsRosterChangeLog(category, subReport);

    public static Guid NewExportId() => Guid.NewGuid();

    public static DateTime TodayAsOfDate() => DateTime.Today;

    public static string FormatExportShortId(Guid exportId) =>
        exportId.ToString("N").Substring(0, 8);

    public static string FormatAsOfDate(DateTime? asOf) =>
        asOf is { } d
            ? d.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)
            : string.Empty;

    public static string AppendExportIdToFileName(string baseNameWithoutExtension, Guid exportId)
    {
        var stem = string.IsNullOrWhiteSpace(baseNameWithoutExtension)
            ? "report"
            : baseNameWithoutExtension.Trim();
        if (stem.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            stem = stem.Substring(0, stem.Length - 5);
        return stem + "_" + FormatExportShortId(exportId) + ".xlsx";
    }

    public static string BuildFingerprint(
        string projectKey,
        ReportDashboardPersonType audience,
        bool excludeNonPositiveDaysRemaining,
        bool oneLastValidVisaPerPerson)
    {
        return string.Join('|',
            string.IsNullOrWhiteSpace(projectKey) ? "All" : projectKey.Trim(),
            audience.ToString(),
            excludeNonPositiveDaysRemaining ? "ex0:1" : "ex0:0",
            oneLastValidVisaPerPerson ? "1visa:1" : "1visa:0");
    }

    public static IReadOnlyList<ReportDashboardRosterMember> MembersFromRows(
        IEnumerable<ReportDashboardPreviewRow> rows,
        DateTime? asOfDate = null,
        bool excludeNonPositiveDaysRemaining = true)
    {
        var asOf = (asOfDate ?? TodayAsOfDate()).Date;
        var list = new List<ReportDashboardRosterMember>();
        var seen = new HashSet<Guid>();
        foreach (var row in rows)
        {
            var id = row.PersonId is { } pid && pid != Guid.Empty
                ? pid
                : row.RecordId is { } rid && rid != Guid.Empty ? rid : Guid.Empty;
            if (id == Guid.Empty || !seen.Add(id))
                continue;

            DateTime? endOn = null;
            if (row.DaysRemainingValue is { } days)
                endOn = asOf.AddDays(days);
            else if (TryParseOfficerDate(row.VisaEndDate, out var parsedEnd))
                endOn = parsedEnd;

            DateTime? startOn = null;
            if (TryParseOfficerDate(row.VisaStartDate, out var parsedStart))
                startOn = parsedStart;

            list.Add(new ReportDashboardRosterMember
            {
                PersonId = id,
                Name = row.Name ?? string.Empty,
                PassportOrKey = FirstNonEmpty(row.ColumnA, row.VisaNumber),
                VisaStartOn = startOn,
                VisaEndOn = endOn
            });
        }

        return list;
    }

    public static ReportDashboardRosterDiff Diff(
        IReadOnlyList<ReportDashboardRosterMember> current,
        ReportDashboardRosterSnapshot? previous,
        DateTime? currentAsOfDate = null,
        bool excludeNonPositiveDaysRemaining = true)
    {
        var currentAsOf = (currentAsOfDate ?? TodayAsOfDate()).Date;
        if (previous == null)
        {
            return new ReportDashboardRosterDiff
            {
                CurrentAsOfDate = currentAsOf,
                CurrentTotalCount = current.Count
            };
        }

        var previousAsOf = previous.AsOfDate is { } aod && aod != default
            ? aod.Date
            : previous.CreatedOnUtc.ToLocalTime().Date;
        var previousMembers = DeserializeMembers(previous.MembersJson);
        var previousById = previousMembers.ToDictionary(m => m.PersonId);
        var currentById = current.ToDictionary(m => m.PersonId);

        var added = current
            .Where(m => !previousById.ContainsKey(m.PersonId))
            .OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var removed = previousMembers
            .Where(m => !currentById.ContainsKey(m.PersonId))
            .OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var dataAdded = new List<ReportDashboardRosterMember>();
        var dateAdded = new List<ReportDashboardRosterMember>();
        var dataRemoved = new List<ReportDashboardRosterMember>();
        var dateRemoved = new List<ReportDashboardRosterMember>();

        var sameDay = previousAsOf == currentAsOf;
        foreach (var person in added)
        {
            if (!sameDay && IsDateFilterAdd(person, previousAsOf, currentAsOf, excludeNonPositiveDaysRemaining))
                dateAdded.Add(person);
            else
                dataAdded.Add(person);
        }

        foreach (var person in removed)
        {
            // Prefer previous snapshot visa window; fall back to current-row dates if re-added somehow.
            var window = person;
            if (!sameDay && IsDateFilterRemove(window, previousAsOf, currentAsOf, excludeNonPositiveDaysRemaining))
                dateRemoved.Add(person);
            else
                dataRemoved.Add(person);
        }

        return new ReportDashboardRosterDiff
        {
            PreviousCreatedOnUtc = previous.CreatedOnUtc,
            PreviousAsOfDate = previousAsOf,
            CurrentAsOfDate = currentAsOf,
            PreviousExportId = previous.ExportId is { } eid && eid != Guid.Empty ? eid : null,
            PreviousFileName = previous.FileName,
            PreviousTotalCount = previous.TotalCount > 0 ? previous.TotalCount : previousMembers.Count,
            CurrentTotalCount = current.Count,
            DataAdded = dataAdded,
            DataRemoved = dataRemoved,
            DateFilterAdded = dateAdded,
            DateFilterRemoved = dateRemoved
        };
    }

    /// <summary>
    /// Qualifies on current as-of but not on previous as-of using the person's visa window
    /// (e.g. visa start entered the window, or end still valid only under the newer as-of).
    /// </summary>
    private static bool IsDateFilterAdd(
        ReportDashboardRosterMember person,
        DateTime previousAsOf,
        DateTime currentAsOf,
        bool excludeNonPositive)
    {
        if (person.VisaEndOn == null && person.VisaStartOn == null)
            return false;
        return !QualifiesOn(person, previousAsOf, excludeNonPositive)
            && QualifiesOn(person, currentAsOf, excludeNonPositive);
    }

    /// <summary>
    /// Was in window on previous as-of, out of window on current as-of (typical: visa expired overnight).
    /// </summary>
    private static bool IsDateFilterRemove(
        ReportDashboardRosterMember person,
        DateTime previousAsOf,
        DateTime currentAsOf,
        bool excludeNonPositive)
    {
        if (person.VisaEndOn == null && person.VisaStartOn == null)
            return false;
        return QualifiesOn(person, previousAsOf, excludeNonPositive)
            && !QualifiesOn(person, currentAsOf, excludeNonPositive);
    }

    private static bool QualifiesOn(
        ReportDashboardRosterMember person,
        DateTime asOf,
        bool excludeNonPositive)
    {
        var day = asOf.Date;
        if (person.VisaStartOn is { } start && start.Date > day)
            return false;
        if (person.VisaEndOn is { } end)
        {
            var endDay = end.Date;
            if (excludeNonPositive)
                return endDay > day;
            return endDay >= day;
        }

        // Start-only: treat as in window once start has begun.
        return person.VisaStartOn is { } s && s.Date <= day;
    }

    public static void EnsureSchema(IObjectSpace objectSpace) =>
        ReportDashboardRosterSnapshotSchemaSql.EnsureFromObjectSpace(objectSpace);

    public static ReportDashboardRosterSnapshot? TryGetLatest(
        IObjectSpace objectSpace,
        ReportDashboardCategory category,
        string subReportKey,
        string fingerprint)
    {
        EnsureSchema(objectSpace);
        return objectSpace.GetObjectsQuery<ReportDashboardRosterSnapshot>()
            .Where(s => s.Category == (int)category
                && s.SubReportKey == subReportKey
                && s.FilterFingerprint == fingerprint)
            .OrderByDescending(s => s.CreatedOnUtc)
            .FirstOrDefault();
    }

    public static ReportDashboardRosterSnapshot Save(
        IObjectSpace objectSpace,
        ReportDashboardCategory category,
        string subReportKey,
        string fingerprint,
        IReadOnlyList<ReportDashboardRosterMember> members,
        string? userName,
        Guid? exportId = null,
        string? fileName = null,
        DateTime? asOfDate = null,
        ReportDashboardRosterDiff? diffVsPrevious = null,
        ReportDashboardRosterSnapshot? previous = null)
    {
        EnsureSchema(objectSpace);
        var snap = objectSpace.CreateObject<ReportDashboardRosterSnapshot>();
        snap.CreatedOnUtc = DateTime.UtcNow;
        snap.AsOfDate = (asOfDate ?? TodayAsOfDate()).Date;
        snap.CreatedByUserName = string.IsNullOrWhiteSpace(userName) ? null : userName.Trim();
        snap.Category = (int)category;
        snap.SubReportKey = subReportKey ?? string.Empty;
        snap.FilterFingerprint = fingerprint ?? string.Empty;
        snap.TotalCount = members.Count;
        snap.ExportId = exportId is { } id && id != Guid.Empty ? id : NewExportId();
        snap.FileName = string.IsNullOrWhiteSpace(fileName) ? null : fileName.Trim();
        snap.MembersJson = SerializeMembers(members);
        snap.LastChangeJson = ResolveLastChangeJson(diffVsPrevious, previous);
        objectSpace.CommitChanges();

        PruneOld(objectSpace, category, snap.SubReportKey, snap.FilterFingerprint);
        return snap;
    }

    /// <summary>
    /// Sticky last membership change for the UI. New moves replace it; unchanged Excel copies it forward.
    /// </summary>
    public static string? ResolveLastChangeJson(
        ReportDashboardRosterDiff? diffVsPrevious,
        ReportDashboardRosterSnapshot? previous)
    {
        if (diffVsPrevious?.HasMembershipMoves == true)
            return SerializeLastChange(diffVsPrevious);
        if (!string.IsNullOrWhiteSpace(previous?.LastChangeJson))
            return previous!.LastChangeJson;
        return null;
    }

    public static string SerializeLastChange(ReportDashboardRosterDiff diff)
    {
        var dto = new LastChangeDto
        {
            PreviousCreatedOnUtc = diff.PreviousCreatedOnUtc,
            PreviousAsOfDate = diff.PreviousAsOfDate,
            CurrentAsOfDate = diff.CurrentAsOfDate,
            PreviousExportId = diff.PreviousExportId,
            PreviousTotalCount = diff.PreviousTotalCount,
            CurrentTotalCount = diff.CurrentTotalCount,
            DataAdded = diff.DataAdded.Select(m => new MemberDto(m)).ToList(),
            DataRemoved = diff.DataRemoved.Select(m => new MemberDto(m)).ToList(),
            DateFilterAdded = diff.DateFilterAdded.Select(m => new MemberDto(m)).ToList(),
            DateFilterRemoved = diff.DateFilterRemoved.Select(m => new MemberDto(m)).ToList()
        };
        return JsonSerializer.Serialize(dto, JsonOptions);
    }

    public static ReportDashboardRosterDiff? TryDeserializeLastChange(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;
        try
        {
            var dto = JsonSerializer.Deserialize<LastChangeDto>(json, JsonOptions);
            if (dto == null)
                return null;
            return new ReportDashboardRosterDiff
            {
                PreviousCreatedOnUtc = dto.PreviousCreatedOnUtc,
                PreviousAsOfDate = dto.PreviousAsOfDate,
                CurrentAsOfDate = dto.CurrentAsOfDate,
                PreviousExportId = dto.PreviousExportId,
                PreviousTotalCount = dto.PreviousTotalCount,
                CurrentTotalCount = dto.CurrentTotalCount,
                DataAdded = dto.DataAdded?.Select(d => d.ToMember()).ToList()
                    ?? new List<ReportDashboardRosterMember>(),
                DataRemoved = dto.DataRemoved?.Select(d => d.ToMember()).ToList()
                    ?? new List<ReportDashboardRosterMember>(),
                DateFilterAdded = dto.DateFilterAdded?.Select(d => d.ToMember()).ToList()
                    ?? new List<ReportDashboardRosterMember>(),
                DateFilterRemoved = dto.DateFilterRemoved?.Select(d => d.ToMember()).ToList()
                    ?? new List<ReportDashboardRosterMember>()
            };
        }
        catch
        {
            return null;
        }
    }

    public static string? CurrentUserName()
    {
        try
        {
            return SecuritySystem.CurrentUserName;
        }
        catch
        {
            return null;
        }
    }

    private static void PruneOld(
        IObjectSpace objectSpace,
        ReportDashboardCategory category,
        string subReportKey,
        string fingerprint)
    {
        var old = objectSpace.GetObjectsQuery<ReportDashboardRosterSnapshot>()
            .Where(s => s.Category == (int)category
                && s.SubReportKey == subReportKey
                && s.FilterFingerprint == fingerprint)
            .OrderByDescending(s => s.CreatedOnUtc)
            .Skip(KeepPerFingerprint)
            .ToList();
        foreach (var s in old)
            objectSpace.Delete(s);
        if (old.Count > 0)
            objectSpace.CommitChanges();
    }

    private static string SerializeMembers(IReadOnlyList<ReportDashboardRosterMember> members) =>
        JsonSerializer.Serialize(
            members.Select(m => new MemberDto(m)).ToList(),
            JsonOptions);

    private static List<ReportDashboardRosterMember> DeserializeMembers(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        try
        {
            var dtos = JsonSerializer.Deserialize<List<MemberDto>>(json, JsonOptions);
            if (dtos == null)
                return [];
            return dtos
                .Where(d => d.Id != Guid.Empty)
                .Select(d => d.ToMember())
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    private static bool TryParseOfficerDate(string? text, out DateTime date)
    {
        date = default;
        if (string.IsNullOrWhiteSpace(text))
            return false;
        return DateTime.TryParseExact(
                   text.Trim(),
                   ["dd.MM.yyyy", "d.M.yyyy", "yyyy-MM-dd"],
                   CultureInfo.InvariantCulture,
                   DateTimeStyles.None,
                   out date)
               || DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out date);
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var v in values)
        {
            if (!string.IsNullOrWhiteSpace(v))
                return v.Trim();
        }

        return string.Empty;
    }

    private sealed class LastChangeDto
    {
        public DateTime? PreviousCreatedOnUtc { get; set; }
        public DateTime? PreviousAsOfDate { get; set; }
        public DateTime? CurrentAsOfDate { get; set; }
        public Guid? PreviousExportId { get; set; }
        public int PreviousTotalCount { get; set; }
        public int CurrentTotalCount { get; set; }
        public List<MemberDto>? DataAdded { get; set; }
        public List<MemberDto>? DataRemoved { get; set; }
        public List<MemberDto>? DateFilterAdded { get; set; }
        public List<MemberDto>? DateFilterRemoved { get; set; }
    }

    private sealed class MemberDto
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("n")]
        public string? N { get; set; }

        [JsonPropertyName("p")]
        public string? P { get; set; }

        [JsonPropertyName("vs")]
        public string? Vs { get; set; }

        [JsonPropertyName("ve")]
        public string? Ve { get; set; }

        public MemberDto() { }

        public MemberDto(ReportDashboardRosterMember m)
        {
            Id = m.PersonId;
            N = m.Name;
            P = m.PassportOrKey;
            Vs = m.VisaStartOn?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            Ve = m.VisaEndOn?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        public ReportDashboardRosterMember ToMember()
        {
            DateTime? start = null;
            DateTime? end = null;
            if (!string.IsNullOrWhiteSpace(Vs)
                && DateTime.TryParse(Vs, CultureInfo.InvariantCulture, DateTimeStyles.None, out var s))
                start = s.Date;
            if (!string.IsNullOrWhiteSpace(Ve)
                && DateTime.TryParse(Ve, CultureInfo.InvariantCulture, DateTimeStyles.None, out var e))
                end = e.Date;
            return new ReportDashboardRosterMember
            {
                PersonId = Id,
                Name = N ?? string.Empty,
                PassportOrKey = P ?? string.Empty,
                VisaStartOn = start,
                VisaEndOn = end
            };
        }
    }
}
