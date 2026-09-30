using System;
using System.Collections.ObjectModel;
using DevExpress.Persistent.BaseImpl.EF;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.HeaderLinkedDocuments;
using Visa2026.Module.Services.PersonDossier;
using Visa2026.Module.Services.PreviewSlot;
using Visa2026.Module.Tests.TestSupport;
using Xunit;

namespace Visa2026.Module.Tests.Services.PersonDossier;

/// <summary>
/// Verifies dossier section builders wire Preview vs Upload from scan presence
/// (passport / visa / education person-copies and invitation header copies).
/// </summary>
public class PersonDossierPersonCopyUploadWiringTests
{
    [Fact]
    public void Passport_with_scan_exposes_person_copy_preview_not_upload()
    {
        var personId = Guid.NewGuid();
        var passportId = Guid.NewGuid();
        var person = new Person
        {
            ID = personId,
            FirstName = "Ayse",
            LastName = "Yilmaz",
            PersonRole = PersonRecordRole.Employee,
            Passports =
            [
                new Passport
                {
                    ID = passportId,
                    PassportNumber = "U123",
                    ExpirationDate = DateTime.Today.AddYears(2),
                    Documents =
                    [
                        new PassportDocument
                        {
                            File = new FileData { FileName = "scan.pdf", Content = [1], Size = 1 },
                        },
                    ],
                },
            ],
        };

        var snapshot = PersonDossierResolver.Resolve(QueryableObjectSpaceStub.Create(), person);
        var record = Assert.Single(Assert.Single(snapshot.Sections, s => s.SectionId == "passports").Records);

        Assert.True(record.HasPersonCopyPreview);
        Assert.True(record.HasPreview);
        Assert.False(record.CanUploadDetail);
        Assert.Equal(personId, record.PersonCopyPersonId);
        Assert.Equal($"Passport:{passportId:N}", record.PersonCopyRecordKey);
        Assert.Null(record.UploadDetailType);
        Assert.Null(record.UploadDetailId);
    }

    [Fact]
    public void Passport_without_scan_exposes_upload_detail_not_preview()
    {
        var personId = Guid.NewGuid();
        var passportId = Guid.NewGuid();
        var person = new Person
        {
            ID = personId,
            FirstName = "Ayse",
            LastName = "Yilmaz",
            PersonRole = PersonRecordRole.Employee,
            Passports =
            [
                new Passport
                {
                    ID = passportId,
                    PassportNumber = "U123",
                    ExpirationDate = DateTime.Today.AddYears(2),
                },
            ],
        };

        var snapshot = PersonDossierResolver.Resolve(QueryableObjectSpaceStub.Create(), person);
        var record = Assert.Single(Assert.Single(snapshot.Sections, s => s.SectionId == "passports").Records);

        Assert.False(record.HasPersonCopyPreview);
        Assert.False(record.HasPreview);
        Assert.True(record.CanUploadDetail);
        Assert.Equal(typeof(Passport), record.UploadDetailType);
        Assert.Equal(passportId, record.UploadDetailId);
    }

    [Fact]
    public void Visa_with_scan_uses_passport_scoped_person_copy_key()
    {
        var personId = Guid.NewGuid();
        var passportId = Guid.NewGuid();
        var visaId = Guid.NewGuid();
        var person = new Person
        {
            ID = personId,
            FirstName = "Ayse",
            LastName = "Yilmaz",
            PersonRole = PersonRecordRole.Employee,
            Passports =
            [
                new Passport
                {
                    ID = passportId,
                    PassportNumber = "U123",
                    ExpirationDate = DateTime.Today.AddYears(2),
                    Visas =
                    [
                        new Visa
                        {
                            ID = visaId,
                            VisaNumber = "V9",
                            ExpirationDate = DateTime.Today.AddMonths(6),
                            Documents =
                            [
                                new VisaDocument
                                {
                                    File = new FileData { FileName = "visa.pdf", Content = [2], Size = 1 },
                                },
                            ],
                        },
                    ],
                },
            ],
        };

        var snapshot = PersonDossierResolver.Resolve(QueryableObjectSpaceStub.Create(), person);
        var record = Assert.Single(Assert.Single(snapshot.Sections, s => s.SectionId == "visas").Records);

        Assert.True(record.HasPersonCopyPreview);
        Assert.Equal($"Passport:{passportId:N}/Visa:{visaId:N}", record.PersonCopyRecordKey);
        Assert.False(record.CanUploadDetail);
    }

