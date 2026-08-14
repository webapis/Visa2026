using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Controllers;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

public class ApplicationProfileLockHelperTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("IS_BEING_PREPARED", false)]
    [InlineData("OFFICE_PREPARATION", false)]
    [InlineData("DRAFT", false)]
    [InlineData("REVIEW_1_STARTED", true)]
    [InlineData("PROCESS_STARTED", true)]
    public void IsPrimaryStateAtOrPastLockStateA_RecognizesOfficeVsSubmitted(string? code, bool expected) =>
        Assert.Equal(expected, ApplicationProfileLockHelper.IsPrimaryStateAtOrPastLockStateA(code));

    [Fact]
    public void IsApplicationAtOrPastLockStateA_UsesLatestPrimaryStateCode()
    {
        var app = new ApplicationProfileInstance { LatestPrimaryStateCode = "REVIEW_1_STARTED" };
        Assert.True(ApplicationProfileLockHelper.IsApplicationAtOrPastLockStateA(app));
    }

    [Fact]
    public void IsProfileConfigLocked_UsesLinkedApplicationsCollection()
    {
        var profile = new ApplicationProfile();
        var app = new ApplicationProfileInstance
        {
            ApplicationProfile = profile,
            LatestPrimaryStateCode = "PROCESS_STARTED",
        };
        profile.Instances = [app];

        Assert.True(ApplicationProfileLockHelper.IsProfileConfigLocked(profile));
    }

    [Fact]
    public void HasConfigurationScalarsChanged_DetectsActionFamilyChange()
    {
        var original = new ApplicationProfile { ActionFamily = ApplicationProfileActionFamily.Issuance };
        var current = new ApplicationProfile { ActionFamily = ApplicationProfileActionFamily.Cancellation };

        Assert.True(ApplicationProfileLockHelper.HasConfigurationScalarsChanged(original, current));
    }

    [Fact]
    public void BuildCloneCode_AppendsSuffixWithinMaxLength()
    {
        var code = ApplicationProfileCloneController.BuildCloneCode("app_inv", " - Copy");
        Assert.Contains("copy", code, StringComparison.OrdinalIgnoreCase);
        Assert.True(code.Length <= 64);
    }
}
