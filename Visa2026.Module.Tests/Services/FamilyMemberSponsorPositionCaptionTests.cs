using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;
using Visa2026.Module.Services.ApplicationWorkspace;
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
}