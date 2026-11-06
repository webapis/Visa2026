using System;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.PreviewSlot;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class BorderZoneSourceVisaComposeTests
{
    [Fact]
    public void FormatSourceVisaCaption_includes_number_and_end()
    {
        var visa = new Visa
        {
            VisaNumber = "V-100",
            ExpirationDate = new DateTime(2026, 12, 31),
        };

        var caption = IssueIssuedHeaderComposeService.FormatSourceVisaCaption(visa);
        Assert.Contains("V-100", caption, StringComparison.Ordinal);
        Assert.Contains("31.12.2026", caption, StringComparison.Ordinal);
    }

    [Fact]
    public void BorderZoneEndDateDiffersFromVisa_true_when_dates_differ()
    {
        var draft = new IssueIssuedHeaderComposeDraft
        {
            Kind = IssueIssuedHeaderKind.BorderZone,
            SourceVisaEndDate = new DateTime(2026, 12, 31),
            ExpirationDate = new DateTime(2026, 11, 30),
        };

        Assert.True(IssueIssuedHeaderComposeService.BorderZoneEndDateDiffersFromVisa(draft));
    }

    [Fact]
    public void BorderZoneEndDateDiffersFromVisa_false_when_same_day()
    {
        var draft = new IssueIssuedHeaderComposeDraft
        {
            Kind = IssueIssuedHeaderKind.BorderZone,
            SourceVisaEndDate = new DateTime(2026, 12, 31),
            ExpirationDate = new DateTime(2026, 12, 31),
        };

        Assert.False(IssueIssuedHeaderComposeService.BorderZoneEndDateDiffersFromVisa(draft));
    }

    [Fact]
    public void TryResolveKind_borderZone_catalog()
    {
        Assert.True(IssueIssuedHeaderComposeService.TryResolveKind("borderZone", out var kind));
        Assert.Equal(IssueIssuedHeaderKind.BorderZone, kind);
        Assert.Equal("borderZone", IssueIssuedHeaderComposeService.CatalogKeyFor(kind));
    }
}