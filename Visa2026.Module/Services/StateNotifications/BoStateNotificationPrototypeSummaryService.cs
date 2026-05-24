using Visa2026.Module.BusinessObjects.StateNotifications;

namespace Visa2026.Module.Services.StateNotifications;

/// <summary>Prototype summary from <see cref="BoStateNotificationPrototypeData"/>.</summary>
public sealed class BoStateNotificationPrototypeSummaryService : IBoStateNotificationSummaryService
{
    public BoStateNotificationSummary GetCurrentSummary() =>
        BoStateNotificationPrototypeData.GetSummary();
}
