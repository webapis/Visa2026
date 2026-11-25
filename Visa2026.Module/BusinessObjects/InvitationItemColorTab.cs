using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Visa2026.Module.Services.ApplicationWorkspace;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// ListView color tabs for Çakylyklar (<see cref="InvitationItem"/> lifecycle).
/// </summary>
public static class InvitationItemColorTab
{
    public const string AllKey = "";
    public const string SearchKey = "search";
    public const string CriteriaKey = "InvitationColorTab";

    public const string Ready = "ready";
    public const string Used = "used";
    public const string Cancelled = "cancelled";
    public const string Changed = "changed";
    public const string Expired = "expired";

    private static readonly string[] BucketOrder =
    [
        Ready,
        Used,
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
        InvitationLaterLife.Used => Used,
        InvitationLaterLife.Cancelled => Cancelled,
        InvitationLaterLife.Changed => Changed,
        InvitationLaterLife.ExpiredUnused => Expired,
        _ => Ready,
    };

    public static string ResolveBucketKey(InvitationItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (!string.IsNullOrEmpty(item.ListViewLifeKey))
            return FromLaterLife(item.ListViewLifeKey);

        var cancelled = Visa2026.Module.Services.IssuedDocumentLifecycle.IsCancelled(item);
        var changed = Visa2026.Module.Services.IssuedDocumentLifecycle.IsChanged(item);
        var used = Visa2026.Module.Services.IssuedDocumentLifecycle.IsUsed(item);
        var expired = InvitationLaterLife.IsExpired(item.ExpirationDate);
        var life = InvitationLaterLife.Resolve(cancelled, changed, used, expired)
            ?? InvitationLaterLife.InForce;
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
                    Ready => "Taýýar",
                    Used => "Ulanyldy",
                    Cancelled => "Ýatyryldy",
                    Changed => "Üýtgedildi",
                    Expired => "Möhleti gutaran",
                    _ => bucketKey,
                };

    /// <summary>Row / tab swatches aligned with .invitation-life-banner--*.</summary>
    public static string GetBackgroundHex(string bucketKey) => bucketKey switch
    {
        Ready => "#86efac",
        Used => "#4ade80",
        Cancelled => "#f8d7da",
        Changed => "#e9d5ff",
        Expired => "#fecdd3",
        _ => string.Empty,
    };

    public static string GetRowCssClass(string bucketKey)
    {
        if (string.IsNullOrEmpty(bucketKey) || IsSearchTab(bucketKey))
            return string.Empty;
        return "visa-invitation-row visa-invitation-row--" + bucketKey;
    }

    public static IReadOnlyList<TabInfo> BuildTabs(
        IEnumerable<InvitationItem> items,
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