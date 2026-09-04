using System;
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
    public void HasConfigurationScalarsChanged_DetectsProduceVisaChange()
    {
        var original = new ApplicationProfile { ProduceVisa = true };
        var current = new ApplicationProfile { ProduceVisa = false };

        Assert.True(ApplicationProfileLockHelper.HasConfigurationScalarsChanged(original, current));
    }

    [Fact]
    public void HasConfigurationScalarsChanged_DetectsRegistrationKindChange()
    {
        var original = new ApplicationProfile
        {
            ActionFamily = ApplicationProfileActionFamily.Registration,
            RegistrationKind = ApplicationProfileRegistrationKind.CheckIn,
        };
        var current = new ApplicationProfile
        {
            ActionFamily = ApplicationProfileActionFamily.Registration,
            RegistrationKind = ApplicationProfileRegistrationKind.CheckOut,
        };

        Assert.True(ApplicationProfileLockHelper.HasConfigurationScalarsChanged(original, current));
    }

    [Fact]
    public void HasConfigurationScalarsChanged_IgnoresDefaultApprovalLegProfile()
    {
        var original = new ApplicationProfile { DefaultApprovalLegProfileId = Guid.NewGuid() };
        var current = new ApplicationProfile { DefaultApprovalLegProfileId = Guid.NewGuid() };

        Assert.False(ApplicationProfileLockHelper.HasConfigurationScalarsChanged(original, current));
    }

    [Fact]
    public void AllowsNestedEditWhenConfigLocked_VersionsYes_ExistingTemplateNo()
    {
        Assert.True(ApplicationProfileLockHelper.AllowsNestedEditWhenConfigLocked(new ApplicationProfileApprovalLegVersion()));
        Assert.True(ApplicationProfileLockHelper.AllowsNestedEditWhenConfigLocked(new ApplicationProfileApprovalLeg()));
        Assert.False(ApplicationProfileLockHelper.AllowsNestedEditWhenConfigLocked(new ApplicationProfileTemplate()));
        Assert.False(ApplicationProfileLockHelper.AllowsNestedEditWhenConfigLocked(new ApplicationProfileProgressStateSetting()));
    }

    [Fact]
    public void IsAllowedResminamalarRecycleBinMutation_AllowsRecycleFieldsOnly()
    {
        Assert.True(ApplicationProfileLockHelper.IsAllowedResminamalarRecycleBinMutation(
            isDelete: false,
            recycledAtUtc: DateTime.UtcNow,
            modifiedMemberNames: [nameof(ApplicationProfileTemplate.RecycledAtUtc), nameof(ApplicationProfileTemplate.RecycledByUserName)]));
        Assert.True(ApplicationProfileLockHelper.IsAllowedResminamalarRecycleBinMutation(
            isDelete: true,
            recycledAtUtc: DateTime.UtcNow,
            modifiedMemberNames: []));
        Assert.False(ApplicationProfileLockHelper.IsAllowedResminamalarRecycleBinMutation(
            isDelete: true,
            recycledAtUtc: null,
            modifiedMemberNames: []));
        Assert.False(ApplicationProfileLockHelper.IsAllowedResminamalarRecycleBinMutation(
            isDelete: false,
            recycledAtUtc: DateTime.UtcNow,
            modifiedMemberNames: [nameof(ApplicationProfileTemplate.TemplateName)]));
    }

    [Fact]
    public void IsAllowedResminamalarSharedIncludeMutation_AllowsSharedScopesOnly()
    {
        Assert.True(ApplicationProfileLockHelper.IsAllowedResminamalarSharedIncludeMutation(
            ApplicationProfileTemplateCatalogScope.Global));
        Assert.True(ApplicationProfileLockHelper.IsAllowedResminamalarSharedIncludeMutation(
            ApplicationProfileTemplateCatalogScope.Category));
        Assert.False(ApplicationProfileLockHelper.IsAllowedResminamalarSharedIncludeMutation(
            ApplicationProfileTemplateCatalogScope.ProfileSpecific));
    }

    [Fact]
    public void CanRemoveApprovalLegVersion_RequiresAnotherVersion()
    {
        var profile = new ApplicationProfile();
        var v1 = new ApplicationProfileApprovalLegVersion { Name = "Version 1", ApplicationProfile = profile };
        profile.ApprovalLegVersions.Add(v1);

        Assert.False(ApplicationProfileLockHelper.CanRemoveApprovalLegVersion(profile, v1));

        var v2 = new ApplicationProfileApprovalLegVersion { Name = "Version 2", ApplicationProfile = profile };
        profile.ApprovalLegVersions.Add(v2);

        Assert.True(ApplicationProfileLockHelper.CanRemoveApprovalLegVersion(profile, v1));
    }

    [Fact]
    public void BuildCloneCode_AppendsSuffixWithinMaxLength()
    {
        var code = ApplicationProfileCloneController.BuildCloneCode("app_inv", " - Copy");
        Assert.Contains("copy", code, StringComparison.OrdinalIgnoreCase);
        Assert.True(code.Length <= 64);
    }
}
