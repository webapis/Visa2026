using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

public class ApplicationProgressResultNumberHelperTests
{
    [Theory]
    [InlineData("1_REVIEW_APPROVED", true)]
    [InlineData("2_REVIEW_REJECTED", true)]
    [InlineData("1_REVIEW_STARTED", false)]
    [InlineData("PROCESS_CANCELLED", false)]
    [InlineData("PROCESS_STARTED", false)]
    public void AppliesTo_IsMinistryDecisionOnly(string stateCode, bool expected) =>
        Assert.Equal(expected, ApplicationProgressResultNumberHelper.AppliesTo(stateCode));

    [Fact]
    public void TryNormalize_Required_RejectsBlank()
    {
        var ok = ApplicationProgressResultNumberHelper.TryNormalize("  ", required: true, out var normalized, out var error);

        Assert.False(ok);
        Assert.Null(normalized);
        Assert.Equal(
            VisaUiMessages.Get("ApplicationProfileInstance.Workspace.ResultNumberRequired"),
            error);
    }

    [Fact]
    public void TryNormalize_TrimsAndAllowsOptionalBlank()
    {
        Assert.True(ApplicationProgressResultNumberHelper.TryNormalize("  12/2026  ", required: true, out var normalized, out var error));
        Assert.Equal("12/2026", normalized);
        Assert.Null(error);

        Assert.True(ApplicationProgressResultNumberHelper.TryNormalize(null, required: false, out normalized, out error));
        Assert.Null(normalized);
        Assert.Null(error);
    }

    [Fact]
    public void TryNormalize_RejectsOverMaxLength()
    {
        var ok = ApplicationProgressResultNumberHelper.TryNormalize(
            new string('A', ApplicationProgressResultNumberHelper.MaxLength + 1),
            required: true,
            out var normalized,
            out var error);

        Assert.False(ok);
        Assert.Null(normalized);
        Assert.Equal(
            VisaUiMessages.Get("ApplicationProfileInstance.Workspace.ResultNumberTooLong"),
            error);
    }
}