    [Fact]
    public void Education_without_scan_exposes_upload_detail()
    {
        var personId = Guid.NewGuid();
        var educationId = Guid.NewGuid();
        var person = new Person
        {
            ID = personId,
            FirstName = "Ayse",
            LastName = "Yilmaz",
            PersonRole = PersonRecordRole.Employee,
            Educations =
            [
                new Education { ID = educationId },
            ],
        };

        var snapshot = PersonDossierResolver.Resolve(QueryableObjectSpaceStub.Create(), person);
        var record = Assert.Single(Assert.Single(snapshot.Sections, s => s.SectionId == "education").Records);

        Assert.True(record.CanUploadDetail);
        Assert.Equal(typeof(Education), record.UploadDetailType);
        Assert.Equal(educationId, record.UploadDetailId);
        Assert.False(record.HasPreview);
    }

    [Fact]
    public void Invitation_with_header_scan_exposes_header_preview()
    {
        var personId = Guid.NewGuid();
        var invitationId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var person = new Person
        {
            ID = personId,
            FirstName = "Ayse",
            LastName = "Yilmaz",
            PersonRole = PersonRecordRole.Employee,
            InvitationItems =
            [
                new InvitationItem
                {
                    ID = itemId,
                    Invitation = new Invitation
                    {
                        ID = invitationId,
                        InvitationNumber = "INV-1",
                        ExpirationDate = DateTime.Today.AddMonths(3),
                        IssuedDate = DateTime.Today.AddMonths(-1),
                        Documents =
                        [
                            new InvitationDocument
                            {
                                File = new FileData { FileName = "inv.pdf", Content = [3], Size = 1 },
                            },
                        ],
                    },
                },
            ],
        };

        var snapshot = PersonDossierResolver.Resolve(QueryableObjectSpaceStub.Create(), person);
        var record = Assert.Single(Assert.Single(snapshot.Sections, s => s.SectionId == "invitations").Records);

        Assert.True(record.HasHeaderPreview);
        Assert.True(record.HasPreview);
        Assert.Equal(HeaderDocumentCopiesFamily.Invitation, record.PreviewFamily);
        Assert.Equal(invitationId, record.PreviewParentId);
        Assert.False(record.CanUploadHeader);
    }

    [Fact]
    public void Invitation_without_header_scan_exposes_header_upload()
    {
        var invitationId = Guid.NewGuid();
        var caseId = Guid.NewGuid();
        var person = new Person
        {
            ID = Guid.NewGuid(),
            FirstName = "Ayse",
            LastName = "Yilmaz",
            PersonRole = PersonRecordRole.Employee,
            InvitationItems =
            [
                new InvitationItem
                {
                    ID = Guid.NewGuid(),
                    Invitation = new Invitation
                    {
                        ID = invitationId,
                        InvitationNumber = "INV-2",
                        ExpirationDate = DateTime.Today.AddMonths(3),
                        IssuedDate = DateTime.Today.AddMonths(-1),
                        ApplicationProfileInstance = new ApplicationProfileInstance { ID = caseId },
                        Documents = new ObservableCollection<InvitationDocument>(),
                    },
                },
            ],
        };

        var snapshot = PersonDossierResolver.Resolve(QueryableObjectSpaceStub.Create(), person);
        var record = Assert.Single(Assert.Single(snapshot.Sections, s => s.SectionId == "invitations").Records);

        Assert.False(record.HasHeaderPreview);
        Assert.True(record.CanUploadHeader);
        Assert.Equal(invitationId, record.UploadHeaderId);
        Assert.Equal(IssueIssuedHeaderKind.Invitation, record.UploadIssuedKind);
        Assert.Equal(caseId, record.UploadApplicationProfileInstanceId);
    }
}
