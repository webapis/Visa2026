using System;
using Visa2026.Module.Services.WordReports;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ResminamalarCaseCatalogCacheTests
{
    [Fact]
    public void TryGet_Misses_WhenProjectContractChanges()
    {
        var cache = new ResminamalarCaseCatalogCache();
        var applicationId = Guid.NewGuid();
        var first = new ResminamalarCaseCatalogKey(applicationId, Guid.NewGuid(), Guid.Empty);
        var second = first with { ProjectContractId = Guid.NewGuid() };
        var catalog = EmptyCatalog("First");

        cache.Set(first, catalog, "9/-003");

        Assert.True(cache.TryGet(first, out var hit, out var number));
        Assert.Equal("First", hit.Entries[0].DisplayName);
        Assert.Equal("9/-003", number);
        Assert.False(cache.TryGet(second, out _, out _));
    }

    [Fact]
    public void TryGet_Misses_WhenMigrationServiceChanges()
    {
        var cache = new ResminamalarCaseCatalogCache();
        var applicationId = Guid.NewGuid();
        var first = new ResminamalarCaseCatalogKey(applicationId, Guid.Empty, Guid.NewGuid());
        var second = first with { MigrationServiceId = Guid.NewGuid() };

        cache.Set(first, EmptyCatalog("A"), "1");

        Assert.False(cache.TryGet(second, out _, out _));
    }

    [Fact]
    public void Clear_DropsCachedCatalog()
    {
        var cache = new ResminamalarCaseCatalogCache();
        var key = new ResminamalarCaseCatalogKey(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty);
        cache.Set(key, EmptyCatalog("A"), "1");
        cache.Clear();

        Assert.False(cache.TryGet(key, out _, out _));
    }

    private static ApplicationWordReportPackageCatalog EmptyCatalog(string name) =>
        new()
        {
            Entries =
            [
                new ApplicationWordReportPackageCatalogEntry
                {
                    EntryKey = "profile:" + Guid.NewGuid().ToString("D"),
                    DisplayName = name,
                },
            ],
        };
}