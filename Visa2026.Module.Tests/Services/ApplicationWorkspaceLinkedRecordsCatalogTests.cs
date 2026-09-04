using System;
using System.Collections.Generic;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationWorkspace;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationWorkspaceLinkedRecordsCatalogTests
{
    [Fact]
    public void IsConfigured_UsesProfileRequirePersonPassport()
    {
        var app = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile { RequirePersonPassport = true },
        };

        Assert.True(ApplicationWorkspaceLinkedRecordsCatalog.IsConfigured(app, ApplicationProfileInstancePersonLinkKind.Passport));
        Assert.False(ApplicationWorkspaceLinkedRecordsCatalog.IsConfigured(app, ApplicationProfileInstancePersonLinkKind.Visa));
    }

    [Fact]
    public void CountResolved_CountsStickyLinksPerKind()
    {
        var passportId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var personA = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var personB = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var links = new List<ApplicationProfileInstancePersonResolvedLink>
        {
            new()
            {
                PersonId = personA,
                LinkKind = ApplicationProfileInstancePersonLinkKind.Passport,
                LinkedObjectId = passportId,
            },
            new()
            {
                PersonId = personB,
                LinkKind = ApplicationProfileInstancePersonLinkKind.Passport,
                LinkedObjectId = Guid.NewGuid(),
            },
            new()
            {
                PersonId = personB,
                LinkKind = ApplicationProfileInstancePersonLinkKind.Visa,
                LinkedObjectId = Guid.NewGuid(),
            },
        };

        Assert.Equal(2, ApplicationWorkspaceLinkedRecordsCatalog.CountResolved(links, ApplicationProfileInstancePersonLinkKind.Passport));
        Assert.Equal(1, ApplicationWorkspaceLinkedRecordsCatalog.CountResolved(links, ApplicationProfileInstancePersonLinkKind.Visa));
        Assert.Equal(0, ApplicationWorkspaceLinkedRecordsCatalog.CountResolved(links, ApplicationProfileInstancePersonLinkKind.Education));
        Assert.Equal(1, ApplicationWorkspaceLinkedRecordsCatalog.CountResolvedForPerson(links, personA, ApplicationProfileInstancePersonLinkKind.Passport));
        Assert.Equal(0, ApplicationWorkspaceLinkedRecordsCatalog.CountResolvedForPerson(links, personA, ApplicationProfileInstancePersonLinkKind.Visa));
    }

    [Fact]
    public void Definitions_IncludesVisaAndRejectionPersonRecordKeys()
    {
        Assert.Contains(ApplicationWorkspaceLinkedRecordsCatalog.Definitions,
            d => d.TabKey == "visa" && d.PersonRecordKey == "visa");
        Assert.Contains(ApplicationWorkspaceLinkedRecordsCatalog.Definitions,
            d => d.TabKey == "rejection" && d.PersonRecordKey == "rejection");
    }
}
