using System;
using System.Reflection;

namespace Visa2026.Module.DatabaseUpdate.LookupCatalogs;

/// <summary>
/// Decides when JSON catalog sync should run (schema upgrade or bumped manifest), not on every <c>--forceUpdate</c>.
/// </summary>
internal static class LookupCatalogSyncPolicy
{
    public static Version GetModuleAssemblyVersion() =>
        typeof(LookupCatalogEntitySync).Assembly.GetName().Version ?? new Version(0, 0, 0, 0);

    public static int GetEffectiveManifestVersion(LookupCatalogManifest manifest) =>
        manifest.Version < 0 ? 0 : manifest.Version;

    public static bool ShouldRunCatalogSync(
        Version currentDbVersion,
        int storedManifestVersion,
        int effectiveManifestVersion,
        out string reason)
    {
        var moduleVersion = GetModuleAssemblyVersion();
        if (currentDbVersion < moduleVersion)
        {
            reason =
                $"database version {currentDbVersion} is behind module {moduleVersion} (schema/module upgrade).";
            return true;
        }

        if (effectiveManifestVersion > storedManifestVersion)
        {
            reason =
                $"lookup catalog manifest version {effectiveManifestVersion} is ahead of stored {storedManifestVersion}.";
            return true;
        }

        reason =
            "database and module versions match and lookup manifest is already applied; skipping JSON catalog sync.";
        return false;
    }
}
