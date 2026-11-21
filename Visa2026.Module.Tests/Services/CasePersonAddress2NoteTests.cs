using System;
using System.Collections.ObjectModel;
using System.Linq;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;
using Visa2026.Module.Services.ApplicationPersonRoster;
using Visa2026.Module.Services.ApplicationWorkspace;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class CasePersonAddress2NoteTests
{
    [Fact]
    public void Build_CaseSummary_Address2IsOptionalText_NotCatalog()
    {
        var profile = new ApplicationProfile
        {
            RequireAddress1 = true,
            RequireAddress2 = true,
            PersonAddressOfResidenceMaxCount = 2,
        };
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = profile,
            Address2DefaultText = "Case default note",
        };
        var fields = ApplicationWorkspaceCaseHeaderFieldsHelper.Build(application, profile, null);

        var address1 = Assert.Single(fields, item => item.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.Address1);
        Assert.Equal(ApplicationWorkspaceCaseHeaderFieldKind.Text, address1.Kind);
        Assert.True(ApplicationWorkspaceCaseHeaderFieldsHelper.IsAddress1DefaultTextField(address1.Key));
        Assert.False(ApplicationWorkspaceCaseHeaderFieldsHelper.IsAddressObjectField(address1.Key));

        var address2 = Assert.Single(fields, item => item.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.Address2);
        Assert.Equal(ApplicationWorkspaceCaseHeaderFieldKind.Text, address2.Kind);
        Assert.Equal("Case default note", address2.Value);
        Assert.Equal(ApplicationWorkspaceCaseSummaryFillState.Officer, address2.FillState);
        Assert.True(ApplicationWorkspaceCaseHeaderFieldsHelper.IsAddress2DefaultTextField(address2.Key));
        Assert.False(ApplicationWorkspaceCaseHeaderFieldsHelper.IsAddressObjectField(address2.Key));
    }

    [Fact]
    public void Build_CaseSummary_HidesAddress2_WhenTemplateDoesNotRequire()
    {
        var profile = new ApplicationProfile
        {
            RequireAddress1 = true,
            RequireAddress2 = false,
        };
        var application = new ApplicationProfileInstance { ApplicationProfile = profile };
        var fields = ApplicationWorkspaceCaseHeaderFieldsHelper.Build(application, profile, null);
        Assert.DoesNotContain(fields, item => item.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.Address2);
    }

    [Fact]
    public void AddressSlotLines_WhenMaxSlotsTwo_EmitsFreeTextAddress1And2()
    {
        var profile = new ApplicationProfile
        {
            RequireAddress1 = true,
            RequireAddress2 = true,
            PersonAddressOfResidenceMaxCount = 2,
        };
        var application = new ApplicationProfileInstance
        {
            ID = Guid.NewGuid(),
            ApplicationProfile = profile,
            PersonAddress2Notes = new ObservableCollection<ApplicationProfileInstancePersonAddress2Note>(),
            PersonResolvedLinks = new ObservableCollection<ApplicationProfileInstancePersonResolvedLink>(),
        };
        var person = new Person { ID = Guid.NewGuid() };

        var lines = PersonLinkReview.AddressSlotLines(null!, application, person, "Address");

        Assert.Equal(2, lines.Count);
        Assert.True(lines[0].UsesMultilineText);
        Assert.Equal(1, lines[0].SlotIndex);
        Assert.Equal("Address 1", lines[0].Label);
        Assert.Empty(lines[0].Choices);
        Assert.Equal(Guid.Empty, lines[0].ObjectId);
        Assert.True(lines[1].UsesMultilineText);
        Assert.Equal(2, lines[1].SlotIndex);
        Assert.Equal("Address 2", lines[1].Label);
        Assert.Equal("AddressOfResidence:2", lines[1].ResolveTabKey());
        Assert.Empty(lines[1].Choices);
        Assert.Equal(Guid.Empty, lines[1].ObjectId);
    }

    [Fact]
    public void Address2Notes_AreScopedPerPersonOnInstance()
    {
        var applicationId = Guid.NewGuid();
        var personA = Guid.NewGuid();
        var personB = Guid.NewGuid();
        var application = new ApplicationProfileInstance
        {
            ID = applicationId,
            PersonAddress2Notes = new ObservableCollection<ApplicationProfileInstancePersonAddress2Note>
            {
                new()
                {
                    ApplicationProfileInstanceId = applicationId,
                    PersonId = personA,
                    Address2Text = "Note A",
                },
                new()
                {
                    ApplicationProfileInstanceId = applicationId,
                    PersonId = personB,
                    Address2Text = "Note B",
                },
            },
        };

        var forA = application.PersonAddress2Notes.First(n => n.PersonId == personA).Address2Text;
        var forB = application.PersonAddress2Notes.First(n => n.PersonId == personB).Address2Text;
        Assert.Equal("Note A", forA);
        Assert.Equal("Note B", forB);
        Assert.DoesNotContain(application.PersonAddress2Notes, n => n.PersonId == personA && n.Address2Text == "Note B");
    }

    [Fact]
    public void Upsert_ReturnsFalse_WhenObjectSpaceOrIdsMissing()
    {
        var application = new ApplicationProfileInstance { ID = Guid.NewGuid() };
        var person = new Person { ID = Guid.NewGuid() };
        Assert.False(CasePersonAddress2Note.Upsert(null!, application, person, "x"));
        Assert.False(CasePersonAddress2Note.Upsert(null!, application, new Person { ID = Guid.Empty }, "x"));
        Assert.False(CasePersonAddress2Note.Upsert(null!, new ApplicationProfileInstance { ID = Guid.Empty }, person, "x"));
    }

    [Fact]
    public void SetInstanceSummaryAddress_Slot2_ClearsObsoleteFk()
    {
        var application = new ApplicationProfileInstance();
#pragma warning disable CS0618
        application.Address2 = new AddressOfResidence();
        ApplicationProfileInstanceAddressResolver.SetInstanceSummaryAddress(
            application, ApplicationProfileInstanceAddressResolver.Slot2, new AddressOfResidence());
        Assert.Null(application.Address2);
#pragma warning restore CS0618
    }

    [Fact]
    public void MergeLine_Address2_FullAddress_UsesNoteText_StructuredEmpty()
    {
        var line = new ApplicationRosterMergeLine { Address2Text = "Building 4\nApt 12" };
        Assert.Equal("Building 4\nApt 12", line.Address2_FullAddress);
        Assert.Equal(string.Empty, line.Address2_Type);
        Assert.Equal(string.Empty, line.Address2_RegionTm);
        Assert.Equal(string.Empty, line.Address2_CityTm);
        Assert.Null(line.Address2_ExpirationDate);
        Assert.Equal(string.Empty, line.Address2_ExpirationDateText);
    }

    [Fact]
    public void MergeLine_Address1_FullAddress_PrefersFreeTextNote()
    {
        var line = new ApplicationRosterMergeLine
        {
            Address1Text = "Free text address one",
        };
        Assert.Equal("Free text address one", line.Address1_FullAddress);
        Assert.Equal(string.Empty, line.Address1_Type);
    }

    [Fact]
    public void Build_CaseSummary_Address1IsRequiredFreeText_NotCatalog()
    {
        var profile = new ApplicationProfile
        {
            RequireAddress1 = true,
            RequireAddress2 = false,
        };
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = profile,
            Address1DefaultText = "Case address one",
        };
        var fields = ApplicationWorkspaceCaseHeaderFieldsHelper.Build(application, profile, null);
        var address1 = Assert.Single(fields, item => item.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.Address1);
        Assert.Equal(ApplicationWorkspaceCaseHeaderFieldKind.Text, address1.Kind);
        Assert.Equal("Case address one", address1.Value);
        Assert.Equal(ApplicationWorkspaceCaseSummaryFillState.Officer, address1.FillState);
        Assert.True(ApplicationWorkspaceCaseHeaderFieldsHelper.IsAddressDefaultTextField(address1.Key));
    }

    [Fact]
    public void SetInstanceSummaryAddress_Slot1_ClearsObsoleteFk()
    {
        var application = new ApplicationProfileInstance();
#pragma warning disable CS0618
        application.Address1 = new AddressOfResidence();
        ApplicationProfileInstanceAddressResolver.SetInstanceSummaryAddress(
            application, ApplicationProfileInstanceAddressResolver.Slot1, new AddressOfResidence());
        Assert.Null(application.Address1);
#pragma warning restore CS0618
    }
}
