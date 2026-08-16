using System;
using Visa2026.Module.DatabaseUpdate.LookupCatalogs;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

public sealed class LookupCatalogSyncPolicyTests
{
    [Fact]
    public void GetEffectiveManifestVersion_ClampsNegativeToZero()
    {
        Assert.Equal(0, LookupCatalogSyncPolicy.GetEffectiveManifestVersion(new LookupCatalogManifest { Version = -3 }));
        Assert.Equal(7, LookupCatalogSyncPolicy.GetEffectiveManifestVersion(new LookupCatalogManifest { Version = 7 }));
    }

    [Fact]
    public void ShouldRunCatalogSync_WhenDbBehindModule_ReturnsTrue()
    {
        var moduleVersion = LookupCatalogSyncPolicy.GetModuleAssemblyVersion();
        var behind = new Version(
            Math.Max(0, moduleVersion.Major - 1),
            0,
            0,
            0);
        if (behind >= moduleVersion)
        {
            behind = new Version(0, 0, 0, 0);
        }

        var shouldRun = LookupCatalogSyncPolicy.ShouldRunCatalogSync(
            behind,
            storedManifestVersion: 99,
            effectiveManifestVersion: 99,
            out var reason);

        Assert.True(shouldRun);
        Assert.Contains("behind module", reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ShouldRunCatalogSync_WhenManifestAhead_ReturnsTrue()
    {
        var moduleVersion = LookupCatalogSyncPolicy.GetModuleAssemblyVersion();

        var shouldRun = LookupCatalogSyncPolicy.ShouldRunCatalogSync(
            moduleVersion,
            storedManifestVersion: 2,
            effectiveManifestVersion: 5,
            out var reason);

        Assert.True(shouldRun);
        Assert.Contains("manifest version 5", reason, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("stored 2", reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ShouldRunCatalogSync_WhenVersionsMatch_ReturnsFalse()
    {
        var moduleVersion = LookupCatalogSyncPolicy.GetModuleAssemblyVersion();

        var shouldRun = LookupCatalogSyncPolicy.ShouldRunCatalogSync(
            moduleVersion,
            storedManifestVersion: 4,
            effectiveManifestVersion: 4,
            out var reason);

        Assert.False(shouldRun);
        Assert.Contains("skipping JSON catalog sync", reason, StringComparison.OrdinalIgnoreCase);
    }
}
