using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public sealed class VisaFamilyMemberLinesHelperTests
{
    [Fact]
    public void Parse_manualOnly_noFallbackToMaster()
    {
        const string manual = "Ayşe Yılmaz; 12.10.1989; aýaly; TUR";

        var rows = VisaFamilyMemberLinesHelper.Parse(manual);

        Assert.Single(rows);
        Assert.Equal("Ayşe Yılmaz", rows[0].FullName);
    }

    [Fact]
    public void IsManualVisaFamilyEmpty_treatsYokAsEmpty()
    {
        Assert.True(VisaFamilyMemberLinesHelper.IsManualVisaFamilyEmpty(VisaFamilyMemberLinesHelper.NoneValue));
        Assert.True(VisaFamilyMemberLinesHelper.IsManualVisaFamilyEmpty(null));
        Assert.False(VisaFamilyMemberLinesHelper.IsManualVisaFamilyEmpty("Name; 01.01.2000; gyzy; TUR"));
    }

    [Fact]
    public void FindSpouseLine_matchesSpouseRelationshipName()
    {
        const string manual =
            "Child One; 01.01.2010; gyzy; TUR" + "\n" +
            "Spouse Person; 12.10.1989; aýaly; TUR";

        var spouse = VisaFamilyMemberLinesHelper.FindSpouseLine(manual, objectSpace: null);

        Assert.NotNull(spouse);
        Assert.Equal("Spouse Person", spouse!.FullName);
    }

    [Fact]
    public void SplitFullNameForPdf_splitsFirstTokenAndRemainder()
    {
        VisaFamilyMemberLinesHelper.SplitFullNameForPdf("John Smith Doe", out var first, out var last);

        Assert.Equal("John", first);
        Assert.Equal("Smith Doe", last);
    }

    [Fact]
    public void FormatSahsyKagyzFamilyStatus_returnsNullWhenManualIsYok()
    {
        Assert.Null(VisaFamilyMemberLinesHelper.FormatSahsyKagyzFamilyStatus(VisaFamilyMemberLinesHelper.NoneValue));
    }

    [Fact]
    public void FormatForVisaPdfMaritalFamilyBlock_returnsNullWhenManualIsYok()
    {
        Assert.Null(VisaFamilyMemberLinesHelper.FormatForVisaPdfMaritalFamilyBlock(VisaFamilyMemberLinesHelper.NoneValue));
    }
}
