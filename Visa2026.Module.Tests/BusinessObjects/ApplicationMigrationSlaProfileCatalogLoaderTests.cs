using System.Text.Json;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

public class ApplicationMigrationSlaProfileCatalogLoaderTests
{
    [Fact]
    public void Deserialize_ReadsMigrationSlaProfileSeedRows()
    {
        const string json = """
            {
              "rows": [
                {
                  "Code": "UP-TO-3-DAYS",
                  "NameTm": "3 güne çenli",
                  "MaxDaysInReview": 3,
                  "WarningDaysBeforeMax": 2
                },
                {
                  "Code": "UP-TO-ONE-MONTH",
                  "NameTm": "1 aýa çenli",
                  "MaxDaysInReview": 20,
                  "WarningDaysBeforeMax": 16
                }
              ]
            }
            """;

        var catalog = JsonSerializer.Deserialize<ApplicationMigrationSlaProfileCatalogFileDto>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(catalog);
        Assert.Equal(2, catalog!.Rows.Count);
        Assert.Equal("UP-TO-ONE-MONTH", catalog.Rows[1].Code);
        Assert.Equal(16, catalog.Rows[1].WarningDaysBeforeMax);
    }

    private sealed class ApplicationMigrationSlaProfileCatalogFileDto
    {
        public List<ApplicationMigrationSlaProfileCatalogRowDto> Rows { get; set; } = new();
    }

    private sealed class ApplicationMigrationSlaProfileCatalogRowDto
    {
        public string Code { get; set; } = string.Empty;

        public string NameTm { get; set; } = string.Empty;

        public int? MaxDaysInReview { get; set; }

        public int? WarningDaysBeforeMax { get; set; }
    }
}
