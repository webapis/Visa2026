using System;

#nullable enable

namespace Visa2026.Module.Services.ApplicationWorkspace;

public interface IApplicationProfileInstanceDeletedNotifier
{
    event Action<Guid>? Deleted;

    event Action<Guid, bool>? SpecialFollowUpChanged;

    void NotifyDeleted(Guid instanceId);

    void NotifySpecialFollowUpChanged(Guid instanceId, bool enabled);
}

public sealed class ApplicationProfileInstanceDeletedNotifier : IApplicationProfileInstanceDeletedNotifier
{
    public event Action<Guid>? Deleted;

    public event Action<Guid, bool>? SpecialFollowUpChanged;

    public void NotifyDeleted(Guid instanceId) => Deleted?.Invoke(instanceId);

    public void NotifySpecialFollowUpChanged(Guid instanceId, bool enabled) =>
        SpecialFollowUpChanged?.Invoke(instanceId, enabled);
}