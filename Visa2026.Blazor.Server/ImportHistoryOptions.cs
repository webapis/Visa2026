namespace Visa2026.Blazor.Server;

/// <summary>
/// On-prem Import reimport history folder (JSON archives under history\runs\).
/// Set per IIS slot in appsettings.Production.json; defaults from DeploymentEnvironment:Slot.
/// </summary>
public sealed class ImportHistoryOptions
{
    public const string SectionName = "ImportHistory";

    /// <summary>
    /// Absolute path to the history root (contains index.html and runs\).
    /// Example Demo: C:\visa2026-sync-demo\history
    /// </summary>
    public string? RootPath { get; set; }
}
