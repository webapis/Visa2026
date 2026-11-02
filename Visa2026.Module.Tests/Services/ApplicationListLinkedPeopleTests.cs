using System;
using System.Collections.Generic;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationWorkspace;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationListLinkedPeopleTests
{
    [Fact]
    public void Build_marks_each_person_for_the_results_that_profile_produces()
    {
        var ali = Guid.NewGuid();
        var berdi = Guid.NewGuid();
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile
            {
                ProduceInvitation = true,
                ProduceVisa = true,
                ProduceRejection = true,
                ProduceWorkPermit = false,
                ProduceBorderZone = false,
            },
        };

        var rows = ApplicationListLinkedPeople.Build(
            application,
            [ali, berdi],
            new Dictionary<Guid, string>
            {
                [ali] = "Ali Aydogdy",
                [berdi] = "Berdi Annayev",
            },
            invitation: new HashSet<Guid> { ali },
            workPermit: new HashSet<Guid> { ali, berdi },
            visa: new HashSet<Guid> { berdi },
            borderZone: new HashSet<Guid>(),
            rejection: new HashSet<Guid> { berdi });

        Assert.Equal(2, rows.Count);
        Assert.Equal("Ali Aydogdy", rows[0].Name);
        Assert.Equal(
            [
                (ApplicationWorkspaceIssuedRecordsCatalog.Invitation, ApplicationListLinkedPeople.ToneDone),
                (ApplicationWorkspaceIssuedRecordsCatalog.IssuedVisa, ApplicationListLinkedPeople.ToneMissing),
            ],
            Marks(rows[0]));

        Assert.Equal("Berdi Annayev", rows[1].Name);
        Assert.Equal(
            [
                (ApplicationWorkspaceIssuedRecordsCatalog.Invitation, ApplicationListLinkedPeople.ToneMissing),
                (ApplicationWorkspaceIssuedRecordsCatalog.IssuedVisa, ApplicationListLinkedPeople.ToneDone),
                (ApplicationWorkspaceIssuedRecordsCatalog.Rejection, ApplicationListLinkedPeople.ToneRejected),
            ],
            Marks(rows[1]));
    }

    [Fact]
    public void Build_is_empty_when_the_profile_has_no_active_people()
    {
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile { ProduceInvitation = true },
        };

        Assert.Empty(ApplicationListLinkedPeople.Build(
            application,
            activeRoster: null,
            names: new Dictionary<Guid, string>(),
            invitation: new HashSet<Guid>(),
            workPermit: new HashSet<Guid>(),
            visa: new HashSet<Guid>(),
            borderZone: new HashSet<Guid>(),
            rejection: new HashSet<Guid>()));
    }

    [Fact]
    public void FormatName_uses_first_middle_last()
    {
        Assert.Equal("Ali Gurban Berdiyev", ApplicationListLinkedPeople.FormatName("Ali", "Gurban", "Berdiyev"));
        Assert.Equal("—", ApplicationListLinkedPeople.FormatName(" ", null, ""));
    }

    private static List<(string Key, string Tone)> Marks(ApplicationListLinkedPeople.Row row)
    {
        var marks = new List<(string Key, string Tone)>(row.Marks.Count);
        foreach (var mark in row.Marks)
            marks.Add((mark.Key, mark.Tone));
        return marks;
    }
}
