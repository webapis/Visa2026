using DevExpress.ExpressApp;
using Visa2026.Module.DatabaseUpdate.LookupCatalogs;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Inserts missing Company / Signatory / Representative rows from tenant JSON.
/// Runs from host start (SeedGate) because ModuleUpdater JSON sync is skipped when the
/// lookup manifest version is already stored.
/// </summary>
public static class OrganizationCatalogSeed
{
    public static int EnsureMissing(IObjectSpace objectSpace)
    {
        ArgumentNullException.ThrowIfNull(objectSpace);
        var manifest = LookupCatalogResourceLoader.LoadManifest();
        return LookupCatalogEntitySync.EnsureMissingOrganizationCatalogRows(objectSpace, manifest.Catalogs);
    }
}