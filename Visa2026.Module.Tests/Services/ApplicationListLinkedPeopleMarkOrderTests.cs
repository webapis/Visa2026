using System;
using System.Collections.Generic;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationWorkspace;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Extra edges for list-row people marks: name sort, rejection-only-when-present,
/// and LoadNames empty when ObjectSpace is missing.
/// </summary>
public sealed class ApplicationListLinkedPeopleMarkOrderTests
{
    [Fact]
    public void Build_sorts_rows_by_display_name()
    {
        var zebra = Guid.NewGuid();
        var alpha = Guid.NewGuid();
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile { ProduceVisa = true },
        };

        var rows = ApplicationListLinkedPeople.Build(
            application,
            [zebra, alpha],
            new Dictionary<Guid, string>
            {
                [zebra] = "Zebra",
                [alpha] = "Alpha",
            },
            invitation: new HashSet<Guid>(),
            workPermit: new HashSet<Guid>(),
            visa: new HashSet<Guid> { alpha },
            borderZone: new HashSet<Guid>(),
            rejection: new HashSet<Guid>());

        Assert.Equal(["Alpha", "Zebra"], rows.Select(r => r.Name).ToArray());
    }

    [Fact]
    public void Build_omits_rejection_mark_when_the_person_has_no_rejection()
    {
        var personId = Guid.NewGuid();
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile
            {
                ProduceInvitation = true,
                ProduceRejection = true,
            },
        };

        var rows = ApplicationListLinkedPeople.Build(
            application,
            [personId],
            new Dictionary<Guid, string> { [personId] = "Ali" },
            invitation: new HashSet<Guid>(),
            workPermit: new HashSet<Guid>(),
            visa: new HashSet<Guid>(),
            borderZone: new HashSet<Guid>(),
            rejection: new HashSet<Guid>());

        Assert.Single(rows);
        Assert.DoesNotContain(
            rows[0].Marks,
            m => m.Key == ApplicationWorkspaceIssuedRecordsCatalog.Rejection);
        Assert.Contains(
            rows[0].Marks,
            m => m.Key == ApplicationWorkspaceIssuedRecordsCatalog.Invitation
                && m.Tone == ApplicationListLinkedPeople.ToneMissing);
    }

    [Fact]
    public void LoadNames_returns_empty_when_object_space_or_roster_is_empty()
    {
        Assert.Empty(ApplicationListLinkedPeople.LoadNames(null, new Dictionary<Guid, HashSet<Guid>>()));
        Assert.Empty(ApplicationListLinkedPeople.LoadNames(
            null,
            new Dictionary<Guid, HashSet<Guid>>
            {
                [Guid.NewGuid()] = new HashSet<Guid> { Guid.NewGuid() },
            }));
    }
}
