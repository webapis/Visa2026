using Visa2026.Module.DatabaseUpdate;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

public class BusinessTripLookupSchemaSqlTests
{
    [Fact]
    public void EnsureTablesPostgres_ReferencesBusinessTripAddressTable()
    {
        Assert.Contains("BusinessTripAddress", BusinessTripLookupSchemaSql.EnsureTablesPostgres, StringComparison.Ordinal);
        Assert.Contains("BusinessTripPurpose", BusinessTripLookupSchemaSql.EnsureTablesPostgres, StringComparison.Ordinal);
        Assert.Contains("\"CityID\"", BusinessTripLookupSchemaSql.EnsureTablesPostgres, StringComparison.Ordinal);
    }
}
