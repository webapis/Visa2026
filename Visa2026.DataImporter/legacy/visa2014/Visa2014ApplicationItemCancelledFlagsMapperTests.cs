using Visa2026.DataImporter.Legacy.Visa2014;
using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

public class Visa2014ApplicationItemCancelledFlagsMapperTests
{
    private static readonly ApplicationTypeVisibilityCatalog Visibility = ApplicationTypeVisibilityCatalog.Load();

    [Fact]
    public void ApplyLegacyCancelledFlags_NotCancelled_ClearsAllFlags()
    {
        var row = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["InvitationItemIsCancelled"] = true,
            ["IsCancelled"] = true,
            ["VisaIsCancelled"] = true,
        };

        Visa2014ApplicationItemCancelledFlagsMapper.ApplyLegacyCancelledFlags(
            row,
            "App_Cancel_Inv",
            Visibility,
            legacyCancelled: false);

        Assert.False(row["InvitationItemIsCancelled"] as bool?);
        Assert.False(row["IsCancelled"] as bool?);
        Assert.False(row["VisaIsCancelled"] as bool?);
    }

    [Fact]
    public void ApplyLegacyCancelledFlags_CancelInvitation_SetsInvitationFlag()
    {
        var row = CreateRow();

        Visa2014ApplicationItemCancelledFlagsMapper.ApplyLegacyCancelledFlags(
            row,
            "App_Cancel_Inv",
            Visibility,
            legacyCancelled: true);

        Assert.True(row["InvitationItemIsCancelled"] as bool?);
        Assert.False(row["IsCancelled"] as bool?);
        Assert.False(row["VisaIsCancelled"] as bool?);
    }

    [Fact]
    public void ApplyLegacyCancelledFlags_CancelWorkPermit_SetsWorkPermitFlag()
    {
        var row = CreateRow();

        Visa2014ApplicationItemCancelledFlagsMapper.ApplyLegacyCancelledFlags(
            row,
            "App_Cancell_WP",
            Visibility,
            legacyCancelled: true);

        Assert.False(row["InvitationItemIsCancelled"] as bool?);
        Assert.True(row["IsCancelled"] as bool?);
        Assert.False(row["VisaIsCancelled"] as bool?);
    }

    [Fact]
    public void ApplyLegacyCancelledFlags_CancelVisa_SetsVisaFlagViaHeuristic()
    {
        var row = CreateRow();

        Visa2014ApplicationItemCancelledFlagsMapper.ApplyLegacyCancelledFlags(
            row,
            "App_Cancel_Visa",
            Visibility,
            legacyCancelled: true);

        Assert.False(row["InvitationItemIsCancelled"] as bool?);
        Assert.False(row["IsCancelled"] as bool?);
        Assert.True(row["VisaIsCancelled"] as bool?);
    }

    [Fact]
    public void ApplyLegacyCancelledFlags_CancelVisaAndWorkPermit_SetsBothFlags()
    {
        var row = CreateRow();

        Visa2014ApplicationItemCancelledFlagsMapper.ApplyLegacyCancelledFlags(
            row,
            "App_Cancel_Visa_and_WP",
            Visibility,
            legacyCancelled: true);

        Assert.False(row["InvitationItemIsCancelled"] as bool?);
        Assert.True(row["IsCancelled"] as bool?);
        Assert.True(row["VisaIsCancelled"] as bool?);
    }

    [Fact]
    public void ApplyLegacyCancelledFlags_UnknownType_FallsBackToWorkPermitFlag()
    {
        var row = CreateRow();

        Visa2014ApplicationItemCancelledFlagsMapper.ApplyLegacyCancelledFlags(
            row,
            "App_Unknown_Type",
            Visibility,
            legacyCancelled: true);

        Assert.False(row["InvitationItemIsCancelled"] as bool?);
        Assert.True(row["IsCancelled"] as bool?);
        Assert.False(row["VisaIsCancelled"] as bool?);
    }

    private static Dictionary<string, object?> CreateRow() =>
        new(StringComparer.Ordinal);
}
