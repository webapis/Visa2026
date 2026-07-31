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

    [Fact]
    public void BuildVisaExtensionEvidence_RequiresFullPiaCoverage()
    {
        var partial = Visa2014ApplicationProgressCompletionIndex.BuildVisaExtensionEvidence(
            new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["ApplicationItemCount"] = "3",
                ["VisaLinkedCount"] = "2",
                ["MaxVisaIssuedDate"] = "2018-05-01",
                ["SampleVisaNumber"] = "V-1",
            });
        Assert.False(partial.HasCompletion);

        var full = Visa2014ApplicationProgressCompletionIndex.BuildVisaExtensionEvidence(
            new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["ApplicationItemCount"] = "3",
                ["VisaLinkedCount"] = "3",
                ["MaxVisaIssuedDate"] = "2018-05-01",
                ["SampleVisaNumber"] = "V-1",
            });
        Assert.True(full.HasCompletion);
        Assert.Equal(new DateTime(2018, 5, 1), full.CompletionDate);
        Assert.Equal("VisaNumber", full.SourceLabel);
        Assert.Equal("V-1", full.SourceValue);
    }

    [Fact]
    public void Merge_PrefersInvitationWorkPermitOverVisaExtension()
    {
        var appOid = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var inv = new Visa2014ApplicationProgressCompletionEvidence(
            new DateTime(2016, 1, 1), "InvitationNumber", "INV-1");
        var visa = new Visa2014ApplicationProgressCompletionEvidence(
            new DateTime(2018, 5, 1), "VisaNumber", "V-1");

        var map = Visa2014ApplicationProgressCompletionIndex.Merge(
            [(appOid, inv)],
            [(appOid, visa)]);

        Assert.Equal("InvitationNumber", map[appOid].SourceLabel);
    }
}