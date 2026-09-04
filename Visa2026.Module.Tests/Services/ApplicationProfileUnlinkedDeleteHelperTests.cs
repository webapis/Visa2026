using Visa2026.Module.Services.ApplicationProfileCatalog;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationProfileUnlinkedDeleteHelperTests
{
    [Theory]
    [InlineData(0, true)]
    [InlineData(-1, true)]
    [InlineData(1, false)]
    [InlineData(12, false)]
    public void CanDelete_OnlyWhenNoLinkedInstances(int linked, bool expected) =>
        Assert.Equal(expected, ApplicationProfileUnlinkedDeleteHelper.CanDelete(linked));
}