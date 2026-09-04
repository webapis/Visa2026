using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;

namespace Visa2026.Module.Services.OfficerShell;

public sealed class OfficerShellNavQueryService : IOfficerShellNavQueryService
{
    private readonly IOfficerShellStagedQueryService _stagedQueryService;
    private readonly IOfficerShellInProcessQueryService _inProcessQueryService;

    public OfficerShellNavQueryService(
        IOfficerShellStagedQueryService stagedQueryService,
        IOfficerShellInProcessQueryService inProcessQueryService)
    {
        _stagedQueryService = stagedQueryService;
        _inProcessQueryService = inProcessQueryService;
    }

    public OfficerShellNavCounts GetCounts(IObjectSpace objectSpace) =>
        new()
        {
            StagedCount = _stagedQueryService.GetStagedProfiles(objectSpace).Count,
            InProcessCount = _inProcessQueryService.GetInProcessProfiles(objectSpace).Count,
        };
}
