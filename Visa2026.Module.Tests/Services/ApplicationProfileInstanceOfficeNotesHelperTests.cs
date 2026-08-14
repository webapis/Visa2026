using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.OfficerShell;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationProfileInstanceOfficeNotesHelperTests
{
    [Fact]
    public void Save_NoHistory_WritesOfficePreparationNotes()
    {
        var application = new ApplicationProfileInstance();

        ApplicationProfileInstanceOfficeNotesHelper.Save(application, latest: null, "  office note  ");

        Assert.Equal("office note", application.OfficePreparationNotes);
    }

    [Fact]
    public void Save_WithLatest_WritesDescription()
    {
        var application = new ApplicationProfileInstance { OfficePreparationNotes = "stale" };
        var latest = new ApplicationProfileInstanceProgress();

        ApplicationProfileInstanceOfficeNotesHelper.Save(application, latest, "step note");

        Assert.Equal("step note", latest.Description);
        Assert.Equal("stale", application.OfficePreparationNotes);
    }

    [Fact]
    public void CopyOntoNewRow_UsesDraftThenClearsOfficeNotes()
    {
        var application = new ApplicationProfileInstance { OfficePreparationNotes = "from office" };
        var row = new ApplicationProfileInstanceProgress();

        ApplicationProfileInstanceOfficeNotesHelper.CopyOntoNewRow(application, row, notesOnLatestStep: null);

        Assert.Equal("from office", row.Description);
        Assert.Null(application.OfficePreparationNotes);
    }

    [Fact]
    public void CopyOntoNewRow_PrefersAdvanceNotes()
    {
        var application = new ApplicationProfileInstance { OfficePreparationNotes = "from office" };
        var row = new ApplicationProfileInstanceProgress();

        ApplicationProfileInstanceOfficeNotesHelper.CopyOntoNewRow(application, row, "from advance");

        Assert.Equal("from advance", row.Description);
        Assert.Null(application.OfficePreparationNotes);
    }
}