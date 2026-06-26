using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class LegacyMaritalStatusLParserTests
{
    [Fact]
    public void Parse_SmoumaBelkhadem_SplitsSpouseAndChild()
    {
        const string statusL =
            "Aýaly-Smouma Belkhadem 21.06.1984ý.d.,Ogly-Mohamed, Yahya, Manessa Alqawasmeh 06.03.2016ý.d., (CAN)";

        var rows = LegacyMaritalStatusLParser.Parse(statusL);

        Assert.Equal(2, rows.Count);
        Assert.Equal("Smouma Belkhadem", rows[0].FullName);
        Assert.Equal(new DateTime(1984, 6, 21), rows[0].BirthDate);
        Assert.Equal("aýaly", rows[0].RelationshipNameTm);
        Assert.Equal("CAN", rows[0].CountryCode);

        Assert.Equal("Mohamed, Yahya, Manessa Alqawasmeh", rows[1].FullName);
        Assert.Equal(new DateTime(2016, 3, 6), rows[1].BirthDate);
        Assert.Equal("ogly", rows[1].RelationshipNameTm);
        Assert.Equal("CAN", rows[1].CountryCode);
    }

    [Fact]
    public void Parse_Ciccone_SplitsTwoChildren()
    {
        const string statusL =
            "Öýlenen, Aýaly - CICCONE FRANCESCA 26.09.1987, Ogly - ANDREA MATTIOLI 20.02.2015, (ITA)";

        var rows = LegacyMaritalStatusLParser.Parse(statusL);

        Assert.Equal(2, rows.Count);
        Assert.Equal("CICCONE FRANCESCA", rows[0].FullName);
        Assert.Equal("ANDREA MATTIOLI", rows[1].FullName);
        Assert.Equal("ITA", rows[0].CountryCode);
        Assert.Equal("ITA", rows[1].CountryCode);
    }

    [Fact]
    public void Parse_Bilgin_SplitsFourMembers()
    {
        const string statusL =
            "öýlenen, aýaly - Rahime BILGIN,28.06.1986ý.(TUR), gyzy-Aýşe BILGIN,08.01.2004ý.(TUR), ogly-Ali Ziyaddin BILGIN,03.07.2006ý.(TUR),gyzy-Zaferan BILGIN,16.12.2013ý.(TUR)";

        var rows = LegacyMaritalStatusLParser.Parse(statusL);

        Assert.Equal(4, rows.Count);
        Assert.All(rows, r => Assert.Equal("TUR", r.CountryCode));
    }

    [Fact]
    public void Parse_CanonicalLine_DoesNotUseLegacyParser()
    {
        const string canonical = "Smith John; 15.03.2010; ogly; TUR";

        Assert.False(LegacyMaritalStatusLParser.LooksLikeLegacyStatusL(canonical));

        var rows = VisaFamilyMemberLinesHelper.Parse(canonical);

        Assert.Single(rows);
        Assert.Equal("Smith John", rows[0].FullName);
    }

    [Fact]
    public void ToStorageText_ProducesEditorWireFormat()
    {
        const string statusL = "Aýaly-Melike Yazgan 30.01.1984ý.d., Ogly-Fatih Yazgan 26.01.2012ý.d., (TUR)";

        var storage = LegacyMaritalStatusLParser.ToStorageText(statusL);
        var rows = VisaFamilyMemberLinesHelper.Parse(storage);

        Assert.Equal(2, rows.Count);
        Assert.Equal("Melike Yazgan", rows[0].FullName);
        Assert.Equal("Fatih Yazgan", rows[1].FullName);
    }

    [Fact]
    public void Parse_RemovesParenthesesFromNames()
    {
        const string statusL = "Aýaly-Zeynep Şahin(01.01.1992, Ogly-Dewrim Şahin(31.05.2010, (TUR)";

        var rows = LegacyMaritalStatusLParser.Parse(statusL);

        Assert.Equal(2, rows.Count);
        Assert.Equal("Zeynep Şahin", rows[0].FullName);
        Assert.Equal("Dewrim Şahin", rows[1].FullName);
    }

    [Fact]
    public void SanitizeFamilyMemberFullName_StripsBrackets()
    {
        Assert.Equal("Zeynep Şahin", VisaFamilyMemberLinesHelper.SanitizeFamilyMemberFullName("Zeynep Şahin("));
        Assert.Equal("Valide", VisaFamilyMemberLinesHelper.SanitizeFamilyMemberFullName("Valide: ()"));
    }

    [Fact]
    public void IsLegacySingleMaritalStatus_StatusOne_IsSingle()
    {
        Assert.True(VisaFamilyMemberLinesHelper.IsLegacySingleMaritalStatus("1"));
        Assert.False(VisaFamilyMemberLinesHelper.IsLegacySingleMaritalStatus("2"));
    }
}
