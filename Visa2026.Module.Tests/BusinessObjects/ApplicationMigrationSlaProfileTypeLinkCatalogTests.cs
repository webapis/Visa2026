using System.Reflection;
using System.Text.Json;
using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

/// <summary>
/// Validates migration SLA tier codes in the application type catalog against profile seed JSON.
/// </summary>
public class ApplicationMigrationSlaProfileTypeLinkCatalogTests
{
    [Fact]
    public void EveryCatalogType_HasMigrationSlaProfileCode()
    {
        var rows = LoadApplicationTypeCatalogRows();
        Assert.NotEmpty(rows);

        foreach (var row in rows)
        {
            Assert.False(
                string.IsNullOrWhiteSpace(row.MigrationSlaProfileCode),
                $"Application type '{row.Name}' is missing MigrationSlaProfileCode in ApplicationTypeConfigurationCatalog.json.");
        }
    }

    [Fact]
    public void ProfileJson_ApplicationTypeNames_MatchTypeCatalog()
    {
        var typeRows = LoadApplicationTypeCatalogRows();
        var profileRows = LoadMigrationSlaProfileRows();

        var expectedByType = typeRows
            .Where(r => !string.IsNullOrWhiteSpace(r.MigrationSlaProfileCode))
            .ToDictionary(r => r.Name, r => r.MigrationSlaProfileCode!, StringComparer.OrdinalIgnoreCase);

        var actualByType = profileRows
            .SelectMany(r => r.ApplicationTypeNames.Select(name => (Name: name, Code: r.Code)))
            .ToDictionary(x => x.Name, x => x.Code, StringComparer.OrdinalIgnoreCase);

        Assert.Equal(expectedByType.Count, actualByType.Count);
        foreach (var (typeName, expectedCode) in expectedByType)
        {
            Assert.True(actualByType.TryGetValue(typeName, out var actualCode),
                $"Profile JSON missing ApplicationTypeNames entry for '{typeName}'.");
            Assert.Equal(expectedCode, actualCode, StringComparer.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void ProfileJson_DeserializesApplicationTypeNames()
    {
        var profileRows = LoadMigrationSlaProfileRows();
        var twoWeeks = profileRows.First(r => r.Code == "UP-TO-TWO-WEEKS");

        Assert.Equal(12, twoWeeks.ApplicationTypeNames.Count);
        Assert.Contains("App_Reg_Check_In", twoWeeks.ApplicationTypeNames);
    }
    [Fact]
    public void EveryMigrationSlaProfileCode_ExistsInProfileSeed()
    {
        var typeRows = LoadApplicationTypeCatalogRows();
        var profileCodes = LoadMigrationSlaProfileCodes();

        var distinctCodes = typeRows
            .Select(r => r.MigrationSlaProfileCode)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        Assert.NotEmpty(distinctCodes);
        foreach (var code in distinctCodes)
        {
            Assert.Contains(
                code,
                profileCodes,
                StringComparer.OrdinalIgnoreCase);
        }
    }

    private static IReadOnlyList<ApplicationTypeCatalogRowDto> LoadApplicationTypeCatalogRows()
    {
        var json = ReadEmbeddedResource(".DatabaseUpdate.LookupCatalogs.ApplicationTypeConfigurationCatalog.json");
        var catalog = JsonSerializer.Deserialize<ApplicationTypeConfigurationCatalogFileDto>(
            json,
            JsonOptions);
        Assert.NotNull(catalog);
        return catalog!.Rows;
    }

    private static IReadOnlyList<ApplicationMigrationSlaProfileCatalogRowDto> LoadMigrationSlaProfileRows()
    {
        var json = ReadEmbeddedResource(".DatabaseUpdate.LookupCatalogs.tenant.application-migration-sla-profile.json");
        var catalog = JsonSerializer.Deserialize<ApplicationMigrationSlaProfileCatalogFileDto>(
            json,
            JsonOptions);
        Assert.NotNull(catalog);
        return catalog!.Rows;
    }

    private static HashSet<string> LoadMigrationSlaProfileCodes()
    {
        return LoadMigrationSlaProfileRows()
            .Select(r => r.Code)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static string ReadEmbeddedResource(string resourceSuffix)
    {
        var assembly = typeof(ApplicationMigrationSlaProfile).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(resourceSuffix, StringComparison.OrdinalIgnoreCase));
        Assert.False(string.IsNullOrEmpty(resourceName), $"Embedded resource ending with '{resourceSuffix}' not found.");

        using var stream = assembly.GetManifestResourceStream(resourceName!);
        Assert.NotNull(stream);
        using var reader = new StreamReader(stream!);
        return reader.ReadToEnd();
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private sealed class ApplicationTypeConfigurationCatalogFileDto
    {
        public List<ApplicationTypeCatalogRowDto> Rows { get; set; } = new();
    }

    private sealed class ApplicationTypeCatalogRowDto
    {
        public string Name { get; set; } = string.Empty;

        public string? MigrationSlaProfileCode { get; set; }
    }

    private sealed class ApplicationMigrationSlaProfileCatalogFileDto
    {
        public List<ApplicationMigrationSlaProfileCatalogRowDto> Rows { get; set; } = new();
    }

    private sealed class ApplicationMigrationSlaProfileCatalogRowDto
    {
        public string Code { get; set; } = string.Empty;

        public List<string> ApplicationTypeNames { get; set; } = new();
    }
}
