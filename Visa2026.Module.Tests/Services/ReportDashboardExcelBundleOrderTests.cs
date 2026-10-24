#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using DevExpress.ExpressApp;
using DevExpress.Persistent.BaseImpl.EF;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ReportDashboard;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Shared "Reports in this file" sheet order: merge saved JSON with the live catalog,
/// drop unknown keys, append new catalog sheets, persist camelCase {"c","k"} entries.
/// </summary>
public sealed class ReportDashboardExcelBundleOrderTests
{
    [Fact]
    public void ResolveOrderedReports_empty_settings_returns_full_catalog()
    {
        var stub = BundleObjectSpaceStub.Create();
        var catalog = ReportDashboardCatalog.ExcelBundleReports();

        var ordered = ReportDashboardExcelBundleOrder.ResolveOrderedReports(stub.ObjectSpace);

        Assert.Equal(catalog.Count, ordered.Count);
        Assert.Equal(catalog.Select(EntryKey), ordered.Select(EntryKey));
    }

    [Fact]
    public void ResolveOrderedReports_reorders_known_entries_and_appends_missing()
    {
        var catalog = ReportDashboardCatalog.ExcelBundleReports();
        Assert.True(catalog.Count >= 3);

        var first = catalog[0];
        var second = catalog[1];
        var third = catalog[2];
        var settings = new ReportDashboardExcelBundleSettings
        {
            ID = Guid.NewGuid(),
            OrderJson = JsonSerializer.Serialize(new[]
            {
                new Dictionary<string, object> { ["c"] = (int)second.Category, ["k"] = second.Key },
                new Dictionary<string, object> { ["c"] = (int)first.Category, ["k"] = first.Key },
            }),
        };
        var stub = BundleObjectSpaceStub.Create(settings);

        var ordered = ReportDashboardExcelBundleOrder.ResolveOrderedReports(stub.ObjectSpace);

        Assert.Equal(EntryKey(second), EntryKey(ordered[0]));
        Assert.Equal(EntryKey(first), EntryKey(ordered[1]));
        Assert.Equal(EntryKey(third), EntryKey(ordered[2]));
        Assert.Equal(catalog.Count, ordered.Count);
        Assert.Equal(
            catalog.Select(EntryKey).OrderBy(x => x, StringComparer.Ordinal),
            ordered.Select(EntryKey).OrderBy(x => x, StringComparer.Ordinal));
    }

    [Fact]
    public void ResolveOrderedReports_skips_unknown_keys_and_duplicates()
    {
        var catalog = ReportDashboardCatalog.ExcelBundleReports();
        var keep = catalog[0];
        var settings = new ReportDashboardExcelBundleSettings
        {
            ID = Guid.NewGuid(),
            OrderJson = JsonSerializer.Serialize(new object[]
            {
                new Dictionary<string, object> { ["c"] = 0, ["k"] = "not-a-real-subreport" },
                new Dictionary<string, object> { ["c"] = 999, ["k"] = "ready-by-project" },
                new Dictionary<string, object> { ["c"] = 0, ["k"] = "" },
                new Dictionary<string, object> { ["c"] = (int)keep.Category, ["k"] = keep.Key },
                new Dictionary<string, object> { ["c"] = (int)keep.Category, ["k"] = keep.Key },
            }),
        };
        var stub = BundleObjectSpaceStub.Create(settings);

        var ordered = ReportDashboardExcelBundleOrder.ResolveOrderedReports(stub.ObjectSpace);

        Assert.Equal(EntryKey(keep), EntryKey(ordered[0]));
        Assert.Equal(1, ordered.Count(x => EntryKey(x) == EntryKey(keep)));
        Assert.Equal(catalog.Count, ordered.Count);
    }

