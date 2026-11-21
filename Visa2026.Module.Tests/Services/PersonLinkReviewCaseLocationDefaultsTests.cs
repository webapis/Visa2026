using System.Collections.Generic;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class PersonLinkReviewCaseLocationDefaultsTests
{
    [Fact]
    public void ApplyCaseLocationDefaults_fills_empty_wp_from_case()
    {
        var line = new PersonLinkReviewLine
        {
            Kind = ApplicationProfileInstancePersonLinkKind.WorkPermitItem,
            TabKey = "WorkPermitLocation",
            OmitFromJoin = true,
            LocationChips = new List<PersonLinkReviewLocationChip>(),
            Summary = "—",
            Fields = new List<PersonLinkReviewField>
            {
                new() { Label = "WP", Value = "—", Required = true },
            },
        };

        PersonLinkReview.ApplyCaseLocationDefaults(
            new List<PersonLinkReviewLine> { line },
            "Aşgabat şäheri, Mary şäheri",
            "Sarahs etraby");

        Assert.Equal(2, line.LocationChips.Count);
        Assert.Contains(line.LocationChips, c => c.Value == "Aşgabat şäheri");
        Assert.Contains(line.LocationChips, c => c.Value == "Mary şäheri");
        Assert.Equal(LinkReviewLocationChipSource.Case, line.LocationChips[0].Source);
    }

    [Fact]
    public void ApplyCaseLocationDefaults_does_not_overwrite_joined_chips()
    {
        var line = new PersonLinkReviewLine
        {
            Kind = ApplicationProfileInstancePersonLinkKind.WorkPermitItem,
            TabKey = "WorkPermitLocation",
            OmitFromJoin = true,
            LocationChips = LinkReviewLocationChips.Build("Kopetdag", "Aşgabat", null, null).ToList(),
        };

        PersonLinkReview.ApplyCaseLocationDefaults(
            new List<PersonLinkReviewLine> { line },
            "Aşgabat şäheri, Mary şäheri",
            null);

        Assert.Single(line.LocationChips);
        Assert.Equal("Kopetdag", line.LocationChips[0].Value);
        Assert.Equal(LinkReviewLocationChipSource.Joined, line.LocationChips[0].Source);
    }
}