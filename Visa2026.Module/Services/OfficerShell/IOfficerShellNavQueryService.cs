using DevExpress.ExpressApp;

namespace Visa2026.Module.Services.OfficerShell;

public interface IOfficerShellNavQueryService
{
    OfficerShellNavCounts GetCounts(IObjectSpace objectSpace);
}
