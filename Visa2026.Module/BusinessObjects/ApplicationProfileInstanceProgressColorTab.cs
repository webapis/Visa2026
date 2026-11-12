using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Visa2026.Module.Appearance;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// One ListView color-tab per painted progress row color
/// (<see cref="docs/APPLICATION_PROFILE_LISTVIEW_PROGRESS_COLORS.md"/>).
/// </summary>
public static class ApplicationProfileInstanceProgressColorTab
{
    public const string AllKey = "";
    public const string SearchKey = "search";
    public const string CriteriaKey = "ProgressColorTab";

    public const string Office = "office";
    public const string ApprovalInProgress = "approval";
    public const string LastApproval = "last-approval";
    public const string Submitted = "submitted";
    public const string ProcessStarted = "process-started";
    public const string Issued = "issued";
    public const string Rejected = "rejected";
    public const string Cancelled = "cancelled";

    private static readonly string[] BucketOrder =
    [
        Office,
        ApprovalInProgress,
        LastApproval,
        Submitted,
        ProcessStarted,
        Issued,
        Rejected,
        Cancelled,
    ];

    public readonly record struct TabInfo(string Key, string Label, string BackgroundHex, int Count);

    public static IReadOnlyList<string> OrderedBucketKeys => BucketOrder;

    public static bool IsSearchTab(string? key) =>
        string.Equals(key?.Trim(), SearchKey, StringComparison.OrdinalIgnoreCase);

    public static string ResolveBucketKey(string? listRowAppearanceStateCode, string? primaryStateCode = null)
    {
        var appearance = listRowAppearanceStateCode?.Trim() ?? string.Empty;
        if (string.Equals(appearance, ApplicationProfileInstanceProgressSlaCodes.Warning, StringComparison.OrdinalIgnoreCase)
            || string.Equals(appearance, ApplicationProfileInstanceProgressSlaCodes.Overdue, StringComparison.OrdinalIgnoreCase))
        {
            appearance = primaryStateCode?.Trim() ?? string.Empty;
        }

        if (string.IsNullOrEmpty(appearance))
            return Office;

        if (string.Equals(appearance, ApplicationProfileInstanceProgressStateCodes.IsBeingPrepared, StringComparison.OrdinalIgnoreCase)
            || string.Equals(appearance, ApplicationProfileInstanceProgressLocationCodes.AtOffice, StringComparison.OrdinalIgnoreCase))
            return Office;

        if (string.Equals(appearance, ApplicationProfileInstanceProgressLegCodes.LastApprovalCompleteListAppearance, StringComparison.OrdinalIgnoreCase))
            return LastApproval;

        if (string.Equals(appearance, ApplicationProfileInstanceProgressStateCodes.SubmittedToMigration, StringComparison.OrdinalIgnoreCase))
            return Submitted;

        if (string.Equals(appearance, ApplicationProfileInstanceProgressStateCodes.ProcessStarted, StringComparison.OrdinalIgnoreCase))
            return ProcessStarted;

        if (string.Equals(appearance, ApplicationProfileInstanceProgressStateCodes.ProcessIssued, StringComparison.OrdinalIgnoreCase))
            return Issued;

        if (string.Equals(appearance, ApplicationProfileInstanceProgressStateCodes.ProcessCancelled, StringComparison.OrdinalIgnoreCase))
            return Cancelled;

        if (string.Equals(appearance, ApplicationProfileInstanceProgressStateCodes.ProcessRejected, StringComparison.OrdinalIgnoreCase)
            || ApplicationProfileInstanceProgressLegCodes.IsReviewRejectedStateCode(appearance))
            return Rejected;

        if (ApplicationProfileInstanceProgressLegCodes.IsApprovalLegListRowTintState(appearance))
            return ApprovalInProgress;

        if (!string.IsNullOrWhiteSpace(primaryStateCode)
            && !string.Equals(primaryStateCode, appearance, StringComparison.OrdinalIgnoreCase))
            return ResolveBucketKey(primaryStateCode, null);

        return Office;
    }

