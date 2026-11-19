using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Visa2026.Module.Localization;
using Visa2026.Module.Services.ApplicationWorkspace;

namespace Visa2026.Module.Services.ApplicationProfilePicker;

/// <summary>
/// Create-picker Case summary "Recently used" cache: unique snapshots per Application Profile
/// and Approval leg version (direct / no-legs profiles use empty version id).
/// Persistence is host-side (browser localStorage), one key per user+profile+leg.
/// </summary>
public static class ApplicationProfilePickerCaseSummaryRecentHelper
{
    public const int Cap = 6;

    /// <summary>v4: one localStorage key per user + Application Profile + Approval leg.</summary>
    public const string StorageKeyPrefix = "visa2026.caseSummaryRecent.v4.";

    public const string LegacyStorageKeyPrefixV1 = "visa2026.caseSummaryRecent.v1.";
    public const string LegacyStorageKeyPrefixV2 = "visa2026.caseSummaryRecent.v2.";
    public const string LegacyStorageKeyPrefixV3 = "visa2026.caseSummaryRecent.v3.";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    public static string StorageKey(string? userKey, Guid profileId, Guid approvalLegVersionId)
    {
        var user = string.IsNullOrWhiteSpace(userKey) ? "anon" : userKey.Trim();
        var profilePart = profileId == Guid.Empty ? "none" : profileId.ToString("D");
        var legPart = approvalLegVersionId == Guid.Empty ? "noleg" : approvalLegVersionId.ToString("D");
        return StorageKeyPrefix + user + "." + profilePart + "." + legPart;
    }

    public static string LegacyStorageKeyV1(string? userKey)
    {
        var user = string.IsNullOrWhiteSpace(userKey) ? "anon" : userKey.Trim();
        return LegacyStorageKeyPrefixV1 + user;
    }

    public static string LegacyStorageKeyV2(string? userKey)
    {
        var user = string.IsNullOrWhiteSpace(userKey) ? "anon" : userKey.Trim();
        return LegacyStorageKeyPrefixV2 + user;
    }

    public static string LegacyStorageKeyPrefixV3Only => LegacyStorageKeyPrefixV3;

    public static ApplicationProfilePickerCaseSummaryRecentEntry CreateFromFields(
        Guid profileId,
        Guid approvalLegVersionId,
        IEnumerable<ApplicationWorkspaceCaseHeaderField>? fields,
        string? companyLabel,
        DateTime? usedAtUtc = null)
    {
        var list = fields?.ToList() ?? new List<ApplicationWorkspaceCaseHeaderField>();
        return new ApplicationProfilePickerCaseSummaryRecentEntry
        {
            Id = Guid.NewGuid(),
            ProfileId = profileId,
            ApprovalLegVersionId = approvalLegVersionId,
            UsedAtUtc = usedAtUtc ?? DateTime.UtcNow,
            ProjectLabel = DisplayLabel(list, ApplicationWorkspaceCaseHeaderFieldsHelper.Project),
            CompanyLabel = string.IsNullOrWhiteSpace(companyLabel) ? string.Empty : companyLabel.Trim(),
            VisaTypeLabel = DisplayLabel(list, ApplicationWorkspaceCaseHeaderFieldsHelper.VisaType),
            PeriodLabel = DisplayLabel(list, ApplicationWorkspaceCaseHeaderFieldsHelper.VisaPeriod),
            Updates = CaptureUpdates(list),
        };
    }

    public static IReadOnlyList<ApplicationWorkspaceCaseHeaderFieldUpdate> CaptureUpdates(
        IEnumerable<ApplicationWorkspaceCaseHeaderField>? fields)
    {
        if (fields == null)
            return Array.Empty<ApplicationWorkspaceCaseHeaderFieldUpdate>();

        var updates = new List<ApplicationWorkspaceCaseHeaderFieldUpdate>();
        foreach (var field in fields)
        {
            if (field == null || ApplicationProfilePickerCaseSummaryDraft.IsHiddenOnCreate(field.Key))
                continue;

            updates.Add(new ApplicationWorkspaceCaseHeaderFieldUpdate
            {
                Key = field.Key,
                Value = CaptureValue(field),
            });
        }

        return updates;
    }

    /// <summary>
    /// Same Project (or same filled-field fingerprint) within a profile replaces the older card.
    /// </summary>
    public static string UniquenessKey(ApplicationProfilePickerCaseSummaryRecentEntry? entry)
    {
        if (entry == null)
            return string.Empty;

        var projectId = entry.Updates?
            .FirstOrDefault(u => string.Equals(
                u.Key,
                ApplicationWorkspaceCaseHeaderFieldsHelper.Project,
                StringComparison.Ordinal))
            ?.Value;
        if (!string.IsNullOrWhiteSpace(projectId))
            return "project:" + projectId.Trim().ToLowerInvariant();

        if (!string.IsNullOrWhiteSpace(entry.ProjectLabel))
            return "project-label:" + entry.ProjectLabel.Trim().ToLowerInvariant();

        return "fp:" + Fingerprint(entry.Updates);
    }

