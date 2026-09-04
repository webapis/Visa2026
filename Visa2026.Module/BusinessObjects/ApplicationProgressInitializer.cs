using System;
using DevExpress.ExpressApp;
using Visa2026.Module.Services.MigrationImport;

namespace Visa2026.Module.BusinessObjects
{
    /// <summary>
    /// Office preparation is implied when there is no progress history (no seed row).
    /// Kept as a no-op hook from <see cref="Application"/> create for backward compatibility.
    /// </summary>
    public static class ApplicationProfileInstanceProgressInitializer
    {
        public static void EnsureInitialProgress(ApplicationProfileInstance application, IObjectSpace objectSpace)
        {
            _ = application;
            _ = objectSpace;
            _ = MigrationImportContext.IsDataImport;
            // Intentionally empty: IS_BEING_PREPARED is not written — "at office" is implied
            // until the first explicit step (1_REVIEW_STARTED / PROCESS_STARTED).
        }
    }
}