    [Fact]
    public void ResolveOrderedReports_invalid_json_falls_back_to_catalog()
    {
        var catalog = ReportDashboardCatalog.ExcelBundleReports();
        var settings = new ReportDashboardExcelBundleSettings
        {
            ID = Guid.NewGuid(),
            OrderJson = "{not-json",
        };
        var stub = BundleObjectSpaceStub.Create(settings);

        var ordered = ReportDashboardExcelBundleOrder.ResolveOrderedReports(stub.ObjectSpace);

        Assert.Equal(catalog.Select(EntryKey), ordered.Select(EntryKey));
    }

    [Fact]
    public void SaveOrder_writes_camelCase_json_and_commits()
    {
        var stub = BundleObjectSpaceStub.Create();
        var order = new List<(ReportDashboardCategory Category, string Key)>
        {
            (ReportDashboardCategory.Invitation, "used"),
            (ReportDashboardCategory.WorkPermit, "active-by-project"),
        };

        ReportDashboardExcelBundleOrder.SaveOrder(stub.ObjectSpace, order);

        Assert.True(stub.CommitCount >= 1);
        Assert.Single(stub.Entities.OfType<ReportDashboardExcelBundleSettings>());
        var json = stub.Entities.OfType<ReportDashboardExcelBundleSettings>().Single().OrderJson;
        using var doc = JsonDocument.Parse(json);
        var arr = doc.RootElement;
        Assert.Equal(JsonValueKind.Array, arr.ValueKind);
        Assert.Equal(2, arr.GetArrayLength());
        Assert.Equal((int)ReportDashboardCategory.Invitation, arr[0].GetProperty("c").GetInt32());
        Assert.Equal("used", arr[0].GetProperty("k").GetString());
        Assert.Equal((int)ReportDashboardCategory.WorkPermit, arr[1].GetProperty("c").GetInt32());
        Assert.Equal("active-by-project", arr[1].GetProperty("k").GetString());
    }

    private static string EntryKey((ReportDashboardCategory Category, string Key) entry) =>
        ((int)entry.Category).ToString() + "|" + entry.Key;

    /// <summary>DispatchProxy stub — must not be sealed.</summary>
    private class BundleObjectSpaceStub : DispatchProxy
    {
        private readonly List<BaseObject> _all = new();

        public int CommitCount { get; private set; }

        public IReadOnlyList<BaseObject> Entities => _all;

        public IObjectSpace ObjectSpace => (IObjectSpace)this;

        public static BundleObjectSpaceStub Create(params BaseObject[] entities)
        {
            var proxy = Create<IObjectSpace, BundleObjectSpaceStub>();
            var stub = (BundleObjectSpaceStub)proxy;
            foreach (var entity in entities)
                stub._all.Add(entity);
            return stub;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            if (targetMethod.Name == nameof(IObjectSpace.GetObjectsQuery))
            {
                var elementType = targetMethod.ReturnType.GetGenericArguments()[0];
                var matches = _all.Where(x => elementType.IsInstanceOfType(x)).ToList();
                return typeof(BundleObjectSpaceStub)
                    .GetMethod(nameof(AsQueryableOf), BindingFlags.NonPublic | BindingFlags.Static)!
                    .MakeGenericMethod(elementType)
                    .Invoke(null, [matches]);
            }

            if (targetMethod.Name == nameof(IObjectSpace.CreateObject)
                && targetMethod.IsGenericMethod)
            {
                var type = targetMethod.GetGenericArguments()[0];
                var created = (BaseObject)Activator.CreateInstance(type)!;
                if (created.ID == Guid.Empty)
                    created.ID = Guid.NewGuid();
                _all.Add(created);
                return created;
            }

            if (targetMethod.Name == nameof(IObjectSpace.CommitChanges))
            {
                CommitCount++;
                return null;
            }

            throw new InvalidOperationException(
                $"Unexpected IObjectSpace call: {targetMethod.Name}");
        }

        private static IQueryable<T> AsQueryableOf<T>(IList matches) =>
            matches.Cast<T>().AsQueryable();
    }
}
