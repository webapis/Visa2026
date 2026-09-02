using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.Services.UserReports;

public class UserReportSahsyKagyzNameTests
{
    [Theory]
    [InlineData("Sahsy kagyz")]
    [InlineData("SAHSY KAGYZ_117")]
    [InlineData("sahsy_kagyz.docx")]
    [InlineData("SAHSY_KAGYZ_117.docx")]
    public void LooksLikeSahsyKagyzName_matches_seed_and_officer_copies(string name) =>
        Assert.True(UserReportMergeDataHelper.LooksLikeSahsyKagyzName(name));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Sanaw_ckl")]
    [InlineData("Forma 16")]
    public void LooksLikeSahsyKagyzName_rejects_other_templates(string? name) =>
        Assert.False(UserReportMergeDataHelper.LooksLikeSahsyKagyzName(name));
}
