using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

public class OrganizationPassportLineHelperTests
{
    [Fact]
    public void Format_AllParts_JoinsWithCommaAndYearSuffix()
    {
        var line = OrganizationPassportLineHelper.Format(
            " A1234567 ",
            " Ashgabat ",
            new DateTime(2024, 3, 15));

        Assert.Equal("A1234567, Ashgabat, 15.03.2024ý.", line);
    }

    [Fact]
    public void Format_NumberOnly_ReturnsTrimmedNumber()
    {
        Assert.Equal("P99", OrganizationPassportLineHelper.Format("P99", null, null));
    }

    [Fact]
    public void Format_SkipsDefaultIssueDate()
    {
        Assert.Equal(
            "P1, Auth",
            OrganizationPassportLineHelper.Format("P1", "Auth", default(DateTime)));
    }

    [Fact]
    public void Format_AllEmpty_ReturnsEmptyString()
    {
        Assert.Equal(string.Empty, OrganizationPassportLineHelper.Format(null, "  ", null));
    }
}
