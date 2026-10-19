using Visa2026.Module.Services.PersonList;
using Xunit;

namespace Visa2026.Module.Tests.Services.PersonList;

public class PersonListColumnShareTests
{
    [Fact]
    public void SharePixels_GivesWiderContentALargerShare_AndFitsThePane()
    {
        var widths = PersonListColumnShare.SharePixels(
        [
            new PersonListColumnShare.Measure("Dossier", 8, Fixed: true),
            new PersonListColumnShare.Measure("FullName", 200, Fixed: false),
            new PersonListColumnShare.Measure("Gender", 40, Fixed: false),
            new PersonListColumnShare.Measure("Address", 260, Fixed: false),
        ],
        available: 1000);

        Assert.Equal(4, widths.Count);
        Assert.Equal(1000, widths.Sum(width => width.Pixels));

        var dossier = widths.Single(width => width.Field == "Dossier").Pixels;
        var name = widths.Single(width => width.Field == "FullName").Pixels;
        var gender = widths.Single(width => width.Field == "Gender").Pixels;
        var address = widths.Single(width => width.Field == "Address").Pixels;

        Assert.InRange(dossier, PersonListColumnShare.IconMin, PersonListColumnShare.IconMax);
        Assert.True(address > name);
        Assert.True(name > gender);
    }

    [Fact]
    public void SharePixels_ShrinksEveryColumnWhenThePaneIsNarrow()
    {
        var wide = PersonListColumnShare.SharePixels(
        [
            new PersonListColumnShare.Measure("FullName", 180, Fixed: false),
            new PersonListColumnShare.Measure("Address", 240, Fixed: false),
        ],
        available: 900);
        var narrow = PersonListColumnShare.SharePixels(
        [
            new PersonListColumnShare.Measure("FullName", 180, Fixed: false),
            new PersonListColumnShare.Measure("Address", 240, Fixed: false),
        ],
        available: 400);

        Assert.Equal(900, wide.Sum(width => width.Pixels));
        Assert.Equal(400, narrow.Sum(width => width.Pixels));
        Assert.True(narrow.Single(width => width.Field == "Address").Pixels
            < wide.Single(width => width.Field == "Address").Pixels);
        Assert.True(narrow.Single(width => width.Field == "Address").Pixels
            > narrow.Single(width => width.Field == "FullName").Pixels);
    }

    [Fact]
    public void SharePixels_KeepsShortColumnsTight_AndGivesSpareWidthToLongContent()
    {
        var widths = PersonListColumnShare.SharePixels(
        [
            new PersonListColumnShare.Measure("Nationality", 60, Fixed: false),
            new PersonListColumnShare.Measure("FullName", 160, Fixed: false),
            new PersonListColumnShare.Measure("Address", 240, Fixed: false),
        ],
        available: 900);

        var nationality = widths.Single(width => width.Field == "Nationality").Pixels;
        var name = widths.Single(width => width.Field == "FullName").Pixels;
        var address = widths.Single(width => width.Field == "Address").Pixels;

        Assert.Equal(900, widths.Sum(width => width.Pixels));
        Assert.InRange(nationality, 60, PersonListColumnShare.ShortColumnMax);
        Assert.True(address > name);
        Assert.True(name > nationality);
    }

    [Fact]
    public void SharePixels_WhenThePaneIsTight_KeepsShortColumnsAndShrinksLongOnes()
    {
        var widths = PersonListColumnShare.SharePixels(
        [
            new PersonListColumnShare.Measure("Nationality", 60, Fixed: false),
            new PersonListColumnShare.Measure("Address", 400, Fixed: false),
        ],
        available: 300);

        var nationality = widths.Single(width => width.Field == "Nationality").Pixels;
        var address = widths.Single(width => width.Field == "Address").Pixels;

        Assert.Equal(300, widths.Sum(width => width.Pixels));
        Assert.InRange(nationality, 60, PersonListColumnShare.ShortColumnMax);
        Assert.True(address > nationality);
        Assert.True(address < 400);
    }

    [Fact]
    public void Rebalance_ShrinksNationality_AndWidensAddressAndDepartment()
    {
        var widths = PersonListColumnShare.Rebalance(
            [
                new PersonListColumnShare.Width("Nationality", 140),
                new PersonListColumnShare.Width("EmployeeListDepartmentName", 90),
                new PersonListColumnShare.Width("EmployeeListResidenceAddress", 80),
                new PersonListColumnShare.Width("FullName", 400),
                new PersonListColumnShare.Width("Dossier", 40),
            ],
            [("Nationality", 80)],
            [("EmployeeListResidenceAddress", 220), ("EmployeeListDepartmentName", 150)],
            donorFloor: 76);

        Assert.Equal(750, widths.Sum(width => width.Pixels));
        Assert.Equal(80, widths.Single(width => width.Field == "Nationality").Pixels);
        Assert.True(widths.Single(width => width.Field == "EmployeeListResidenceAddress").Pixels >= 220);
        Assert.True(widths.Single(width => width.Field == "EmployeeListDepartmentName").Pixels >= 150);
        Assert.Equal(40, widths.Single(width => width.Field == "Dossier").Pixels);
    }

    [Fact]
    public void Rebalance_WidensVisaPosition_AndKeepsNationalityCapped()
    {
        var widths = PersonListColumnShare.Rebalance(
            [
                new PersonListColumnShare.Width("Nationality", 120),
                new PersonListColumnShare.Width("EmployeeListPositionName", 76),
                new PersonListColumnShare.Width("EmployeeListActualPositionName", 360),
            ],
            [("Nationality", 56)],
            [("EmployeeListPositionName", 180)],
            donorFloor: 76);

        Assert.Equal(556, widths.Sum(width => width.Pixels));
        Assert.Equal(56, widths.Single(width => width.Field == "Nationality").Pixels);
        Assert.True(widths.Single(width => width.Field == "EmployeeListPositionName").Pixels >= 180);
    }
}
