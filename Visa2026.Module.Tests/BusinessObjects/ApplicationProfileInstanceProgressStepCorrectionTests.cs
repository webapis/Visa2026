using System;
using System.Collections.ObjectModel;
using DevExpress.Persistent.BaseImpl.EF;
using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

public class ApplicationProfileInstanceProgressStepCorrectionTests
{
    [Fact]
    public void Date_between_neighbors_is_accepted()
    {
        var ok = ApplicationProfileInstanceProgressStepDates.TryValidate(
            new DateTime(2026, 9, 24),
            new DateTime(2026, 9, 20),
            new DateTime(2026, 9, 28),
            out var error);

        Assert.True(ok);
        Assert.Null(error);
    }

    [Fact]
    public void Date_after_the_next_step_is_rejected()
    {
        var ok = ApplicationProfileInstanceProgressStepDates.TryValidate(
            new DateTime(2026, 9, 29),
            new DateTime(2026, 9, 20),
            new DateTime(2026, 9, 28),
            out var error);

        Assert.False(ok);
        Assert.Contains("20.09.2026", error);
        Assert.Contains("28.09.2026", error);
    }

    [Fact]
    public void Date_before_the_previous_step_is_rejected()
    {
        var ok = ApplicationProfileInstanceProgressStepDates.TryValidate(
            new DateTime(2026, 9, 19),
            new DateTime(2026, 9, 20),
            latest: null,
            out var error);

        Assert.False(ok);
        Assert.Contains("20.09.2026", error);
    }

    [Fact]
    public void Same_day_as_both_neighbors_is_accepted()
    {
        var day = new DateTime(2026, 9, 24, 15, 0, 0);
        var ok = ApplicationProfileInstanceProgressStepDates.TryValidate(day, day, day, out _);
        Assert.True(ok);
    }

    [Fact]
    public void List_keeps_the_primary_file_and_appends_later_files()
    {
        var primaryId = Guid.NewGuid();
        var extraId = Guid.NewGuid();
        var row = new ApplicationProfileInstanceProgress
        {
            MinistryLetterFile = new FileData { ID = primaryId, FileName = "letter.pdf", Size = 12 },
            Files = new ObservableCollection<ApplicationProfileInstanceProgressFile>
            {
                new()
                {
                    Slot = ApplicationProfileInstanceProgressFiles.MinistryLetter,
                    SortOrder = 1,
                    File = new FileData { ID = extraId, FileName = "scan.jpg", Size = 20 },
                },
            },
        };

        var files = ApplicationProfileInstanceProgressFiles.List(
            row,
            ApplicationProfileInstanceProgressFiles.MinistryLetter);

        Assert.Equal(2, files.Count);
        Assert.Equal(primaryId, files[0].FileId);
        Assert.Equal("letter.pdf", files[0].FileName);
        Assert.Equal(extraId, files[1].FileId);
        Assert.Equal("scan.jpg", files[1].FileName);
    }

    [Fact]
    public void List_does_not_mix_submission_files_into_the_ministry_slot()
    {
        var row = new ApplicationProfileInstanceProgress
        {
            SubmissionDocumentFile = new FileData { ID = Guid.NewGuid(), FileName = "copy.pdf", Size = 8 },
            Files = new ObservableCollection<ApplicationProfileInstanceProgressFile>
            {
                new()
                {
                    Slot = ApplicationProfileInstanceProgressFiles.SubmissionDocument,
                    SortOrder = 1,
                    File = new FileData { ID = Guid.NewGuid(), FileName = "page.png", Size = 9 },
                },
            },
        };

        Assert.Empty(ApplicationProfileInstanceProgressFiles.List(
            row,
            ApplicationProfileInstanceProgressFiles.MinistryLetter));
        Assert.Equal(2, ApplicationProfileInstanceProgressFiles.List(
            row,
            ApplicationProfileInstanceProgressFiles.SubmissionDocument).Count);
    }
}
