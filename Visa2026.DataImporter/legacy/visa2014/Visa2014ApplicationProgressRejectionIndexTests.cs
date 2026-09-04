using Visa2026.DataImporter.Legacy.Visa2014;
using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014.Tests;

public class Visa2014ApplicationProfileInstanceProgressRejectionIndexTests
{
    [Fact]
    public void BuildEvidence_FullCoverage_WhenCountsMatchAndPositive()
    {
        var evidence = Visa2014ApplicationProfileInstanceProgressRejectionIndex.BuildEvidence(
            new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["ApplicationItemCount"] = "3",
                ["RejectionItemCount"] = "3",
                ["MaxRejectionIssuedDate"] = "2018-05-10",
                ["RejectionNumbers"] = "AS-1, AS-2",
            });

        Assert.True(evidence.HasFullCoverage);
        Assert.Equal(3, evidence.ApplicationItemCount);
        Assert.Equal(3, evidence.RejectionItemCount);
        Assert.Equal(new DateTime(2018, 5, 10), evidence.RejectionDate);
        Assert.Equal("AS-1, AS-2", evidence.RejectionNumbers);
    }

    [Fact]
    public void BuildEvidence_NotFullCoverage_WhenPartialOrZero()
    {
        var partial = Visa2014ApplicationProfileInstanceProgressRejectionIndex.BuildEvidence(
            new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["ApplicationItemCount"] = "5",
                ["RejectionItemCount"] = "2",
            });
        Assert.False(partial.HasFullCoverage);

        var zero = Visa2014ApplicationProfileInstanceProgressRejectionIndex.BuildEvidence(
            new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["ApplicationItemCount"] = "0",
                ["RejectionItemCount"] = "0",
            });
        Assert.False(zero.HasFullCoverage);
    }
}