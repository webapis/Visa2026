using System.Text.Json;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

public class ProjectContractMinistryLegCatalogLoaderTests
{
    [Fact]
    public void Deserialize_ReadsNestedMinistryLegsAndSlaFields()
    {
        const string json = """
            {
              "rows": [
                {
                  "NameTm": "Şatlyk‑1 (2 ministrlik)",
                  "MinistryLegs": [
                    {
                      "Sequence": 1,
                      "ApprovingMinistryShortNameTm": "Gurluşyk",
                      "MaxDaysInReview": 10,
                      "WarningDaysBeforeMax": 8
                    },
                    {
                      "Sequence": 2,
                      "ApprovingMinistryShortNameTm": "Söwda",
                      "MaxDaysInReview": 12,
                      "WarningDaysBeforeMax": 10
                    }
                  ]
                }
              ]
            }
            """;

        var catalog = JsonSerializer.Deserialize<ProjectContractCatalogFileDto>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(catalog);
        Assert.Single(catalog!.Rows);
        Assert.Equal(2, catalog.Rows[0].MinistryLegs.Count);
        Assert.Equal(12, catalog.Rows[0].MinistryLegs[1].MaxDaysInReview);
    }

    private sealed class ProjectContractCatalogFileDto
    {
        public List<ProjectContractCatalogRowDto> Rows { get; set; } = new();
    }

    private sealed class ProjectContractCatalogRowDto
    {
        public string NameTm { get; set; } = string.Empty;

        public List<ProjectContractMinistryLegCatalogRowDto> MinistryLegs { get; set; } = new();
    }

    private sealed class ProjectContractMinistryLegCatalogRowDto
    {
        public int Sequence { get; set; }

        public string ApprovingMinistryShortNameTm { get; set; } = string.Empty;

        public int? MaxDaysInReview { get; set; }

        public int? WarningDaysBeforeMax { get; set; }
    }
}
