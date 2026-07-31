using DevExpress.ExpressApp;

namespace Visa2026.Module;

/// <summary>
/// Carries the active Person ListView frame for dossier opens from row icon clicks.
/// </summary>
public static class PersonDossierNavigationContext
{
    private static readonly AsyncLocal<Frame?> SourceFrame = new();

    public static Frame? SourceFrameValue
    {
        get => SourceFrame.Value;
        set => SourceFrame.Value = value;
    }
}
