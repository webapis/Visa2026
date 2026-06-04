namespace Visa2026.Module;

/// <summary>Carries the active Person list view id into the detail view (Blazor list→detail routing).</summary>
public static class PersonDetailViewNavigationContext
{
    private static readonly AsyncLocal<string?> SourceListViewId = new();

    public static string? SourceListViewIdValue
    {
        get => SourceListViewId.Value;
        set => SourceListViewId.Value = value;
    }
}