    /// <summary>
    /// Inserts <paramref name="incoming"/> at the front; drops older duplicates (same uniqueness key).
    /// Caller should pass only this profile's list (v3 storage is already per-profile).
    /// </summary>
    public static IReadOnlyList<ApplicationProfilePickerCaseSummaryRecentEntry> Push(
        IEnumerable<ApplicationProfilePickerCaseSummaryRecentEntry>? current,
        ApplicationProfilePickerCaseSummaryRecentEntry incoming,
        int cap = Cap)
    {
        ArgumentNullException.ThrowIfNull(incoming);
        if (incoming.ProfileId == Guid.Empty)
            throw new ArgumentException("ProfileId is required.", nameof(incoming));
        if (cap < 1)
            cap = Cap;

        var key = UniquenessKey(incoming);
        var list = Deduplicate(
                (current ?? Array.Empty<ApplicationProfilePickerCaseSummaryRecentEntry>())
                    .Where(e => e != null
                        && e.ProfileId == incoming.ProfileId
                        && e.ApprovalLegVersionId == incoming.ApprovalLegVersionId))
            .Where(e => !string.Equals(UniquenessKey(e), key, StringComparison.Ordinal))
            .ToList();

        list.Insert(0, incoming);
        if (list.Count > cap)
            list = list.Take(cap).ToList();
        return list;
    }

    public static IReadOnlyList<ApplicationProfilePickerCaseSummaryRecentEntry> ForProfile(
        IEnumerable<ApplicationProfilePickerCaseSummaryRecentEntry>? entries,
        Guid profileId,
        Guid approvalLegVersionId = default,
        int cap = Cap)
    {
        if (profileId == Guid.Empty || entries == null)
            return Array.Empty<ApplicationProfilePickerCaseSummaryRecentEntry>();
        if (cap < 1)
            cap = Cap;

        return Deduplicate(entries.Where(e =>
                e != null
                && e.ProfileId == profileId
                && e.ApprovalLegVersionId == approvalLegVersionId))
            .Take(cap)
            .ToList();
    }

    public static string Serialize(IEnumerable<ApplicationProfilePickerCaseSummaryRecentEntry>? entries) =>
        JsonSerializer.Serialize(
            Deduplicate(entries ?? Array.Empty<ApplicationProfilePickerCaseSummaryRecentEntry>())
                .Where(e => e.ProfileId != Guid.Empty)
                .ToList(),
            JsonOptions);

    public static IReadOnlyList<ApplicationProfilePickerCaseSummaryRecentEntry> Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return Array.Empty<ApplicationProfilePickerCaseSummaryRecentEntry>();