    public static string ResolveBucketKey(ApplicationProfileInstance application)
    {
        ArgumentNullException.ThrowIfNull(application);
        var display = ApplicationListViewDisplayState.Resolve(application);
        return ResolveBucketKey(display.ListRowAppearanceStateCode, display.PrimaryStateCode);
    }

    public static string GetSearchLabel(CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentUICulture;
        var name = culture.Name ?? string.Empty;
        if (name.StartsWith("tk", StringComparison.OrdinalIgnoreCase))
            return "Gözleg";
        if (name.StartsWith("tr", StringComparison.OrdinalIgnoreCase))
            return "Arama";
        if (name.StartsWith("ru", StringComparison.OrdinalIgnoreCase))
            return "Поиск";
        return "Search";
    }

    public static string GetLabel(string bucketKey) =>
        IsSearchTab(bucketKey)
            ? GetSearchLabel()
            : string.IsNullOrEmpty(bucketKey)
                ? "Ähli"
                : bucketKey switch
                {
                    Office => "Ofisde",
                    ApprovalInProgress => "Ministrlige iberlen",
                    LastApproval => "Ministrlikden çykan",
                    Submitted => "Migrasiýa gullugyna tabşyryldy",
                    ProcessStarted => "CO belgisi alyndy",
                    Issued => "Resmileşdirildi",
                    Rejected => "Garşylyk berildi",
                    Cancelled => "Ýüz tutma ýatyryldy",
                    _ => bucketKey,
                };

    public static string GetBackgroundHex(string bucketKey)
    {
        if (IsSearchTab(bucketKey) || string.IsNullOrEmpty(bucketKey))
            return string.Empty;

        var appearanceCode = bucketKey switch
        {
            Office => ApplicationProfileInstanceProgressStateCodes.IsBeingPrepared,
            ApprovalInProgress => ApplicationProfileInstanceProgressStateCodes.Review1Started,
            LastApproval => ApplicationProfileInstanceProgressLegCodes.LastApprovalCompleteListAppearance,
            Submitted => ApplicationProfileInstanceProgressStateCodes.SubmittedToMigration,
            ProcessStarted => ApplicationProfileInstanceProgressStateCodes.ProcessStarted,
            Issued => ApplicationProfileInstanceProgressStateCodes.ProcessIssued,
            Rejected => ApplicationProfileInstanceProgressStateCodes.ProcessRejected,
            Cancelled => ApplicationProfileInstanceProgressStateCodes.ProcessCancelled,
            _ => null,
        };

        if (appearanceCode != null
            && BoStateAppearanceColors.TryGet(appearanceCode, out var appearance))
            return appearance.CssBackgroundHex;

        return "#e5e7eb";
    }

    public static IReadOnlyList<TabInfo> BuildTabs(
        IEnumerable<ApplicationProfileInstance> applications,
        string selectedKey,
        bool includeSearchTab = false,
        int searchMatchCount = 0)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var total = 0;
        foreach (var app in applications)
        {
            if (app == null)
                continue;
            total++;
            var key = ResolveBucketKey(app);
            counts[key] = counts.TryGetValue(key, out var n) ? n + 1 : 1;
        }

        var tabs = new List<TabInfo>(BucketOrder.Length + 2)
        {
            new(AllKey, GetLabel(AllKey), string.Empty, total),
        };

        if (includeSearchTab)
        {
            tabs.Add(new TabInfo(SearchKey, GetSearchLabel(), string.Empty, Math.Max(0, searchMatchCount)));
        }

        foreach (var key in BucketOrder)
        {
            counts.TryGetValue(key, out var count);
            tabs.Add(new TabInfo(key, GetLabel(key), GetBackgroundHex(key), count));
        }

        _ = selectedKey;
        return tabs;
    }

    public static string NormalizeSelection(string? selectedKey, IEnumerable<TabInfo> tabs)
    {
        var key = selectedKey?.Trim() ?? AllKey;
        if (string.IsNullOrEmpty(key))
            return AllKey;

        if (tabs.Any(t => string.Equals(t.Key, key, StringComparison.OrdinalIgnoreCase)))
            return key;

        return AllKey;
    }
}