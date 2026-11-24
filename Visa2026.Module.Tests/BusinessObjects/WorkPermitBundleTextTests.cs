#nullable enable

using System;
using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

/// <summary>
/// A. Maglumat person rows encode every valid permit in WorkPermitBundle; parse errors hide locations.
/// </summary>
public sealed class WorkPermitBundleTextTests
{
    private const char Field = '\u001e';
    private const char AppSep = '\u001d';
    private const char AppField = '\u001f';

    [Fact]
    public void Parse_empty_or_whitespace_returns_no_slots()
    {
        Assert.Empty(WorkPermitBundleText.Parse(null));
        Assert.Empty(WorkPermitBundleText.Parse(""));
        Assert.Empty(WorkPermitBundleText.Parse("   \n  "));
    }

    [Fact]
    public void Parse_skips_lines_with_fewer_than_five_fields()
    {
        var header = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var item = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var shortLine = string.Join(Field, header, item, "WP-1", "01.01.2026");
        var good = string.Join(Field, header, item, "WP-2", "02.02.2026", "03.03.2027", "Aşgabat");

        var slots = WorkPermitBundleText.Parse(shortLine + "\n" + good);

        Assert.Single(slots);
        Assert.Equal(header, slots[0].HeaderId);
        Assert.Equal(item, slots[0].ItemId);
        Assert.Equal("WP-2", slots[0].Number);
        Assert.Equal("02.02.2026", slots[0].Start);
        Assert.Equal("03.03.2027", slots[0].End);
        Assert.Equal("Aşgabat", slots[0].Locations);
        Assert.Empty(slots[0].AdditionalApplications);
        Assert.Equal(string.Empty, slots[0].AdditionalLocations);
    }

    [Fact]
    public void Parse_reads_additional_location_applications_newest_blocks_as_display_text()
    {
        var header = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var item = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var appId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var encodedApps =
            appId.ToString() + AppField + "CHG-9" + AppField + "15.03.2026" + AppField + "Mary" +
            AppSep +
            "not-a-guid" + AppField + "CHG-8";
        var line = string.Join(
            Field,
            header,
            item,
            "WP-7",
            "01.01.2026",
            "31.12.2026",
            "Ahal",
            encodedApps);

        var slots = WorkPermitBundleText.Parse(line);

        Assert.Single(slots);
        Assert.Equal(2, slots[0].AdditionalApplications.Count);
        Assert.Equal(appId, slots[0].AdditionalApplications[0].ApplicationId);
        Assert.Equal("CHG-9", slots[0].AdditionalApplications[0].Number);
        Assert.Equal("15.03.2026", slots[0].AdditionalApplications[0].Date);
        Assert.Equal("Mary", slots[0].AdditionalApplications[0].Locations);
        Assert.Equal(Guid.Empty, slots[0].AdditionalApplications[1].ApplicationId);
        Assert.Equal("CHG-8", slots[0].AdditionalApplications[1].Number);
        Assert.Equal(
            "CHG-9\n15.03.2026\nMary\n\nCHG-8",
            slots[0].AdditionalLocations);
    }

    [Fact]
    public void Display_stacks_permit_number_and_dates_as_blank_separated_blocks()
    {
        var header = Guid.NewGuid();
        var item = Guid.NewGuid();
        var first = string.Join(Field, header, item, "WP-A", "01.01.2026", "01.01.2027");
        var second = string.Join(Field, header, item, "WP-B", "", "02.02.2027");

        Assert.Equal(
            "WP-A\n01.01.2026\n01.01.2027\n\nWP-B\n02.02.2027",
            WorkPermitBundleText.Display(first + "\n" + second));
        Assert.Equal(string.Empty, WorkPermitBundleText.Display(null));
    }

    [Fact]
    public void Parse_tolerates_invalid_guids_as_empty()
    {
        var line = string.Join(Field, "bad-header", "bad-item", "WP-X", "d1", "d2");

        var slots = WorkPermitBundleText.Parse(line);

        Assert.Single(slots);
        Assert.Equal(Guid.Empty, slots[0].HeaderId);
        Assert.Equal(Guid.Empty, slots[0].ItemId);
        Assert.Equal("WP-X", slots[0].Number);
    }
}
