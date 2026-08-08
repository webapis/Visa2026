using Visa2026.Module.Services.ApplicationProfileOverview;

namespace Visa2026.Blazor.Server.Services;

public sealed class ApplicationProfileOverviewPendingOpen : IApplicationProfileOverviewPendingOpen
{
    public Guid ApplicationProfileId { get; set; }
}
