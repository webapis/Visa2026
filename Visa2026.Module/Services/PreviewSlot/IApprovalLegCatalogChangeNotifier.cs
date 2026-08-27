namespace Visa2026.Module.Services.PreviewSlot;

/// <summary>
/// Lets the Application Profile wizard reload shared approval-leg radios after slot Create / Save / Delete.
/// </summary>
public interface IApprovalLegCatalogChangeNotifier
{
    event Action? Changed;

    void Notify();
}

public sealed class ApprovalLegCatalogChangeNotifier : IApprovalLegCatalogChangeNotifier
{
    public event Action? Changed;

    public void Notify() => Changed?.Invoke();
}
