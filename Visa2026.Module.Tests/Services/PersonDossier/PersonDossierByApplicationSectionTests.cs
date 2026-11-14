using System;
using System.Linq;
using DevExpress.Persistent.BaseImpl.EF;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationWorkspace;
using Visa2026.Module.Services.PersonDossier;
using Xunit;

namespace Visa2026.Module.Tests.Services.PersonDossier;

public class PersonDossierByApplicationSectionTests
{
    [Fact]
    public void Groups_documents_by_the_case_that_issued_them_newest_first()
    {
        var invitationCase = new ApplicationProfileInstance
        {
            ID = Guid.NewGuid(),
            FullApplicationNumber = "CE2026-0100",
            ApplicationDate = new DateTime(2026, 1, 10),
            ApplicationProfile = new ApplicationProfile
            {
                Name = "Work visa",
                ProduceInvitation = true,
                ProduceVisa = true,
                ProduceWorkPermit = true,
            },
        };
        var visaCase = new ApplicationProfileInstance
        {
            ID = Guid.NewGuid(),
            FullApplicationNumber = "CE2026-0200",
            ApplicationDate = new DateTime(2026, 3, 1),
            ApplicationProfile = new ApplicationProfile { Name = "Visa issue", ProduceVisa = true },
        };
        var visaPassport = new Passport
        {
            ID = Guid.NewGuid(),
            PassportNumber = "P-VISA",
            ExpirationDate = DateTime.Today.AddYears(4),
        };
        var invitationPassport = new Passport
        {
            ID = Guid.NewGuid(),
            PassportNumber = "X7192908",
            ExpirationDate = new DateTime(2031, 6, 26),
        };
        var visa = new Visa
        {
            ID = Guid.NewGuid(),
            VisaNumber = "A1731526",
            ExpirationDate = DateTime.Today.AddDays(45),
            IssuingApplicationProfileInstance = visaCase,
        };
        var person = new Person();
        person.InvitationItems.Add(new InvitationItem
        {
            ID = Guid.NewGuid(),
            IssuedVisa = visa,
            Passport = invitationPassport,
            Invitation = new Invitation
            {
                ID = Guid.NewGuid(),
                InvitationNumber = "1602/26",
                IssuedDate = new DateTime(2026, 1, 10),
                ExpirationDate = new DateTime(2026, 4, 11),
                ApplicationProfileInstance = invitationCase,
            },
        });
        visaPassport.Visas.Add(visa);
        person.Passports.Add(visaPassport);
        person.Passports.Add(invitationPassport);
        person.WorkPermitItems.Add(new WorkPermitItem
        {
            ID = Guid.NewGuid(),
            WorkPermitNumber = "WP-88",
            StartDate = new DateTime(2026, 1, 12),
            ExpirationDate = new DateTime(2027, 1, 12),
            WorkPermit = new WorkPermit
            {
                ID = Guid.NewGuid(),
                ApplicationProfileInstance = invitationCase,
            },
        });

        var section = PersonDossierResolver.BuildByApplication(person, usedInvitationItemIds: null);
        var visas = PersonDossierResolver.BuildVisaCases(person, usedInvitationItemIds: null);

        Assert.Equal("byApplication", section.SectionId);
        Assert.Equal("Invitations", section.SectionLabel);
        Assert.True(section.HideStatusColumn);
        Assert.False(section.ShowsStatusColumn);
        Assert.Equal("CE2026-0100", Assert.Single(section.Records).Cells[0]);
        Assert.Equal("CE2026-0200", Assert.Single(visas.Records).Cells[0]);

        var visaRow = visas.Records[0];
        Assert.Equal(visaCase.ID, visaRow.ApplicationLinkId);
        Assert.Equal("Visa issue", visaRow.FirstCellDetail);
        Assert.Equal("P-VISA", visaRow.Cells[1]);
        Assert.Equal("\u2014", visaRow.Cells[2]);
        Assert.Equal("A1731526", visaRow.Cells[3]);
        Assert.Equal("\u2014", visaRow.Cells[4]);
        Assert.Equal("P-VISA", visaRow.CellChips[1]!.Number);
        Assert.StartsWith("Valid to", visaRow.CellChips[1]!.StatusLabel, StringComparison.OrdinalIgnoreCase);
        Assert.Null(visaRow.CellChips[2]);
        Assert.Equal("A1731526", visaRow.CellChips[3]!.Number);
        Assert.Equal("Expires in 45 days", visaRow.CellChips[3]!.StatusLabel);
        Assert.False(visaRow.CellChips[3]!.AppendIssuedVisaBesidePassport);
        Assert.Null(visaRow.CellChips[4]);

        var invitationRow = section.Records[0];
        Assert.Equal(invitationCase.ID, invitationRow.ApplicationLinkId);
        Assert.Equal("Work visa", invitationRow.FirstCellDetail);
        Assert.Equal("X7192908", invitationRow.Cells[1]);
        Assert.Equal("1602/26", invitationRow.Cells[2]);
        Assert.Equal("\u2014", invitationRow.Cells[3]);
        Assert.Equal("WP-88", invitationRow.Cells[4]);
        Assert.StartsWith("Valid to", invitationRow.CellChips[1]!.StatusLabel, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Used", invitationRow.CellChips[2]!.StatusLabel);
        Assert.True(invitationRow.CellChips[2]!.AppendIssuedVisaBesidePassport);
        Assert.Null(invitationRow.CellChips[3]);
        Assert.Equal("WP-88", invitationRow.CellChips[4]!.Number);
        Assert.StartsWith("Valid to", invitationRow.CellChips[4]!.StatusLabel, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Keeps_the_later_invitation_when_one_case_issued_two()
    {
        var application = new ApplicationProfileInstance
        {
            ID = Guid.NewGuid(),
            FullApplicationNumber = "CE2026-0300",
            ApplicationDate = new DateTime(2026, 2, 1),
            ApplicationProfile = new ApplicationProfile { ProduceInvitation = true },
        };
        var person = new Person();
        person.InvitationItems.Add(Invitation("100/26", new DateTime(2026, 5, 1), application));
        person.InvitationItems.Add(Invitation("200/26", new DateTime(2026, 8, 1), application));

        var section = PersonDossierResolver.BuildByApplication(person, usedInvitationItemIds: null);
        var row = Assert.Single(section.Records);

        Assert.Equal("\u2014", row.Cells[1]);
        Assert.Equal("200/26", row.Cells[2]);
        Assert.Equal("\u2014", row.Cells[3]);
        Assert.Equal("\u2014", row.Cells[4]);
    }

    [Fact]
    public void Approval_letters_follow_process_order_and_name_missing_steps()
    {
        var steps = new[]
        {
            new ApplicationWorkspaceCaseProgressStep
            {
                Key = "office",
                Label = "Office preparation",
                FileSlot = 0,
                MinistryLetterFileName = "office.pdf",
                Files = new[] { new ApplicationProfileInstanceProgressStoredFile(Guid.NewGuid(), "office.pdf") },
            },
            new ApplicationWorkspaceCaseProgressStep
            {
                Key = "leg-1",
                Label = "Turkmenenergo",
                FileSlot = 0,
                MinistryLetterFileName = "tng.pdf",
                Files = new[] { new ApplicationProfileInstanceProgressStoredFile(Guid.NewGuid(), "tng.pdf") },
            },
            new ApplicationWorkspaceCaseProgressStep
            {
                Key = "leg-2",
                Label = "Energetika",
                FileSlot = 0,
                MissingMinistryLetter = true,
            },
        };

        var (hasLetters, missing) = PersonDossierResolver.ReadApprovalLetters(steps);

        Assert.True(hasLetters);
        var missingStep = Assert.Single(missing);
        Assert.Equal("Energetika", missingStep);
    }

    [Fact]
    public void Carries_the_case_progress_track_for_the_row()
    {
        var application = new ApplicationProfileInstance
        {
            ID = Guid.NewGuid(),
            FullApplicationNumber = "CE2026-0100",
            ApplicationDate = new DateTime(2026, 1, 10),
            ApplicationProfile = new ApplicationProfile { ProduceInvitation = true },
        };
        var person = new Person();
        person.InvitationItems.Add(new InvitationItem
        {
            ID = Guid.NewGuid(),
            Invitation = new Invitation
            {
                ID = Guid.NewGuid(),
                InvitationNumber = "1602/26",
                IssuedDate = new DateTime(2026, 1, 10),
                ExpirationDate = new DateTime(2026, 4, 11),
                ApplicationProfileInstance = application,
            },
        });
        var letterId = Guid.NewGuid();
        IReadOnlyDictionary<Guid, IReadOnlyList<ApplicationWorkspaceCaseProgressStep>> timelines =
            new Dictionary<Guid, IReadOnlyList<ApplicationWorkspaceCaseProgressStep>>
            {
                [application.ID] =
                [
                    new ApplicationWorkspaceCaseProgressStep
                    {
                        Key = "office",
                        Label = "Office preparation",
                        State = "done",
                    },
                    new ApplicationWorkspaceCaseProgressStep
                    {
                        Key = "leg-1",
                        Label = "Energetika",
                        State = "done",
                        MinistryLetterFileName = "e.pdf",
                        ProgressId = letterId,
                    },
                ],
            };

        var row = Assert.Single(PersonDossierResolver.BuildByApplication(person, null, timelines).Records);

        Assert.Equal(2, row.ProgressSteps.Count);
        Assert.Equal("Energetika", row.ProgressSteps[1].Label);
        Assert.Equal("e.pdf", row.ProgressSteps[1].LetterFileName);
        Assert.Equal(letterId, row.ProgressSteps[1].LetterProgressId);
    }

    [Fact]
    public void Visa_tab_lists_issued_and_open_visa_cases_and_skips_invitation_only()
    {
        var invitationOnly = new ApplicationProfileInstance
        {
            ID = Guid.NewGuid(),
            FullApplicationNumber = "CE2026-0100",
            ApplicationDate = new DateTime(2026, 1, 10),
            ApplicationProfile = new ApplicationProfile { Name = "Invitation", ProduceVisa = false, ProduceInvitation = true },
        };
        var extension = new ApplicationProfileInstance
        {
            ID = Guid.NewGuid(),
            FullApplicationNumber = "CE2026-0300",
            ApplicationDate = new DateTime(2026, 4, 1),
            ApplicationProfile = new ApplicationProfile
            {
                Name = "Visa and work permit extension",
                ProduceVisa = true,
                ProduceWorkPermit = true,
            },
        };
        var transfer = new ApplicationProfileInstance
        {
            ID = Guid.NewGuid(),
            FullApplicationNumber = "CE2026-0200",
            ApplicationDate = new DateTime(2026, 3, 1),
            ApplicationProfile = new ApplicationProfile { Name = "Passport change", ProduceVisa = true },
        };
        var visa = new Visa
        {
            ID = Guid.NewGuid(),
            VisaNumber = "A1731526",
            ExpirationDate = DateTime.Today.AddDays(45),
            IssuingApplicationProfileInstance = transfer,
        };
        var person = new Person();
        person.ApplicationProfileInstances.Add(invitationOnly);
        person.ApplicationProfileInstances.Add(extension);
        person.ApplicationProfileInstances.Add(transfer);
        person.InvitationItems.Add(Invitation("1602/26", new DateTime(2026, 4, 11), invitationOnly));
        var passport = new Passport { ID = Guid.NewGuid(), PassportNumber = "P-NEW", ExpirationDate = DateTime.Today.AddYears(4) };
        passport.Visas.Add(visa);
        person.Passports.Add(passport);
        person.WorkPermitItems.Add(new WorkPermitItem
        {
            ID = Guid.NewGuid(),
            WorkPermitNumber = "232323",
            ExpirationDate = new DateTime(2027, 12, 29),
            WorkPermit = new WorkPermit
            {
                ID = Guid.NewGuid(),
                ApplicationProfileInstance = extension,
            },
        });

        var invitations = PersonDossierResolver.BuildByApplication(person, usedInvitationItemIds: null);
        var visas = PersonDossierResolver.BuildVisaCases(person, usedInvitationItemIds: null);

        Assert.Equal("visas", visas.SectionId);
        Assert.Equal("CE2026-0100", Assert.Single(invitations.Records).Cells[0]);
        Assert.Equal(new[] { "CE2026-0300", "CE2026-0200" }, visas.Records.Select(record => record.Cells[0]).ToArray());
        Assert.Equal("\u2014", visas.Records[0].Cells[2]);
        Assert.Equal("\u2014", visas.Records[0].Cells[3]);
        Assert.Equal("232323", visas.Records[0].Cells[4]);
        Assert.Equal("A1731526", visas.Records[1].Cells[3]);
        Assert.Equal("P-NEW", visas.Records[1].Cells[1]);
        Assert.Equal("Expires in 45 days", visas.Records[1].CellChips[3]!.StatusLabel);
    }

    [Fact]
    public void Invitations_tab_includes_an_invitation_case_before_the_invitation_is_issued()
    {
        var open = new ApplicationProfileInstance
        {
            ID = Guid.NewGuid(),
            FullApplicationNumber = "CE2026-0400",
            ApplicationDate = new DateTime(2026, 9, 8),
            ApplicationProfile = new ApplicationProfile
            {
                Name = "Get invitation",
                ProduceInvitation = true,
                ProduceVisa = true,
            },
        };
        var person = new Person();
        person.ApplicationProfileInstances.Add(open);

        var invitations = PersonDossierResolver.BuildByApplication(person, usedInvitationItemIds: null);
        var visas = PersonDossierResolver.BuildVisaCases(person, usedInvitationItemIds: null);

        var row = Assert.Single(invitations.Records);
        Assert.Equal("CE2026-0400", row.Cells[0]);
        Assert.Equal("\u2014", row.Cells[2]);
        Assert.Empty(visas.Records);
    }

    [Fact]
    public void Empty_invitations_section_stays_on_the_dossier()
    {
        Assert.True(PersonDossierResolver.IncludeSection(
            new PersonDossierSection { SectionId = "activeProcesses" }));
        Assert.True(PersonDossierResolver.IncludeSection(
            new PersonDossierSection { SectionId = "byApplication" }));
        Assert.True(PersonDossierResolver.IncludeSection(
            new PersonDossierSection { SectionId = "visas" }));
        Assert.True(PersonDossierResolver.IncludeSection(
            new PersonDossierSection { SectionId = "workPermits" }));
        Assert.True(PersonDossierResolver.IncludeSection(
            new PersonDossierSection { SectionId = "familyMembers" }));
        Assert.True(PersonDossierResolver.IncludeSection(
            new PersonDossierSection { SectionId = "borderZones" }));
        Assert.True(PersonDossierResolver.IncludeSection(
            new PersonDossierSection { SectionId = "workPermitLocations" }));
        Assert.True(PersonDossierResolver.IncludeSection(
            new PersonDossierSection { SectionId = "rejections" }));
        Assert.True(PersonDossierResolver.IncludeSection(
            new PersonDossierSection { SectionId = "registration" }));
        Assert.True(PersonDossierResolver.IncludeSection(
            new PersonDossierSection { SectionId = "businessTrips" }));
        Assert.False(PersonDossierResolver.IncludeSection(
            new PersonDossierSection { SectionId = "passports" }));
    }

    [Fact]
    public void Active_processes_lists_unfinished_cases_of_any_type_and_skips_terminal_ones()
    {
        var movingInvitation = Case(
            "CE2026-0400",
            new DateTime(2026, 4, 1),
            ApplicationProfileInstanceProgressStateCodes.IsBeingPrepared,
            new ApplicationProfile { Name = "Get invitation", ProduceInvitation = true });
        var movingOther = Case(
            "CE2026-0500",
            new DateTime(2026, 5, 1),
            ApplicationProfileInstanceProgressStateCodes.Review1Started,
            new ApplicationProfile { Name = "Cancellation" });
        var notStarted = Case(
            "CE2026-0300",
            new DateTime(2026, 3, 1),
            stateCode: null,
            new ApplicationProfile { Name = "Visa extension", ProduceVisa = true });
        var issued = Case(
            "CE2026-0200",
            new DateTime(2026, 2, 1),
            ApplicationProfileInstanceProgressStateCodes.ProcessIssued,
            new ApplicationProfile { Name = "Get invitation", ProduceInvitation = true });
        var rejected = Case(
            "CE2026-0100",
            new DateTime(2026, 1, 1),
            ApplicationProfileInstanceProgressStateCodes.ProcessRejected,
            new ApplicationProfile { Name = "Visa extension", ProduceVisa = true });
        var cancelled = Case(
            "CE2026-0150",
            new DateTime(2026, 1, 15),
            ApplicationProfileInstanceProgressStateCodes.ProcessCancelled,
            new ApplicationProfile { Name = "Border zone" });

        var person = new Person();
        person.ApplicationProfileInstances.Add(movingInvitation);
        person.ApplicationProfileInstances.Add(movingOther);
        person.ApplicationProfileInstances.Add(notStarted);
        person.ApplicationProfileInstances.Add(issued);
        person.ApplicationProfileInstances.Add(rejected);
        person.ApplicationProfileInstances.Add(cancelled);
        person.InvitationItems.Add(Invitation("1602/26", new DateTime(2026, 6, 1), movingInvitation));

        var active = PersonDossierResolver.BuildActiveProcesses(person, usedInvitationItemIds: null);
        var invitations = PersonDossierResolver.BuildByApplication(person, usedInvitationItemIds: null);

        Assert.Equal("activeProcesses", active.SectionId);
        Assert.Equal("Active processes", active.SectionLabel);
        Assert.Equal(
            new[] { "CE2026-0500", "CE2026-0400", "CE2026-0300" },
            active.Records.Select(record => record.Cells[0]).ToArray());
        Assert.Equal("1602/26", active.Records[1].Cells[2]);
        Assert.Equal("\u2014", active.Records[0].Cells[2]);
        Assert.Equal("\u2014", active.Records[0].Cells[3]);
        Assert.Equal("\u2014", active.Records[0].Cells[4]);
        Assert.Equal(
            new[] { "CE2026-0400", "CE2026-0200" },
            invitations.Records.Select(record => record.Cells[0]).ToArray());
    }

    [Fact]
    public void Grant_tabs_list_the_case_and_the_granted_value()
    {
        var person = new Person();
        var invitation = new ApplicationProfileInstance
        {
            ID = Guid.NewGuid(),
            FullApplicationNumber = "CE2026-0100",
            ApplicationDate = new DateTime(2026, 1, 10),
            ApplicationProfile = new ApplicationProfile
            {
                Name = "Get invitation",
                ProduceInvitation = true,
                ProduceRejection = true,
                RequireBorderZone = true,
            },
        };
        var invitationItem = Invitation("1602/26", new DateTime(2026, 4, 11), invitation);
        invitationItem.Invitation!.BorderZoneLocation = "Serhet";

        var extension = new ApplicationProfileInstance
        {
            ID = Guid.NewGuid(),
            FullApplicationNumber = "CE2026-0200",
            ApplicationDate = new DateTime(2026, 3, 1),
            ApplicationProfile = new ApplicationProfile
            {
                Name = "Visa and work permit extension",
                ProduceVisa = true,
                ProduceWorkPermit = true,
                RequireBorderZone = true,
            },
        };
        var borderOnly = new ApplicationProfileInstance
        {
            ID = Guid.NewGuid(),
            FullApplicationNumber = "CE2026-0300",
            ApplicationDate = new DateTime(2026, 4, 1),
            ApplicationProfile = new ApplicationProfile { Name = "Border zone", ProduceBorderZone = true },
        };
        var extraPlaces = new ApplicationProfileInstance
        {
            ID = Guid.NewGuid(),
            FullApplicationNumber = "CE2026-0400",
            ApplicationDate = new DateTime(2026, 5, 1),
            ApplicationProfile = new ApplicationProfile { Name = "Additional places", ProduceWorkLocation = true },
        };
        extraPlaces.WorkPermitLocationGrants.Add(new WorkPermitLocationGrant
        {
            GrantNumber = "G-1",
            IssuedDate = new DateTime(2026, 5, 2),
            ApplicationProfileInstance = extraPlaces,
            Items = { new WorkPermitLocationGrantItem { GrantedLocations = "Mary", Person = person } },
        });
        var openRejection = new ApplicationProfileInstance
        {
            ID = Guid.NewGuid(),
            FullApplicationNumber = "CE2026-0500",
            ApplicationDate = new DateTime(2026, 6, 1),
            ApplicationProfile = new ApplicationProfile { Name = "Seretmezlik", Code = "seretmezlik" },
        };
        var issuedRejection = new ApplicationProfileInstance
        {
            ID = Guid.NewGuid(),
            FullApplicationNumber = "CE2026-0600",
            ApplicationDate = new DateTime(2026, 2, 1),
            ApplicationProfile = new ApplicationProfile
            {
                Name = "Get invitation",
                ProduceInvitation = true,
                ProduceRejection = true,
            },
        };
        issuedRejection.Rejections.Add(new Rejection
        {
            RejectedDocNumber = "R-9",
            Date = new DateTime(2026, 2, 2),
            ApplicationProfileInstance = issuedRejection,
        });

        person.ApplicationProfileInstances.Add(invitation);
        person.ApplicationProfileInstances.Add(extension);
        person.ApplicationProfileInstances.Add(borderOnly);
        person.ApplicationProfileInstances.Add(extraPlaces);
        person.ApplicationProfileInstances.Add(openRejection);
        person.ApplicationProfileInstances.Add(issuedRejection);
        person.InvitationItems.Add(invitationItem);
        person.WorkPermitItems.Add(new WorkPermitItem
        {
            ID = Guid.NewGuid(),
            WorkPermitNumber = "1512/1",
            WorkPermittedLocations = "Balkan",
            ExpirationDate = new DateTime(2027, 4, 20),
            WorkPermit = new WorkPermit { ID = Guid.NewGuid(), ApplicationProfileInstance = extension },
        });

        var border = PersonDossierResolver.BuildBorderZoneCases(person, null);
        var locations = PersonDossierResolver.BuildWorkPermitLocationCases(person, null);
        var rejections = PersonDossierResolver.BuildRejectionCases(person, null);
        var invitations = PersonDossierResolver.BuildByApplication(person, null);

        Assert.Equal(new[] { "CE2026-0300", "CE2026-0200", "CE2026-0100" }, border.Records.Select(record => record.Cells[0]).ToArray());
        Assert.Equal("\u2014", border.Records[0].Cells[5]);
        Assert.Equal("Serhet", border.Records[2].Cells[5]);
        Assert.Equal(new[] { "CE2026-0400", "CE2026-0200" }, locations.Records.Select(record => record.Cells[0]).ToArray());
        Assert.Equal("Mary", locations.Records[0].Cells[5]);
        Assert.Equal("Balkan", locations.Records[1].Cells[5]);
        Assert.Equal(new[] { "CE2026-0500", "CE2026-0600" }, rejections.Records.Select(record => record.Cells[0]).ToArray());
        Assert.Equal("\u2014", rejections.Records[0].Cells[5]);
        Assert.Equal("R-9", rejections.Records[1].Cells[5]);
        Assert.Contains(invitations.Records, record => record.Cells[0] == "CE2026-0100");
        Assert.Contains(invitations.Records, record => record.Cells[0] == "CE2026-0600");
        Assert.DoesNotContain(rejections.Records, record => record.Cells[0] == "CE2026-0100");
    }

    [Fact]
    public void Family_member_row_shows_document_states_and_relationship_copy()
    {
        var memberId = Guid.NewGuid();
        var passportId = Guid.NewGuid();
        var visaId = Guid.NewGuid();
        var proofId = Guid.NewGuid();
        var passport = new Passport
        {
            ID = passportId,
            PassportNumber = "P-SON",
            ExpirationDate = DateTime.Today.AddYears(2),
        };
        var visa = new Visa
        {
            ID = visaId,
            VisaNumber = "V-SON",
            ExpirationDate = DateTime.Today.AddDays(20),
            Passport = passport,
        };
        passport.Visas.Add(visa);
        var member = new Person
        {
            ID = memberId,
            FirstName = "Alper",
            LastName = "Kucukakkaya",
            DateOfBirth = new DateTime(2005, 8, 1),
        };
        member.Passports.Add(passport);
        member.InvitationItems.Add(new InvitationItem
        {
            ID = Guid.NewGuid(),
            Passport = passport,
            Invitation = new Invitation
            {
                ID = Guid.NewGuid(),
                InvitationNumber = "INV-SON",
                IssuedDate = DateTime.Today.AddDays(-10),
                ExpirationDate = DateTime.Today.AddDays(80),
            },
        });
        member.FamilyRelationDocuments.Add(new PersonFamilyRelationDocument
        {
            ID = proofId,
            Person = member,
            File = new FileData { FileName = "birth-certificate.pdf" },
        });
        var employee = new Person();
        employee.FamilyMembers.Add(member);

        var section = PersonDossierResolver.BuildFamilyMembers(null, employee);
        var row = Assert.Single(section.Records);

        Assert.Equal("Relationship document", section.ColumnHeaders[^1]);
        Assert.Equal("Alper Kucukakkaya", row.Cells[0]);
        Assert.Equal("P-SON", row.Cells[4]);
        Assert.Equal("INV-SON", row.Cells[5]);
        Assert.Equal("V-SON", row.Cells[6]);
        Assert.Equal("Family relation documents", row.Cells[7]);
        Assert.Equal("Family relation documents", row.MultiChips[0].Number);
        Assert.Equal("P-SON", row.CellChips[4]!.Number);
        Assert.False(string.IsNullOrWhiteSpace(row.CellChips[4]!.StatusLabel));
        Assert.Equal("INV-SON", row.CellChips[5]!.Number);
        Assert.False(string.IsNullOrWhiteSpace(row.CellChips[5]!.StatusLabel));
        Assert.Equal("V-SON", row.CellChips[6]!.Number);
        Assert.Equal("Expires in 20 days", row.CellChips[6]!.StatusLabel);
        var proof = Assert.Single(row.MultiChips);
        Assert.Equal(7, row.MultiChipCellIndex);
        Assert.Equal(memberId, proof.PersonCopyPersonId);
        Assert.Equal("FamilyRelationDocuments", proof.PersonCopySectionId);
        Assert.Equal($"FamilyRelationDocument:{proofId:N}", proof.PersonCopyRecordKey);
        Assert.True(proof.HasPreview);
    }

    [Fact]
    public void Family_member_case_tabs_list_only_that_members_own_cases()
    {
        var memberCase = new ApplicationProfileInstance
        {
            ID = Guid.NewGuid(),
            FullApplicationNumber = "CE-FAM-1",
            ApplicationDate = new DateTime(2026, 11, 1),
            ApplicationProfile = new ApplicationProfile
            {
                Name = "Family invitation",
                ProduceInvitation = true,
            },
        };
        var member = new Person
        {
            ID = Guid.NewGuid(),
            FirstName = "Alper",
            LastName = "Kucukakkaya",
        };
        member.ApplicationProfileInstances.Add(memberCase);
        var employee = new Person();
        employee.FamilyMembers.Add(member);

        var section = PersonDossierResolver.BuildFamilyMembers(null, employee);
        Assert.Equal(
            ["familyActiveProcesses", "familyInvitations", "familyVisas", "familyBorderZones"],
            section.NestedSections.Select(tab => tab.SectionId).ToArray());

        var invitations = section.NestedSections.Single(tab => tab.SectionId == "familyInvitations");
        var row = Assert.Single(invitations.Records);
        Assert.Equal("Alper Kucukakkaya", row.MemberName);
        Assert.Equal("CE-FAM-1", row.Cells[0]);
        Assert.True(invitations.HasMemberColumn);
        Assert.Equal("Invitations", invitations.SectionLabel);

        var active = section.NestedSections.Single(tab => tab.SectionId == "familyActiveProcesses");
        Assert.Contains(active.Records, record => record.Cells[0] == "CE-FAM-1" && record.MemberName == "Alper Kucukakkaya");
        Assert.Empty(section.NestedSections.Single(tab => tab.SectionId == "familyVisas").Records);
    }

    [Fact]
    public void Registration_and_business_trip_tabs_list_only_those_cases()
    {
        var person = new Person();
        person.ApplicationProfileInstances.Add(Case(
            "CE-REG",
            new DateTime(2026, 11, 2),
            null,
            new ApplicationProfile
            {
                Name = "Check in",
                ActionFamily = ApplicationProfileActionFamily.Registration,
            }));
        person.ApplicationProfileInstances.Add(Case(
            "CE-TRIP",
            new DateTime(2026, 11, 3),
            null,
            new ApplicationProfile
            {
                Name = "Departure",
                ActionFamily = ApplicationProfileActionFamily.BusinessTrip,
            }));
        person.ApplicationProfileInstances.Add(Case(
            "CE-INV",
            new DateTime(2026, 11, 1),
            null,
            new ApplicationProfile { Name = "Invitation", ProduceInvitation = true }));

        var registration = PersonDossierResolver.BuildRegistrationCases(person, null);
        var trips = PersonDossierResolver.BuildBusinessTripCases(person, null);

        Assert.Equal("Registration", registration.SectionLabel);
        Assert.Equal("Business trips", trips.SectionLabel);
        var registrationRow = Assert.Single(registration.Records);
        Assert.Equal("CE-REG", registrationRow.Cells[0]);
        Assert.Equal("Check in", registrationRow.FirstCellDetail);
        var tripRow = Assert.Single(trips.Records);
        Assert.Equal("CE-TRIP", tripRow.Cells[0]);
        Assert.Equal("Departure", tripRow.FirstCellDetail);
        Assert.DoesNotContain(registration.Records, record => record.Cells[0] == "CE-INV");
        Assert.DoesNotContain(trips.Records, record => record.Cells[0] == "CE-INV" || record.Cells[0] == "CE-REG");
    }

    private static ApplicationProfileInstance Case(
        string number,
        DateTime date,
        string? stateCode,
        ApplicationProfile profile)
    {
        var application = new ApplicationProfileInstance
        {
            ID = Guid.NewGuid(),
            FullApplicationNumber = number,
            ApplicationDate = date,
            ApplicationProfile = profile,
        };
        if (stateCode != null)
        {
            application.LatestProgress = new ApplicationProfileInstanceProgress
            {
                State = new ApplicationState { Code = stateCode },
            };
        }

        return application;
    }

    private static InvitationItem Invitation(string number, DateTime expiration, ApplicationProfileInstance application) =>
        new()
        {
            ID = Guid.NewGuid(),
            Invitation = new Invitation
            {
                ID = Guid.NewGuid(),
                InvitationNumber = number,
                IssuedDate = expiration.AddDays(-91),
                ExpirationDate = expiration,
                ApplicationProfileInstance = application,
            },
        };
}
