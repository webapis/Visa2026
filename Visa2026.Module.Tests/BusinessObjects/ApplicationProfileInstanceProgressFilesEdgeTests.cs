#nullable enable

using System;
using System.Collections.ObjectModel;
using DevExpress.Persistent.BaseImpl.EF;
using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

/// <summary>
/// Edge cases for multi-file progress storage (blank names, zero-size skip,
/// primary assignment). List happy-path coverage lives in StepCorrectionTests.
/// </summary>
public class ApplicationProfileInstanceProgressFilesEdgeTests
{
    [Fact]
    public void List_null_row_returns_empty()
    {
        Assert.Empty(ApplicationProfileInstanceProgressFiles.List(
            null, ApplicationProfileInstanceProgressFiles.MinistryLetter));
    }

    [Fact]
    public void List_skips_zero_size_primary_and_extras()
    {
        var row = new ApplicationProfileInstanceProgress
        {
            MinistryLetterFile = new FileData { ID = Guid.NewGuid(), FileName = "empty.pdf", Size = 0 },
            Files = new ObservableCollection<ApplicationProfileInstanceProgressFile>
            {
                new()
                {
                    Slot = ApplicationProfileInstanceProgressFiles.MinistryLetter,
                    SortOrder = 1,
                    File = new FileData { ID = Guid.NewGuid(), FileName = "also-empty.jpg", Size = 0 },
                },
            },
        };

        Assert.Empty(ApplicationProfileInstanceProgressFiles.List(
            row, ApplicationProfileInstanceProgressFiles.MinistryLetter));
    }

    [Fact]
    public void List_blank_file_names_become_file()
    {
        var primaryId = Guid.NewGuid();
        var extraId = Guid.NewGuid();
        var row = new ApplicationProfileInstanceProgress
        {
            MinistryLetterFile = new FileData { ID = primaryId, FileName = "   ", Size = 4 },
            Files = new ObservableCollection<ApplicationProfileInstanceProgressFile>
            {
                new()
                {
                    Slot = ApplicationProfileInstanceProgressFiles.MinistryLetter,
                    SortOrder = 1,
                    File = new FileData { ID = extraId, FileName = null!, Size = 8 },
                },
            },
        };

        var files = ApplicationProfileInstanceProgressFiles.List(
            row, ApplicationProfileInstanceProgressFiles.MinistryLetter);

        Assert.Equal(2, files.Count);
        Assert.Equal("file", files[0].FileName);
        Assert.Equal("file", files[1].FileName);
    }

    [Fact]
    public void List_skips_extra_that_duplicates_primary_id()
    {
        var sharedId = Guid.NewGuid();
        var row = new ApplicationProfileInstanceProgress
        {
            SubmissionDocumentFile = new FileData { ID = sharedId, FileName = "copy.pdf", Size = 10 },
            Files = new ObservableCollection<ApplicationProfileInstanceProgressFile>
            {
                new()
                {
                    Slot = ApplicationProfileInstanceProgressFiles.SubmissionDocument,
                    SortOrder = 0,
                    File = new FileData { ID = sharedId, FileName = "copy.pdf", Size = 10 },
                },
            },
        };

        var files = ApplicationProfileInstanceProgressFiles.List(
            row, ApplicationProfileInstanceProgressFiles.SubmissionDocument);

        Assert.Single(files);
        Assert.Equal(sharedId, files[0].FileId);
    }

    [Fact]
    public void AssignPrimary_routes_by_slot()
    {
        var row = new ApplicationProfileInstanceProgress();
        var letter = new FileData { ID = Guid.NewGuid(), FileName = "letter.pdf", Size = 1 };
        var submission = new FileData { ID = Guid.NewGuid(), FileName = "sub.pdf", Size = 2 };

        ApplicationProfileInstanceProgressFiles.AssignPrimary(
            row, ApplicationProfileInstanceProgressFiles.MinistryLetter, letter);
        ApplicationProfileInstanceProgressFiles.AssignPrimary(
            row, ApplicationProfileInstanceProgressFiles.SubmissionDocument, submission);

        Assert.Same(letter, row.MinistryLetterFile);
        Assert.Same(submission, row.SubmissionDocumentFile);
        Assert.Same(letter, ApplicationProfileInstanceProgressFiles.PrimaryFile(
            row, ApplicationProfileInstanceProgressFiles.MinistryLetter));
        Assert.Same(submission, ApplicationProfileInstanceProgressFiles.PrimaryFile(
            row, ApplicationProfileInstanceProgressFiles.SubmissionDocument));
    }
}
