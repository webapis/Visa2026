using System;
using System.Linq;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;
using Visa2026.Module.Tests.TestSupport;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationTypeCodePickerHelperLoadRowsTests
{
    [Fact]
    public void LoadRows_ThrowsWhenObjectSpaceNull()
    {
        Assert.Throws<ArgumentNullException>(() => ApplicationTypeCodePickerHelper.LoadRows(null!));
    }

    [Fact]
    public void LoadRows_OmitsBlankCodes_HiddenDeprecated_AndOrdersBySelectionCode()
    {
        var readyInv = new ApplicationType
        {
            ID = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Name = "App_Inv",
            SelectionCode = "101",
            ApplicationProfileInstanceProgressRoute = ApplicationProfileInstanceProgressRouteKind.ViaMinistries,
        };
        var blankCode = new ApplicationType
        {
            ID = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Name = "App_Inv_FM",
            SelectionCode = "",
            ApplicationProfileInstanceProgressRoute = ApplicationProfileInstanceProgressRouteKind.ViaMinistries,
        };
        var nullCode = new ApplicationType
        {
            ID = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Name = "App_Exit_Visa",
            SelectionCode = null!,
            ApplicationProfileInstanceProgressRoute = ApplicationProfileInstanceProgressRouteKind.ViaMinistries,
        };
        var hiddenExt = new ApplicationType
        {
            ID = Guid.Parse("44444444-4444-4444-4444-444444444444"),
            Name = "App_Visa_Ext",
            SelectionCode = "702",
            ApplicationProfileInstanceProgressRoute = ApplicationProfileInstanceProgressRouteKind.ViaMinistries,
        };
        var readyWp = new ApplicationType
        {
            ID = Guid.Parse("55555555-5555-5555-5555-555555555555"),
            Name = "App_WP_Ext",
            SelectionCode = "401",
            ApplicationProfileInstanceProgressRoute = ApplicationProfileInstanceProgressRouteKind.ViaMinistries,
        };
        var directRoute = new ApplicationType
        {
            ID = Guid.Parse("66666666-6666-6666-6666-666666666666"),
            Name = "App_Reg_Check_In",
            SelectionCode = "301",
            ApplicationProfileInstanceProgressRoute = ApplicationProfileInstanceProgressRouteKind.DirectToMigrationService,
        };

        var objectSpace = TypePickerQueryObjectSpaceStub.Create(stub =>
            stub.Seed(readyInv, blankCode, nullCode, hiddenExt, readyWp, directRoute));

        var rows = ApplicationTypeCodePickerHelper.LoadRows(objectSpace);

        Assert.Equal(new[] { "101", "301", "401" }, rows.Select(r => r.SelectionCode).ToArray());
        Assert.DoesNotContain(rows, r => r.SelectionCode == "702");
        Assert.All(rows, r => Assert.True(r.CanSelect));
        Assert.Equal(ApplicationTypeReadinessStatus.Ready, rows.Single(r => r.SelectionCode == "101").ReadinessStatus);
    }

    [Fact]
    public void LoadRows_FiltersByProgressRoute()
    {
        var via = new ApplicationType
        {
            ID = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Name = "App_Inv",
            SelectionCode = "101",
            ApplicationProfileInstanceProgressRoute = ApplicationProfileInstanceProgressRouteKind.ViaMinistries,
        };
        var direct = new ApplicationType
        {
            ID = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            Name = "App_Reg_Check_In",
            SelectionCode = "301",
            ApplicationProfileInstanceProgressRoute = ApplicationProfileInstanceProgressRouteKind.DirectToMigrationService,
        };

        var objectSpace = TypePickerQueryObjectSpaceStub.Create(stub => stub.Seed(via, direct));

        var viaOnly = ApplicationTypeCodePickerHelper.LoadRows(
            objectSpace,
            ApplicationProfileInstanceProgressRouteKind.ViaMinistries);
        var directOnly = ApplicationTypeCodePickerHelper.LoadRows(
            objectSpace,
            ApplicationProfileInstanceProgressRouteKind.DirectToMigrationService);

        Assert.Equal(new[] { "101" }, viaOnly.Select(r => r.SelectionCode).ToArray());
        Assert.Equal(new[] { "301" }, directOnly.Select(r => r.SelectionCode).ToArray());
        Assert.Equal(via.ID, viaOnly.Single().Id);
        Assert.Equal("App_Reg_Check_In", directOnly.Single().Name);
    }
}
