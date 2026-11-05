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
/// Create-picker Case summary "Recently used" cache: snapshot + cap + relative time.
/// Persistence is host-side (browser localStorage); this type stays pure for tests.
/// </summary>
public static class ApplicationProfilePickerCaseSummaryRecentHelper
{
    public const int Cap = 6;
    public const string StorageKeyPrefix = "visa2026.caseSummaryRecent.v1.";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    public static string StorageKey(string? userKey)
    {
        var key = string.IsNullOrWhiteSpace(userKey) ? "anon" : userKey.Trim();
        return StorageKeyPrefix + key;
    }

    public static ApplicationProfilePickerCaseSummaryRecentEntry CreateFromFields(
        IEnumerable<ApplicationWorkspaceCaseHeaderField>? fields,
        string? companyLabel,
        DateTime? usedAtUtc = null)
    {
        var list = fields?.ToList() ?? new List<ApplicationWorkspaceCaseHeaderField>();
        return new ApplicationProfilePickerCaseSummaryRecentEntry
        {
            Id = Guid.NewGuid(),
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

    public static IReadOnlyList<ApplicationProfilePickerCaseSummaryRecentEntry> Push(
        IEnumerable<ApplicationProfilePickerCaseSummaryRecentEntry>? current,
        ApplicationProfilePickerCaseSummaryRecentEntry incoming,
        int cap = Cap)
    {
        ArgumentNullException.ThrowIfNull(incoming);
        if (cap < 1)
            cap = Cap;

        var fingerprint = Fingerprint(incoming.Updates);
        var list = (current ?? Array.Empty<ApplicationProfilePickerCaseSummaryRecentEntry>())
            .Where(e => e != null && !string.Equals(Fingerprint(e.Updates), fingerprint, StringComparison.Ordinal))
            .ToList();

        list.Insert(0, incoming);
        if (list.Count > cap)
            list = list.Take(cap).ToList();
        return list;
    }

    public static string Serialize(IEnumerable<ApplicationProfilePickerCaseSummaryRecentEntry>? entries) =>
        JsonSerializer.Serialize(
            (entries ?? Array.Empty<ApplicationProfilePickerCaseSummaryRecentEntry>()).ToList(),
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

            return parsed
                .Where(e => e != null && e.Id != Guid.Empty)
                .OrderByDescending(e => e.UsedAtUtc)
                .Take(Cap)
                .ToList();
        }
        catch (JsonException)
        {
            return Array.Empty<ApplicationProfilePickerCaseSummaryRecentEntry>();
        }
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
        var normalized = (updates ?? Array.Empty<ApplicationWorkspaceCaseHeaderFieldUpdate>())
            .Where(u => u != null && !string.IsNullOrWhiteSpace(u.Key))
            .OrderBy(u => u.Key, StringComparer.Ordinal)
            .Select(u => u.Key + "=" + (u.Value ?? string.Empty));
        var raw = string.Join("\n", normalized);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash);
    }

    private static string? CaptureValue(ApplicationWorkspaceCaseHeaderField field)
    {
        if (field.Kind == ApplicationWorkspaceCaseHeaderFieldKind.Lookup)
            return field.SelectedId is Guid id && id != Guid.Empty ? id.ToString("D") : null;

        return string.IsNullOrWhiteSpace(field.Value) ? null : field.Value;
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

    public DateTime UsedAtUtc { get; init; }

    public string ProjectLabel { get; init; } = string.Empty;

    public string CompanyLabel { get; init; } = string.Empty;

    public string VisaTypeLabel { get; init; } = string.Empty;

    public string PeriodLabel { get; init; } = string.Empty;

    public IReadOnlyList<ApplicationWorkspaceCaseHeaderFieldUpdate> Updates { get; init; }
        = Array.Empty<ApplicationWorkspaceCaseHeaderFieldUpdate>();
}