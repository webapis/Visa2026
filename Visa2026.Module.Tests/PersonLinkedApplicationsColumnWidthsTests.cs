using Visa2026.Module;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Model;
using Xunit;

namespace Visa2026.Module.Tests;

public sealed class PersonLinkedApplicationsColumnWidthsTests
{
    [Theory]
    [InlineData(nameof(ApplicationProfileInstance.FullApplicationNumber), 180)]
    [InlineData(nameof(ApplicationProfileInstance.ProcessNumber), 160)]
    [InlineData(nameof(ApplicationProfileInstance.Year), 96)]
    [InlineData(nameof(ApplicationProfileInstance.MonthName), 120)]
    [InlineData(nameof(ApplicationProfileInstance.LatestProgressDate), 170)]
    [InlineData("OtherColumn", PersonLinkedApplicationsColumnWidths.FallbackMinWidth)]
    public void ResolveWidth_KeepsCaptionsWiderThanOneWord(string propertyName, int expected)
    {
        Assert.Equal(expected, PersonLinkedApplicationsColumnWidths.ResolveWidth(propertyName));
        Assert.True(expected >= 96);
    }

    [Fact]
    public void UsesOwnWidth_LeavesStepperAndResultColumnsAlone()
    {
        Assert.True(PersonLinkedApplicationsColumnWidths.UsesOwnWidth(
            nameof(ApplicationProfileInstance.ProgressStepsDisplay)));
        Assert.True(PersonLinkedApplicationsColumnWidths.UsesOwnWidth(
            nameof(ApplicationProfileInstance.ResultCoverageDisplay)));
        Assert.False(PersonLinkedApplicationsColumnWidths.UsesOwnWidth(
            nameof(ApplicationProfileInstance.FullApplicationNumber)));
    }

    [Fact]
    public void SkipAutoFit_OnlyPersonLinkedApplicationsList()
    {
        Assert.True(PersonLinkedApplicationsColumnWidths.SkipAutoFit(
            PersonNestedCollectionLayout.ApplicationProfileInstancesListView));
        Assert.False(PersonLinkedApplicationsColumnWidths.SkipAutoFit(
            "ApplicationProfileInstance_ListView"));
    }
}
