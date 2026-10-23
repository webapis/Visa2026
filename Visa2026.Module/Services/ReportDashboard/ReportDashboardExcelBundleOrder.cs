using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.ReportDashboard;

/// <summary>
/// Shared Excel-bundle sheet order: catalog merge + persist for all officers.
/// </summary>
public static class ReportDashboardExcelBundleOrder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static IReadOnlyList<(ReportDashboardCategory Category, string Key)> ResolveOrderedReports(
        IObjectSpace objectSpace)
    {
        var catalog = ReportDashboardCatalog.ExcelBundleReports().ToList();
        var catalogKeys = new HashSet<string>(
            catalog.Select(EntryKey), StringComparer.OrdinalIgnoreCase);

        var settings = ReportDashboardExcelBundleSettings.TryGetInstance(objectSpace);
        var saved = Parse(settings?.OrderJson);
        var ordered = new List<(ReportDashboardCategory Category, string Key)>(catalog.Count);

        foreach (var entry in saved)
        {
            if (!catalogKeys.Contains(EntryKey(entry)))
                continue;
            if (ordered.Any(x => string.Equals(EntryKey(x), EntryKey(entry), StringComparison.OrdinalIgnoreCase)))
                continue;
            ordered.Add(entry);
        }

        foreach (var entry in catalog)
        {
            if (ordered.Any(x => string.Equals(EntryKey(x), EntryKey(entry), StringComparison.OrdinalIgnoreCase)))
                continue;
            ordered.Add(entry);
        }

        return ordered;
    }

    public static void SaveOrder(
        IObjectSpace objectSpace,
        IReadOnlyList<(ReportDashboardCategory Category, string Key)> order)
    {
        var settings = ReportDashboardExcelBundleSettings.GetOrCreateInstance(objectSpace);
        var payload = order.Select(x => new OrderEntry
        {
            C = (int)x.Category,
            K = x.Key ?? string.Empty,
        }).ToList();
        settings.OrderJson = JsonSerializer.Serialize(payload, JsonOptions);
        objectSpace.CommitChanges();
    }

    private static List<(ReportDashboardCategory Category, string Key)> Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        try
        {
            var entries = JsonSerializer.Deserialize<List<OrderEntry>>(json, JsonOptions);
            if (entries == null || entries.Count == 0)
                return [];

            var list = new List<(ReportDashboardCategory Category, string Key)>(entries.Count);
            foreach (var entry in entries)
            {
                if (string.IsNullOrWhiteSpace(entry.K))
                    continue;
                if (!Enum.IsDefined(typeof(ReportDashboardCategory), entry.C))
                    continue;
                list.Add(((ReportDashboardCategory)entry.C, entry.K.Trim()));
            }

            return list;
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string EntryKey((ReportDashboardCategory Category, string Key) entry) =>
        ((int)entry.Category).ToString() + "|" + (entry.Key ?? string.Empty);

    private sealed class OrderEntry
    {
        public int C { get; set; }
        public string K { get; set; } = string.Empty;
    }
}