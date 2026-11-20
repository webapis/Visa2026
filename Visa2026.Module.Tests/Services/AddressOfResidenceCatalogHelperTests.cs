using System;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class AddressOfResidenceCatalogHelperTests
{
    [Fact]
    public void BuildDedupKey_IncludesTypeAndFoldedStreet()
    {
        var key = AddressOfResidenceCatalogHelper.BuildDedupKey(
            ResidenceType.PrivateHouse,
            "Balkan welaýaty",
            "Türkmenbaşy etraby",
            "Garabogaz awtomobil ýoly");
        Assert.Contains("privatehouse", key, StringComparison.Ordinal);
        Assert.Contains("garabogaz", key, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildDedupKey_SameStreetDifferentType_Differ()
    {
        var a = AddressOfResidenceCatalogHelper.BuildDedupKey(
            ResidenceType.Lodging, "Ahal welaýaty", "Kaka etraby", "Dokma toplumynyň UYJ");
        var b = AddressOfResidenceCatalogHelper.BuildDedupKey(
            ResidenceType.PrivateHouse, "Ahal welaýaty", "Kaka etraby", "Dokma toplumynyň UYJ");
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void ClearOwnership_NullsLegacyOwners()
    {
        var address = new AddressOfResidence();
#pragma warning disable CS0618
        address.OwningApplicationProfileInstance = new ApplicationProfileInstance();
        address.RosterPerson = new Person();
        address.Person = new Person();
#pragma warning restore CS0618
        AddressOfResidenceCatalogHelper.ClearOwnership(address);
#pragma warning disable CS0618
        Assert.Null(address.OwningApplicationProfileInstance);
        Assert.Null(address.RosterPerson);
        Assert.Null(address.Person);
#pragma warning restore CS0618
    }
}