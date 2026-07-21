using Visa2026.DataImporter.Legacy.Visa2014;
using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014.Tests;

public class Visa2014ApplicationProgressCompletionIndexTests
{
    [Fact]
    public void BuildEvidence_PrefersInvitationWhenLaterDate()
    {
        var evidence = Visa2014ApplicationProgressCompletionIndex.BuildEvidence(
            new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["InvitationIssuedDate"] = "2016-01-14",
                ["InvitationNumber"] = "01//77",
                ["WorkPermitIssuedDate"] = "2015-12-24",
                ["WorkPermitNumber"] = "WP-1",
            });

        Assert.True(evidence.HasCompletion);
        Assert.Equal(new DateTime(2016, 1, 14), evidence.CompletionDate);
        Assert.Equal("InvitationNumber", evidence.SourceLabel);
        Assert.Equal("01//77", evidence.SourceValue);
    }

    [Fact]
    public void BuildEvidence_UsesWorkPermitWhenOnlyWorkPermitPresent()
    {
        var evidence = Visa2014ApplicationProgressCompletionIndex.BuildEvidence(
            new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["WorkPermitIssuedDate"] = "2015-12-24",
                ["WorkPermitNumber"] = "WP-99",
            });

        Assert.True(evidence.HasCompletion);
        Assert.Equal("WorkPermitNumber", evidence.SourceLabel);
        Assert.Equal("WP-99", evidence.SourceValue);
    }
}
