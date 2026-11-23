using System;
using System.Collections.Generic;
using Visa2026.Module.Services.PersonIntake;
using Xunit;

namespace Visa2026.Module.Tests.Services.PersonIntake;

public class PersonIntakeCopyPreviewTests
{
    [Fact]
    public void Collect_IncludesUnsavedBytes_AndSkipsExistingWithoutAnId()
    {
        var copies = new[]
        {
            new PersonIntakeDocumentCopyDraft
            {
                FileName = "new.jpg",
                Content = new byte[] { 1, 2, 3 },
                IsExisting = false
            },
            new PersonIntakeDocumentCopyDraft
            {
                FileName = "saved.jpg",
                IsExisting = true
            }
        };

        var files = PersonIntakeCopyPreview.Collect(null, null, copies);

        Assert.Single(files);
        Assert.Equal("new.jpg", files[0].FileName);
        Assert.Equal(new byte[] { 1, 2, 3 }, files[0].Content);
    }

    [Fact]
    public void HasPreview_IsTrueForSavedDocumentId_OrNewBytes()
    {
        Assert.False(PersonIntakeCopyPreview.HasPreview((IEnumerable<PersonIntakeDocumentCopyDraft>?)null));
        Assert.True(PersonIntakeCopyPreview.HasPreview(new[]
        {
            new PersonIntakeDocumentCopyDraft { IsExisting = true, ExistingDocumentId = Guid.NewGuid() }
        }));
        Assert.True(PersonIntakeCopyPreview.HasPreview(new PersonIntakePersonDocumentDraft
        {
            IsExisting = false,
            Content = new byte[] { 9 }
        }));
    }

    [Fact]
    public void LayoutKey_UsesSavedId_OrCardId()
    {
        var saved = Guid.Parse("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
        var card = Guid.Parse("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb");
        Assert.Equal($"Passport:{saved:N}", PersonIntakeCopyPreview.LayoutKey("Passport", saved, card));
        Assert.Equal($"intake:{card:N}", PersonIntakeCopyPreview.LayoutKey("Passport", null, card));
    }
}
