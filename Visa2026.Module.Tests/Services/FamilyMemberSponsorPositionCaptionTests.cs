using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;
using Visa2026.Module.Services.ApplicationWorkspace;
using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class FamilyMemberSponsorPositionCaptionTests
{
    [Fact]
    public void Employee_does_not_use_sponsor_caption()
    {
        var person = new Person { IsEmployee = true };
        Assert.False(FamilyMemberSponsorPositionCaption.UsesSponsorPosition(person));
        Assert.True(FamilyMemberSponsorPositionCaption.IsComplete(person));
    }

    [Fact]
    public void Dependent_without_sponsor_is_incomplete()
    {
        var person = new Person { IsEmployee = false };
        Assert.True(FamilyMemberSponsorPositionCaption.UsesSponsorPosition(person));
        Assert.False(FamilyMemberSponsorPositionCaption.IsComplete(person));
        Assert.Equal(string.Empty, FamilyMemberSponsorPositionCaption.FormatTm(person));
    }

    [Fact]
    public void Completeness_caption_short_when_sponsor_line_missing()
    {
        var record = new ApplicationWorkspaceCasePersonRecord
        {
            Key = "position",
            Count = 0,
            ExpectedCount = 1,
            IsCaptionOnly = true,
        };
        Assert.True(ApplicationWorkspacePeopleLinksCompleteness.IsRecordShort(record));
    }

    [Fact]
    public void Completeness_caption_ok_when_sponsor_line_present()
    {
        var record = new ApplicationWorkspaceCasePersonRecord
        {
            Key = "position",
            Count = 1,
            ExpectedCount = 1,
            IsCaptionOnly = true,
        };
        Assert.False(ApplicationWorkspacePeopleLinksCompleteness.IsRecordShort(record));
    }

    [Fact]
    public void Sanawy_row_includes_FMWZP_alias_for_FM_Wezipesi()
    {
        var sponsor = new Person
        {
            IsEmployee = true,
            FirstName = "İzzet",
            LastName = "Taşdelen",
        };
        var dependent = new Person
        {
            IsEmployee = false,
            PersonRole = PersonRecordRole.FamilyMember,
            SponsoringEmployee = sponsor,
            Relationship = new Relationship { NameTm = "aýaly" },
        };
        var line = new ApplicationRosterMergeLine { Person = dependent };
        var row = UserReportMergeDataHelper.BuildSanawyRowDictionary(line, 1);
        Assert.True(row.ContainsKey("FM_WezipesiTm"));
        Assert.True(row.ContainsKey("FMWZP"));
        Assert.Equal(row["FM_WezipesiTm"], row["FMWZP"]);
    }
}