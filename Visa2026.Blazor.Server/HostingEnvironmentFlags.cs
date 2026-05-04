using Microsoft.Extensions.Configuration;

namespace Visa2026.Blazor.Server;

/// <summary>
/// Mirrors Startup logic for operational env flags shown in the UI.
/// </summary>
public static class HostingEnvironmentFlags
{
    public static bool IsForceXafDbUpdate(IConfiguration configuration)
    {
        var v = configuration["FORCE_XAF_DB_UPDATE"];
        return string.Equals(v, "true", StringComparison.OrdinalIgnoreCase) || v == "1";
    }
}
