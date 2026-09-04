using System;
using System.Collections.Generic;

namespace Visa2026.Module.Services.WordReports;

/// <summary>
/// Case-workspace catalog identity. Includes Project contract / Migration service so a Case summary
/// change does not keep showing templates filtered for the previous lookup.
/// </summary>
public readonly record struct ResminamalarCaseCatalogKey(
    Guid ApplicationId,
    Guid ProjectContractId,
    Guid MigrationServiceId);

/// <summary>
/// Per-circuit cache so leaving and returning to case Resminamalar does not rebuild the catalog.
/// </summary>
public sealed class ResminamalarCaseCatalogCache
{
    private ResminamalarCaseCatalogKey _key;
    private string _applicationNumber = string.Empty;
    private ApplicationWordReportPackageCatalog? _catalog;

    public bool TryGet(
        ResminamalarCaseCatalogKey key,
        out ApplicationWordReportPackageCatalog catalog,
        out string applicationNumber)
    {
        if (key.ApplicationId != Guid.Empty
            && key == _key
            && _catalog != null)
        {
            catalog = _catalog;
            applicationNumber = _applicationNumber;
            return true;
        }

        catalog = new ApplicationWordReportPackageCatalog
        {
            Entries = Array.Empty<ApplicationWordReportPackageCatalogEntry>(),
        };
        applicationNumber = string.Empty;
        return false;
    }

    public void Set(
        ResminamalarCaseCatalogKey key,
        ApplicationWordReportPackageCatalog catalog,
        string applicationNumber)
    {
        _key = key;
        _catalog = catalog;
        _applicationNumber = applicationNumber ?? string.Empty;
    }

    public void Clear()
    {
        _key = default;
        _catalog = null;
        _applicationNumber = string.Empty;
    }
}