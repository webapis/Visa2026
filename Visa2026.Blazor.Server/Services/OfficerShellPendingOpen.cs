using System;
using Visa2026.Module.Services.OfficerShell;

namespace Visa2026.Blazor.Server.Services;

public sealed class OfficerShellPendingOpen : IOfficerShellPendingOpen
{
    public OfficerShellPage Page { get; set; } = OfficerShellPage.Staged;
    public Guid CaseApplicationProfileInstanceId { get; set; }
}
