using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Covers employee default / SahsyKagyz edges for manual-only visa family text.
/// Separate file from <see cref="VisaFamilyMemberLinesHelperTests"/> to avoid open coverage-PR conflicts.
/// </summary>
public sealed class VisaFamilyMemberLinesHelperManualDefaultsTests
{
    [Fact]
    public void ApplyEmployeeDefaultIfEmpty_SetsYokWhenBlankForEmployee()
    {
        var person = new Person
        {
            IsEmployee = true,
            PersonRole = PersonRecordRole.Employee,
            VisaApplicationFamilyMembersText = null,
        };

        VisaFamilyMemberLinesHelper.ApplyEmployeeDefaultIfEmpty(person);

        Assert.Equal(VisaFamilyMemberLinesHelper.NoneValue, person.VisaApplicationFamilyMembersText);
    }

    [Fact]
    public void ApplyEmployeeDefaultIfEmpty_SingleMaritalStatus_ForcesYok()
    {
        var person = new Person
        {
            IsEmployee = true,
            PersonRole = PersonRecordRole.Employee,
            MaritalStatus = new MaritalStatus { Code = "Sallah", LocalizationKey = "Single" },
            VisaApplicationFamilyMembersText = "Someone; 01.01.2000; gyzy; TUR",
        };

        VisaFamilyMemberLinesHelper.ApplyEmployeeDefaultIfEmpty(person);

        Assert.Equal(VisaFamilyMemberLinesHelper.NoneValue, person.VisaApplicationFamilyMembersText);
    }

    [Fact]
    public void ApplyEmployeeDefaultIfEmpty_NonEmployee_IsNoOp()
    {
        var person = new Person
        {
            IsEmployee = false,
            PersonRole = PersonRecordRole.FamilyMember,
            VisaApplicationFamilyMembersText = null,
        };

        VisaFamilyMemberLinesHelper.ApplyEmployeeDefaultIfEmpty(person);

        Assert.Null(person.VisaApplicationFamilyMembersText);
    }

    [Fact]
    public void ApplyEmployeeDefaultIfEmpty_PreservesExistingManualLines()
    {
        const string existing = "Ayşe; 12.10.1989; aýaly; TUR";
        var person = new Person
        {
            IsEmployee = true,
            PersonRole = PersonRecordRole.Employee,
            VisaApplicationFamilyMembersText = existing,
        };

        VisaFamilyMemberLinesHelper.ApplyEmployeeDefaultIfEmpty(person);

        Assert.Equal(existing, person.VisaApplicationFamilyMembersText);
    }

    [Fact]
    public void FormatSahsyKagyzFamilyStatus_FormatsManualLine()
    {
        const string manual = "Ayşe Yılmaz; 12.10.1989; aýaly; TUR";
        var text = VisaFamilyMemberLinesHelper.FormatSahsyKagyzFamilyStatus(manual);

        Assert.NotNull(text);
        Assert.Contains("aýaly-Ayşe Yılmaz 12.10.1989ý. TUR.", text, StringComparison.Ordinal);
    }

    [Fact]
    public void FindSpouseLine_ReturnsNullWhenNoSpouse()
    {
        const string manual = "Child One; 01.01.2010; gyzy; TUR";
        Assert.Null(VisaFamilyMemberLinesHelper.FindSpouseLine(manual, objectSpace: null));
    }

    [Fact]
    public void IsLegacySingleMaritalStatus_MatchesLegacyOne()
    {
        Assert.True(VisaFamilyMemberLinesHelper.IsLegacySingleMaritalStatus("1"));
        Assert.True(VisaFamilyMemberLinesHelper.IsLegacySingleMaritalStatus(" 1 "));
        Assert.False(VisaFamilyMemberLinesHelper.IsLegacySingleMaritalStatus("2"));
        Assert.False(VisaFamilyMemberLinesHelper.IsLegacySingleMaritalStatus(null));
    }
}
