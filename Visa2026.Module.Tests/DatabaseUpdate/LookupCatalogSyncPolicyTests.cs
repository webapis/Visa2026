using Visa2026.Module.DatabaseUpdate.LookupCatalogs;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

public class LookupCatalogSyncPolicyTests
{
    [Fact]
    public void ShouldRunCatalogSync_when_database_version_is_behind_module()
    {
        var shouldRun = LookupCatalogSyncPolicy.ShouldRunCatalogSync(
            new Version(1, 0, 0, 350),
            storedManifestVersion: 1,
            effectiveManifestVersion: 1,
            out _);

        Assert.True(shouldRun);
    }

    [Fact]
    public void ShouldRunCatalogSync_when_manifest_version_is_ahead_of_stored()
    {
        var moduleVersion = LookupCatalogSyncPolicy.GetModuleAssemblyVersion();
        var shouldRun = LookupCatalogSyncPolicy.ShouldRunCatalogSync(
            moduleVersion,
            storedManifestVersion: 1,
            effectiveManifestVersion: 2,
            out _);

        Assert.True(shouldRun);
    }

    [Fact]
    public void ShouldNotRunCatalogSync_on_repeat_force_update_when_versions_match()
    {
        var moduleVersion = LookupCatalogSyncPolicy.GetModuleAssemblyVersion();
        var shouldRun = LookupCatalogSyncPolicy.ShouldRunCatalogSync(
            moduleVersion,
            storedManifestVersion: 1,
            effectiveManifestVersion: 1,
            out var reason);

        Assert.False(shouldRun);
        Assert.Contains("skipping", reason, StringComparison.OrdinalIgnoreCase);
    }
}
