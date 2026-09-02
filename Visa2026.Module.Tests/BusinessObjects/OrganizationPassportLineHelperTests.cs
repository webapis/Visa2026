using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

public sealed class OrganizationPassportLineHelperTests
{
    [Fact]
    public void Format_AllParts_JoinsWithCommaAndYearSuffix()
    {
        var line = OrganizationPassportLineHelper.Format(
            " A123 ",
            " Ministry ",
            new DateTime(2024, 3, 5));

        Assert.Equal("A123, Ministry, 05.03.2024ý.", line);
    }

    [Fact]
    public void Format_MissingParts_OmitsEmptySegments()
    {
        Assert.Equal("A123", OrganizationPassportLineHelper.Format("A123", null, null));
        Assert.Equal("Ministry", OrganizationPassportLineHelper.Format(null, "Ministry", null));
        Assert.Equal("05.03.2024ý.", OrganizationPassportLineHelper.Format(null, null, new DateTime(2024, 3, 5)));
        Assert.Equal(string.Empty, OrganizationPassportLineHelper.Format(null, "  ", default(DateTime)));
    }
}
