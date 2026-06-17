using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Runtime safety net after all model layers merge (localization xafml + per-user model differences).
/// Person document copies use typed Person ListView clones; header parent/item BOs use default ListView ids.
/// </summary>
public static class HeaderParentListViewConfigurator
{
    public static void EnsureConfigured(IModelApplication modelApplication)
    {
        if (modelApplication?.Views == null)
            return;

        HeaderParentListViewColumns.ApplyToViews(modelApplication.Views);
    }

    public static void Wire(XafApplication application)
    {
        application.SetupComplete += (_, _) => EnsureConfigured(application.Model);
        application.LoggedOn += (_, _) => EnsureConfigured(application.Model);
    }
}
