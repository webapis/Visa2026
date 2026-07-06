using Visa2026.DataImporter.Legacy.Visa2014;
using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

public class Visa2014LegacyDocumentCancellationResolverTests
{
    private static readonly ApplicationTypeVisibilityCatalog Visibility = ApplicationTypeVisibilityCatalog.Load();

    [Fact]
    public void ResolveFromCompletedCancelSubtype_VisaAndWorkPermit_SetsBoth()
    {
        var flags = Visa2014ApplicationItemCancelledFlagsMapper.ResolveFromCompletedCancelSubtype(
            Visa2014ApplicationItemCancelledFlagsMapper.LegacySubtypeCancelVisaAndWorkPermit);

        Assert.True(flags.Visa);
        Assert.True(flags.WorkPermitItem);
        Assert.False(flags.InvitationItem);
    }

    [Fact]
    public void ResolveFromCompletedCancelSubtype_VisaOnly_SetsVisa()
    {
        var flags = Visa2014ApplicationItemCancelledFlagsMapper.ResolveFromCompletedCancelSubtype(
            Visa2014ApplicationItemCancelledFlagsMapper.LegacySubtypeCancelVisa);

        Assert.True(flags.Visa);
        Assert.False(flags.WorkPermitItem);
    }

    [Fact]
    public void ResolveFromCompletedCancelSubtype_WorkPermitOnly_SetsWorkPermit()
    {
        var flags = Visa2014ApplicationItemCancelledFlagsMapper.ResolveFromCompletedCancelSubtype(
            Visa2014ApplicationItemCancelledFlagsMapper.LegacySubtypeCancelWorkPermit);

        Assert.False(flags.Visa);
        Assert.True(flags.WorkPermitItem);
    }

    [Fact]
    public void ResolveDocumentCancellation_CancelVisaAndWorkPermit_SetsBothFlags()
    {
        var flags = Visa2014ApplicationItemCancelledFlagsMapper.ResolveDocumentCancellation(
            "App_Cancel_Visa_and_WP",
            Visibility,
            legacyCancelled: true);

        Assert.True(flags.Visa);
        Assert.True(flags.WorkPermitItem);
        Assert.False(flags.InvitationItem);
    }

    [Fact]
    public void Merge_UnionOfEvidence_KeepsAllDocumentFlags()
    {
        var fromSubtype = Visa2014ApplicationItemCancelledFlagsMapper.ResolveFromCompletedCancelSubtype(
            Visa2014ApplicationItemCancelledFlagsMapper.LegacySubtypeCancelVisa);
        var fromCancelled = Visa2014ApplicationItemCancelledFlagsMapper.ResolveDocumentCancellation(
            "App_Cancell_WP",
            Visibility,
            legacyCancelled: true);

        var merged = Visa2014ApplicationItemCancelledFlagsMapper.Merge(fromSubtype, fromCancelled);

        Assert.True(merged.Visa);
        Assert.True(merged.WorkPermitItem);
    }

    [Fact]
    public void ApplyLegacyCancelledFlags_StillSetsApplicationItemWorkflowColumns()
    {
        var row = new Dictionary<string, object?>(StringComparer.Ordinal);

        Visa2014ApplicationItemCancelledFlagsMapper.ApplyLegacyCancelledFlags(
            row,
            "App_Cancell_WP",
            Visibility,
            legacyCancelled: true);

        Assert.True(row["IsCancelled"] as bool?);
        Assert.False(row["VisaIsCancelled"] as bool?);
    }
}
