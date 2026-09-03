using System;
using DevExpress.ExpressApp;
using Visa2026.Module.Services.OrganizationCatalogs;

namespace Visa2026.Module.Services.ApplicationProfileWizard;

/// <summary>
/// Opens Configuration → Organization catalogs (Company / Signatory / Representative are not singletons).
/// </summary>
public static class ApplicationProfileWizardOrganizationOpenHelper
{
    public enum Kind
    {
        Company,
        Signatory,
        Representative
    }

    public static bool TryOpen(XafApplication application, Kind kind, Action? onClosed = null)
    {
        _ = kind;
        return OrganizationCatalogsOpenHelper.TryShow(application, onClosed);
    }
}