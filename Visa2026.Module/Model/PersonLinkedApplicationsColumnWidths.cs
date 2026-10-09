using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Model;

/// <summary>
/// Column floors for Person → Applications (linked).
/// Progress and result columns already use large pixel widths, so the remaining
/// columns otherwise collapse to the grid minimum and captions break one letter per line.
/// </summary>
public static class PersonLinkedApplicationsColumnWidths
{
    public const int FallbackMinWidth = 150;

    public static bool SkipAutoFit(string? viewId) =>
        string.Equals(
            viewId,
            PersonNestedCollectionLayout.ApplicationProfileInstancesListView,
            StringComparison.Ordinal);

    public static bool UsesOwnWidth(string? propertyName) =>
        propertyName is nameof(ApplicationProfileInstance.ProgressStepsDisplay)
            or nameof(ApplicationProfileInstance.ResultCoverageDisplay);

    public static int ResolveWidth(string? propertyName) => propertyName switch
    {
        nameof(ApplicationProfileInstance.FullApplicationNumber) => 180,
        nameof(ApplicationProfileInstance.ProcessNumber) => 160,
        nameof(ApplicationProfileInstance.ApplicationDate) => 150,
        nameof(ApplicationProfileInstance.Year) => 96,
        nameof(ApplicationProfileInstance.ApplicationProfile) => 180,
        nameof(ApplicationProfileInstance.MonthName) => 120,
        nameof(ApplicationProfileInstance.TotalPersonCount) => 130,
        nameof(ApplicationProfileInstance.LatestProgressDate) => 170,
        _ => FallbackMinWidth,
    };
}
