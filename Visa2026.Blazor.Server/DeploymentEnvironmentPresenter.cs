using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Visa2026.Blazor.Server;

public sealed record DeploymentEnvironmentBadgeViewModel(
    string SlotLabel,
    string DatabaseName,
    string CssModifier);

public static class DeploymentEnvironmentPresenter
{
    public static DeploymentEnvironmentBadgeViewModel? TryCreate(IConfiguration configuration)
    {
        var options = configuration.GetSection("DeploymentEnvironment").Get<DeploymentEnvironmentOptions>()
                      ?? new DeploymentEnvironmentOptions();

        if (!options.ShowOnLoginPage)
            return null;

        var databaseName = TryParseDatabaseName(configuration.GetConnectionString("DefaultConnection"));
        if (string.IsNullOrWhiteSpace(databaseName))
            return null;

        var slot = ResolveSlotLabel(configuration, options);
        var cssModifier = ResolveCssModifier(slot);

        return new DeploymentEnvironmentBadgeViewModel(slot, databaseName, cssModifier);
    }

    static string ResolveSlotLabel(IConfiguration configuration, DeploymentEnvironmentOptions options)
    {
        var slot = FirstNonEmpty(
            configuration["VISA2026_DEPLOYMENT_SLOT"],
            configuration["DEPLOYMENT_SLOT"],
            options.Slot);

        if (!string.IsNullOrWhiteSpace(slot))
            return NormalizeSlotLabel(slot);

        var environment = configuration["ASPNETCORE_ENVIRONMENT"];
        return string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase)
            ? "Development"
            : "Production";
    }

    static string NormalizeSlotLabel(string slot) =>
        slot.Trim() switch
        {
            var s when s.Equals("Production", StringComparison.OrdinalIgnoreCase) => "Production",
            var s when s.Equals("Staging", StringComparison.OrdinalIgnoreCase) => "Staging",
            var s when s.Equals("Demo", StringComparison.OrdinalIgnoreCase) => "Demo",
            var s when s.Equals("Development", StringComparison.OrdinalIgnoreCase) => "Development",
            var s when s.Equals("Legacy", StringComparison.OrdinalIgnoreCase) => "Legacy",
            var s => char.ToUpperInvariant(s[0]) + s[1..]
        };

    static string ResolveCssModifier(string slotLabel) =>
        slotLabel switch
        {
            "Production" => "production",
            "Staging" => "staging",
            "Demo" => "demo",
            "Development" => "development",
            "Legacy" => "legacy",
            _ => "default"
        };

    static string? TryParseDatabaseName(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return null;

        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            return string.IsNullOrWhiteSpace(builder.InitialCatalog) ? null : builder.InitialCatalog.Trim();
        }
        catch
        {
            return null;
        }
    }

    static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return null;
    }
}
