using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Visa2026.Module.Services.ApplicationWorkspace;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// ListView color tabs for Work Permits (Employee) registry.
/// </summary>
public static class WorkPermitItemColorTab
{
    public const string AllKey = "";
    public const string SearchKey = "search";
    public const string CriteriaKey = "WorkPermitColorTab";

    public const string InForce = "inforce";
    public const string Cancelled = "cancelled";
    public const string Changed = "changed";
    public const string Expired = "expired";

    private static readonly string[] BucketOrder =
    [
        InForce,
        Cancelled,
        Changed,
        Expired,
    ];

    public readonly record struct TabInfo(string Key, string Label, string BackgroundHex, int Count);

    public static IReadOnlyList<string> OrderedBucketKeys => BucketOrder;

    public static bool IsSearchTab(string? key) =>
        string.Equals(key?.Trim(), SearchKey, StringComparison.OrdinalIgnoreCase);

    public static string FromLaterLife(string? laterLife) => laterLife switch
    {
        WorkPermitItemLaterLife.Cancelled => Cancelled,
        WorkPermitItemLaterLife.Changed => Changed,
        WorkPermitItemLaterLife.Expired => Expired,
        _ => InForce,
    };

    public static string ResolveBucketKey(WorkPermitItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (!string.IsNullOrEmpty(item.ListViewLifeKey))
            return FromLaterLife(item.ListViewLifeKey);

        var cancelled = Visa2026.Module.Services.IssuedDocumentLifecycle.IsCancelled(item);
        var changed = Visa2026.Module.Services.IssuedDocumentLifecycle.IsChanged(item);
        var expired = WorkPermitItemLaterLife.IsExpired(item.ExpirationDate);
        var life = WorkPermitItemLaterLife.Resolve(cancelled, changed, expired)
            ?? WorkPermitItemLaterLife.InForce;
        return FromLaterLife(life);
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
                    InForce => "Güýjünde",
                    Cancelled => "Ýatyryldy",
                    Changed => "Üýtgedildi",
                    Expired => "Möhleti gutaran",
                    _ => bucketKey,
                };

    public static string GetBackgroundHex(string bucketKey) => bucketKey switch
    {
        InForce => "#86efac",
        Cancelled => "#f8d7da",
        Changed => "#e9d5ff",
        Expired => "#fecdd3",
        _ => string.Empty,
    };

    public static string GetRowCssClass(string bucketKey)
    {
        if (string.IsNullOrEmpty(bucketKey) || IsSearchTab(bucketKey))
            return string.Empty;
        return "visa-workpermit-row visa-workpermit-row--" + bucketKey;
    }

    public static IReadOnlyList<TabInfo> BuildTabs(
        IEnumerable<WorkPermitItem> items,
        string selectedKey,
        bool includeSearchTab = false,
        int searchMatchCount = 0)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var total = 0;
        foreach (var item in items)
        {
            if (item == null)
                continue;
            total++;
            var key = ResolveBucketKey(item);
            counts[key] = counts.TryGetValue(key, out var n) ? n + 1 : 1;
        }

        var tabs = new List<TabInfo>(BucketOrder.Length + 2)
        {
            new(AllKey, GetLabel(AllKey), string.Empty, total),
        };

        if (includeSearchTab)
            tabs.Add(new TabInfo(SearchKey, GetSearchLabel(), string.Empty, Math.Max(0, searchMatchCount)));

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