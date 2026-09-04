using System.Collections.Generic;
using DevExpress.ExpressApp;

namespace Visa2026.Module.Services.ApplicationProfileCatalog;

public interface IApplicationProfileCatalogQueryService
{
    IReadOnlyList<ApplicationProfileCatalogRow> GetProfiles(IObjectSpace objectSpace);
}