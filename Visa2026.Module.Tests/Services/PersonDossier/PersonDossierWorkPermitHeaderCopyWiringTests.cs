#nullable enable

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
/// Work-permit dossier rows route Preview/Upload through the header copies slot
/// (parent <see cref="WorkPermit"/>), not person-copy keys.
/// </summary>
public class PersonDossierWorkPermitHeaderCopyWiringTests
{
    [Fact]
    public void Resolve_work_permit_with_header_scan_enables_preview_not_upload()
    {
        var permitId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var person = new Person
        {
            ID = personId,
            FirstName = "Hayati",
            LastName = "Uyan",
            PersonRole = PersonRecordRole.Employee,
        };
        var permit = new WorkPermit
        {
            ID = permitId,
            Documents =
            [
                new WorkPermitDocument
                {
                    File = new FileData { FileName = "wp.pdf", Content = [1, 2, 3] },
                },
            ],
        };
        var item = new WorkPermitItem
        {
            ID = itemId,
            WorkPermitNumber = "WP-9",
            WorkPermit = permit,
            ExpirationDate = DateTime.Today.AddDays(90),
        };
        person.WorkPermitItems = new ObservableCollection<WorkPermitItem> { item };

        var snapshot = PersonDossierResolver.Resolve(PassthroughObjectSpaceStub.Create(), person);
        var section = Assert.Single(snapshot.Sections, s => s.SectionId == "workPermits");
        var record = Assert.Single(section.Records);

        Assert.True(record.HasHeaderPreview);
        Assert.Equal(HeaderDocumentCopiesFamily.WorkPermit, record.PreviewFamily);
        Assert.Equal(permitId, record.PreviewParentId);
        Assert.False(record.CanUpload);
        Assert.Null(record.UploadHeaderId);
        Assert.Null(record.PersonCopyPersonId);
    }

    [Fact]
    public void Resolve_work_permit_without_scan_enables_header_upload()
    {
        var permitId = Guid.NewGuid();
        var instanceId = Guid.NewGuid();
        var person = new Person
        {
            ID = Guid.NewGuid(),
            FirstName = "Hong",
            LastName = "Huang",
            PersonRole = PersonRecordRole.Employee,
        };
        var permit = new WorkPermit
        {
            ID = permitId,
            ApplicationProfileInstance = new ApplicationProfileInstance { ID = instanceId },
            Documents = new ObservableCollection<WorkPermitDocument>(),
        };
        person.WorkPermitItems = new ObservableCollection<WorkPermitItem>
        {
            new()
            {
                ID = Guid.NewGuid(),
                WorkPermitNumber = "WP-1",
                WorkPermit = permit,
                ExpirationDate = DateTime.Today.AddDays(30),
            },
        };

        var snapshot = PersonDossierResolver.Resolve(PassthroughObjectSpaceStub.Create(), person);
        var record = Assert.Single(Assert.Single(snapshot.Sections, s => s.SectionId == "workPermits").Records);

        Assert.False(record.HasPreview);
        Assert.True(record.CanUploadHeader);
        Assert.Equal(permitId, record.UploadHeaderId);
        Assert.Equal(IssueIssuedHeaderKind.WorkPermit, record.UploadIssuedKind);
        Assert.Equal(instanceId, record.UploadApplicationProfileInstanceId);
    }

    [Fact]
    public void Resolve_skips_work_permits_for_temporary_visitors()
    {
        var person = new Person
        {
            ID = Guid.NewGuid(),
            FirstName = "Visitor",
            LastName = "Only",
            PersonRole = PersonRecordRole.TemporaryVisitor,
            WorkPermitItems = new ObservableCollection<WorkPermitItem>
            {
                new()
                {
                    ID = Guid.NewGuid(),
                    WorkPermitNumber = "WP-X",
                    WorkPermit = new WorkPermit { ID = Guid.NewGuid() },
                    ExpirationDate = DateTime.Today.AddDays(10),
                },
            },
        };

        var snapshot = PersonDossierResolver.Resolve(PassthroughObjectSpaceStub.Create(), person);
        Assert.DoesNotContain(snapshot.Sections, s => s.SectionId == "workPermits");
    }
}
