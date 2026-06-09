namespace Visa2026.Blazor.Server;

/// <summary>
/// Host deployment target shown on the login page (IIS slot + SQL database).
/// Set in <c>appsettings.Production.json</c> by Configure-Visa2026Production.ps1 (-Profile).
/// </summary>
public sealed class DeploymentEnvironmentOptions
{
    /// <summary>Production, Staging, Demo, Development, or Legacy.</summary>
    public string? Slot { get; set; }

    /// <summary>When false, hide slot/database on LoginPage even if a connection string exists.</summary>
    public bool ShowOnLoginPage { get; set; } = true;
}
