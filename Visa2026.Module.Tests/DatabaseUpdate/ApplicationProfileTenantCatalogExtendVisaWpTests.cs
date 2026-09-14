using System.Linq;
using Visa2026.Module.DatabaseUpdate.LookupCatalogs;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

public class ApplicationProfileTenantCatalogExtendVisaWpTests
{
    [Fact]
    public void Calik_extend_visa_wp_requires_work_permit_item_being_extended()
    {
        Assert.True(ApplicationProfileTenantCatalogLoader.TryLoadRows(out var rows));
        var row = rows.Single(r => r.Code == "extend_visa_wp");
        Assert.Equal("App_Visa_and_WP_Ext", row.ApplicationTypeName);
        Assert.True(row.RequirePersonVisa);
        Assert.True(row.RequirePersonWorkPermitItem);
        Assert.True(row.ProduceWorkPermit);
        Assert.True(row.ProduceVisa);
    }
}