using System;

namespace Visa2026.Module.Services.ApplicationWorkspace;

/// <summary>Opens the circuit-wide Ýagdaý syny dialog for one list row.</summary>
public interface IApplicationProfileInstanceStatePreviewService
{
    Guid? CurrentId { get; }

    /// <summary>List view that opened the sheet. The copy preview is owned by that view.</summary>
    string? OwnerViewId { get; }

    event Action? Changed;

    void Open(Guid instanceId, string? ownerViewId = null);

    void Close();
}

public sealed class ApplicationProfileInstanceStatePreviewService : IApplicationProfileInstanceStatePreviewService
{
    public Guid? CurrentId { get; private set; }

    public string? OwnerViewId { get; private set; }

    public event Action? Changed;

    public void Open(Guid instanceId, string? ownerViewId = null)
    {
        if (instanceId == Guid.Empty)
            return;

        CurrentId = instanceId;
        OwnerViewId = string.IsNullOrWhiteSpace(ownerViewId) ? null : ownerViewId.Trim();
        Changed?.Invoke();
    }

    public void Close()
    {
        if (CurrentId == null)
            return;

        CurrentId = null;
        OwnerViewId = null;
        Changed?.Invoke();
    }
}
