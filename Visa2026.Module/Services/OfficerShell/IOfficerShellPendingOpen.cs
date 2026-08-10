using System;

namespace Visa2026.Module.Services.OfficerShell;

public interface IOfficerShellPendingOpen
{
    OfficerShellPage Page { get; set; }
    Guid CaseApplicationId { get; set; }
}
