using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

public class Visa2014FamilyMembersTextMapperTests
{
    [Fact]
    public void FromLegacyStatusL_SingleMaritalStatus_ReturnsNoneValue()
    {
        var result = Visa2014FamilyMembersTextMapper.FromLegacyStatusL(
            statusL: "Aýaly-Someone 01.01.1990ý.d., (TUR)",
            legacyMaritalStatusStatus: "1");

        Assert.Equal(VisaFamilyMemberLinesHelper.NoneValue, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FromLegacyStatusL_BlankStatusL_ReturnsNull(string? statusL)
    {
        Assert.Null(Visa2014FamilyMembersTextMapper.FromLegacyStatusL(statusL, legacyMaritalStatusStatus: "2"));
    }

    [Theory]
    [InlineData(".")]
    [InlineData("-")]
    [InlineData("0")]
    [InlineData("Ýok")]
    [InlineData("Yok")]
    [InlineData("Sallah")]
    [InlineData(" yok ")]
    public void FromLegacyStatusL_IgnoredLiterals_ReturnsNull(string statusL)
    {
        Assert.Null(Visa2014FamilyMembersTextMapper.FromLegacyStatusL(statusL, legacyMaritalStatusStatus: "2"));
    }

    [Fact]
    public void FromLegacyStatusL_Narrative_ConvertsToEditorStorageText()
    {
        const string statusL = "Aýaly-Melike Yazgan 30.01.1984ý.d., Ogly-Fatih Yazgan 26.01.2012ý.d., (TUR)";

        var storage = Visa2014FamilyMembersTextMapper.FromLegacyStatusL(statusL, legacyMaritalStatusStatus: "2");
        Assert.False(string.IsNullOrWhiteSpace(storage));

        var rows = VisaFamilyMemberLinesHelper.Parse(storage);
        Assert.Equal(2, rows.Count);
        Assert.Equal("Melike Yazgan", rows[0].FullName);
        Assert.Equal("Fatih Yazgan", rows[1].FullName);
    }
}
