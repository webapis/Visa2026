using System;
using System.Collections.Generic;

namespace Visa2026.Module.Services.WordReports;

/// <summary>
/// Per-circuit cache so leaving and returning to case Resminamalar does not rebuild the catalog.
/// </summary>
public sealed class ResminamalarCaseCatalogCache
{
    private Guid _applicationId;
    private string _applicationNumber = string.Empty;
    private ApplicationWordReportPackageCatalog? _catalog;

    public bool TryGet(
        Guid applicationId,
        out ApplicationWordReportPackageCatalog catalog,
        out string applicationNumber)
    {
        if (applicationId != Guid.Empty
            && applicationId == _applicationId
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
        Guid applicationId,
        ApplicationWordReportPackageCatalog catalog,
        string applicationNumber)
    {
        _applicationId = applicationId;
        _catalog = catalog;
        _applicationNumber = applicationNumber ?? string.Empty;
    }

    public void Clear()
    {
        _applicationId = Guid.Empty;
        _catalog = null;
        _applicationNumber = string.Empty;
    }
}