using System;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.HeaderLinkedDocuments;
using Visa2026.Module.Services.PersonDossier;
using Visa2026.Module.Services.PreviewSlot;
using Xunit;

namespace Visa2026.Module.Tests.Services.PersonDossier;

/// <summary>
/// Preview / Upload gates on dossier rows (passport/visa/education person-copies vs issued headers).
/// </summary>
public class PersonDossierRecordPreviewUploadTests
{
    [Fact]
    public void HasHeaderPreview_requires_family_and_non_empty_parent()
    {
        Assert.False(new PersonDossierRecord().HasHeaderPreview);
        Assert.False(new PersonDossierRecord
        {
            PreviewFamily = HeaderDocumentCopiesFamily.Invitation,
            PreviewParentId = Guid.Empty,
        }.HasHeaderPreview);
        Assert.True(new PersonDossierRecord
        {
            PreviewFamily = HeaderDocumentCopiesFamily.Invitation,
            PreviewParentId = Guid.NewGuid(),
        }.HasHeaderPreview);
    }

    [Fact]
    public void HasPersonCopyPreview_requires_person_and_record_key()
    {
        var personId = Guid.NewGuid();
        Assert.False(new PersonDossierRecord
        {
            PersonCopyPersonId = personId,
            PersonCopyRecordKey = "   ",
        }.HasPersonCopyPreview);
        Assert.False(new PersonDossierRecord
        {
            PersonCopyPersonId = Guid.Empty,
            PersonCopyRecordKey = "Passport:abc",
        }.HasPersonCopyPreview);
        Assert.True(new PersonDossierRecord
        {
            PersonCopyPersonId = personId,
            PersonCopyRecordKey = $"Passport:{Guid.NewGuid():N}",
        }.HasPersonCopyPreview);
    }

    [Fact]
    public void HasPreview_true_for_either_header_or_person_copy()
    {
        Assert.True(new PersonDossierRecord
        {
            PreviewFamily = HeaderDocumentCopiesFamily.WorkPermit,
            PreviewParentId = Guid.NewGuid(),
        }.HasPreview);
        Assert.True(new PersonDossierRecord
        {
            PersonCopyPersonId = Guid.NewGuid(),
            PersonCopyRecordKey = "Education:deadbeef",
        }.HasPreview);
        Assert.False(new PersonDossierRecord().HasPreview);
    }

    [Fact]
    public void CanUploadHeader_blocked_when_any_preview_exists()
    {
        var headerId = Guid.NewGuid();
        Assert.True(new PersonDossierRecord { UploadHeaderId = headerId }.CanUploadHeader);
        Assert.False(new PersonDossierRecord
        {
            UploadHeaderId = headerId,
            PersonCopyPersonId = Guid.NewGuid(),
            PersonCopyRecordKey = "Passport:1",
        }.CanUploadHeader);
        Assert.False(new PersonDossierRecord
        {
            UploadHeaderId = Guid.Empty,
        }.CanUploadHeader);
    }

    [Fact]
    public void CanUploadDetail_requires_type_and_id_without_preview()
    {
        var id = Guid.NewGuid();
        Assert.True(new PersonDossierRecord
        {
            UploadDetailType = typeof(Passport),
            UploadDetailId = id,
        }.CanUploadDetail);
        Assert.False(new PersonDossierRecord
        {
            UploadDetailType = typeof(Passport),
            UploadDetailId = Guid.Empty,
        }.CanUploadDetail);
        Assert.False(new PersonDossierRecord
        {
            UploadDetailType = typeof(Passport),
            UploadDetailId = id,
            PreviewFamily = HeaderDocumentCopiesFamily.Invitation,
            PreviewParentId = Guid.NewGuid(),
        }.CanUploadDetail);
    }

    [Fact]
    public void CanUpload_is_header_or_detail()
    {
        Assert.True(new PersonDossierRecord
        {
            UploadHeaderId = Guid.NewGuid(),
            UploadIssuedKind = IssueIssuedHeaderKind.Invitation,
        }.CanUpload);
        Assert.True(new PersonDossierRecord
        {
            UploadDetailType = typeof(Visa),
            UploadDetailId = Guid.NewGuid(),
        }.CanUpload);
        Assert.False(new PersonDossierRecord().CanUpload);
    }
}
