namespace Visa2026.Module.Services.PreviewSlot;

/// <summary>
/// Reloads Choose Approval legs cards and wizard Default radios after slot Create / Save / Delete.
/// </summary>
public interface IApprovalLegCatalogChangeNotifier
{
    event Action? Changed;

    Guid? LastChangedProfileId { get; }

    void Notify(Guid? approvalLegProfileId = null);
}

public sealed class ApprovalLegCatalogChangeNotifier : IApprovalLegCatalogChangeNotifier
{
    public event Action? Changed;

    public Guid? LastChangedProfileId { get; private set; }

    public void Notify(Guid? approvalLegProfileId = null)
    {
        LastChangedProfileId = approvalLegProfileId;
        Changed?.Invoke();
    }
}
