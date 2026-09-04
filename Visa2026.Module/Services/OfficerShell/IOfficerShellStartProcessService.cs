using System;
using System.Collections.Generic;
using DevExpress.ExpressApp;

namespace Visa2026.Module.Services.OfficerShell;

public interface IOfficerShellStartProcessService
{
    OfficerShellStartProcessResult Start(IObjectSpace objectSpace, IReadOnlyList<Guid> applicationIds);
}
