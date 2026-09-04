using System.Collections.Generic;
using DevExpress.ExpressApp;

namespace Visa2026.Module.Services.OfficerShell;

public interface IOfficerShellStagedQueryService
{
    IReadOnlyList<OfficerShellStagedRow> GetStagedProfiles(IObjectSpace objectSpace);
}
