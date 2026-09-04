#nullable enable

using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.Services.UserReports;

public class UserReportLooseRowTokenPromoterTests
{
    private sealed class RowStub
    {
        public string Person_FullName { get; set; } = "Hilmi Erol";
    }

    [Fact]
    public void Promote_copies_first_row_fields_onto_ds_when_no_loop()
    {
        var data = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        UserReportMergeDataHelper.PromoteLooseRowTokensOntoRoot(
            [".PFN", "ADAT"],
            data,
            new RowStub(),
            applicationItems: null);

        Assert.Equal("Hilmi Erol", data["PFN"]);
        Assert.Equal("Hilmi Erol", data["Person_FullName"]);
    }

    [Fact]
    public void Promote_skips_when_rows_loop_is_present()
    {
        var data = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        UserReportMergeDataHelper.PromoteLooseRowTokensOntoRoot(
            [".PFN", "#ds.rows"],
            data,
            new RowStub(),
            applicationItems: null);

        Assert.Empty(data);
    }
}