        try
        {
            var parsed = JsonSerializer.Deserialize<List<ApplicationProfilePickerCaseSummaryRecentEntry>>(json, JsonOptions);
            if (parsed == null || parsed.Count == 0)
                return Array.Empty<ApplicationProfilePickerCaseSummaryRecentEntry>();

            return Deduplicate(
                parsed.Where(e => e != null && e.Id != Guid.Empty && e.ProfileId != Guid.Empty));
        }
        catch (JsonException)
        {
            return Array.Empty<ApplicationProfilePickerCaseSummaryRecentEntry>();
        }
    }

    /// <summary>Newest-first, one row per <see cref="UniquenessKey"/>.</summary>
    public static IReadOnlyList<ApplicationProfilePickerCaseSummaryRecentEntry> Deduplicate(
        IEnumerable<ApplicationProfilePickerCaseSummaryRecentEntry>? entries)
    {
        if (entries == null)
            return Array.Empty<ApplicationProfilePickerCaseSummaryRecentEntry>();

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<ApplicationProfilePickerCaseSummaryRecentEntry>();
        foreach (var entry in entries
                     .Where(e => e != null)
                     .OrderByDescending(e => e.UsedAtUtc))
        {
            var key = UniquenessKey(entry);
            if (string.IsNullOrEmpty(key) || !seen.Add(key))
                continue;
            result.Add(entry);
        }

        return result;
    }

    public static string FormatRelativeTime(DateTime usedAtUtc, DateTime? nowUtc = null)
    {
        var now = nowUtc ?? DateTime.UtcNow;
        var at = usedAtUtc.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(usedAtUtc, DateTimeKind.Utc)
            : usedAtUtc.ToUniversalTime();
        var span = now.ToUniversalTime() - at;
        if (span < TimeSpan.Zero)
            span = TimeSpan.Zero;

        if (span.TotalMinutes < 1)
            return VisaUiMessages.Get("ApplicationProfileInstance.Picker.Recent.JustNow");
        if (span.TotalHours < 1)
            return VisaUiMessages.Format(
                "ApplicationProfileInstance.Picker.Recent.MinutesAgo",
                Math.Max(1, (int)span.TotalMinutes));
        if (span.TotalHours < 24)
            return VisaUiMessages.Format(
                "ApplicationProfileInstance.Picker.Recent.HoursAgo",
                Math.Max(1, (int)span.TotalHours));
        if (span.TotalDays < 2)
            return VisaUiMessages.Get("ApplicationProfileInstance.Picker.Recent.Yesterday");
        if (span.TotalDays < 14)
            return VisaUiMessages.Format(
                "ApplicationProfileInstance.Picker.Recent.DaysAgo",
                Math.Max(2, (int)span.TotalDays));
        return VisaUiMessages.Format(
            "ApplicationProfileInstance.Picker.Recent.WeeksAgo",
            Math.Max(2, (int)(span.TotalDays / 7)));
    }

    public static string PrimaryLabel(ApplicationProfilePickerCaseSummaryRecentEntry? entry)
    {
        if (entry == null)
            return VisaUiMessages.Get("ApplicationProfileInstance.Picker.Recent.Untitled");
        if (!string.IsNullOrWhiteSpace(entry.ProjectLabel))
            return entry.ProjectLabel.Trim();
        if (!string.IsNullOrWhiteSpace(entry.VisaTypeLabel))
            return entry.VisaTypeLabel.Trim();
        return VisaUiMessages.Get("ApplicationProfileInstance.Picker.Recent.Untitled");
    }

    public static string MetaLine(ApplicationProfilePickerCaseSummaryRecentEntry? entry)
    {
        if (entry == null)
            return string.Empty;

        var parts = new List<string>(2);
        if (!string.IsNullOrWhiteSpace(entry.VisaTypeLabel))
            parts.Add(entry.VisaTypeLabel.Trim());
        if (!string.IsNullOrWhiteSpace(entry.PeriodLabel))
            parts.Add(entry.PeriodLabel.Trim());
        return string.Join(" · ", parts);
    }

    public static string Fingerprint(IEnumerable<ApplicationWorkspaceCaseHeaderFieldUpdate>? updates)
    {
        // Only non-empty values so omitted vs blank do not create duplicate cards.
        var normalized = (updates ?? Array.Empty<ApplicationWorkspaceCaseHeaderFieldUpdate>())
            .Where(u => u != null
                && !string.IsNullOrWhiteSpace(u.Key)
                && !string.IsNullOrWhiteSpace(u.Value))
            .OrderBy(u => u.Key, StringComparer.Ordinal)
            .Select(u => u.Key + "=" + u.Value!.Trim());
        var raw = string.Join("\n", normalized);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash);
    }

    private static string? CaptureValue(ApplicationWorkspaceCaseHeaderField field)
    {
        if (field.Kind == ApplicationWorkspaceCaseHeaderFieldKind.Lookup
            || field.Kind == ApplicationWorkspaceCaseHeaderFieldKind.AddressObject)
            return field.SelectedId is Guid id && id != Guid.Empty ? id.ToString("D") : null;

        return string.IsNullOrWhiteSpace(field.Value) ? null : field.Value.Trim();
    }

    private static string DisplayLabel(
        IReadOnlyList<ApplicationWorkspaceCaseHeaderField> fields,
        string key)
    {
        var field = fields.FirstOrDefault(f => string.Equals(f.Key, key, StringComparison.Ordinal));
        if (field == null)
            return string.Empty;
        if (string.IsNullOrWhiteSpace(field.DisplayValue) || field.DisplayValue == "—")
            return string.Empty;
        return field.DisplayValue.Trim();
    }
}

public sealed class ApplicationProfilePickerCaseSummaryRecentEntry
{
    public Guid Id { get; init; }

    public Guid ProfileId { get; init; }

    /// <summary>Empty when the profile does not require an Approval leg version.</summary>
    public Guid ApprovalLegVersionId { get; init; }

    public DateTime UsedAtUtc { get; init; }

    public string ProjectLabel { get; init; } = string.Empty;

    public string CompanyLabel { get; init; } = string.Empty;

    public string VisaTypeLabel { get; init; } = string.Empty;

    public string PeriodLabel { get; init; } = string.Empty;

    public IReadOnlyList<ApplicationWorkspaceCaseHeaderFieldUpdate> Updates { get; init; }
        = Array.Empty<ApplicationWorkspaceCaseHeaderFieldUpdate>();
}