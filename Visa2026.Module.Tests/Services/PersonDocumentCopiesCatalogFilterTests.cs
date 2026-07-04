using Visa2026.Module.Services.PersonLinkedDocuments;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public sealed class PersonDocumentCopiesCatalogFilterTests
{
    private static PersonLinkedDocumentSection Section(
        string sectionId,
        params (string key, bool isCurrent)[] records) =>
        new()
        {
            SectionId = sectionId,
            SectionLabel = sectionId,
            Records = records
                .Select(record => new PersonLinkedDocumentRecord
                {
                    RecordKey = record.key,
                    RecordLabel = record.key,
                    IsCurrent = record.isCurrent
                })
                .ToList()
        };

    [Fact]
    public void GetVisibleRecords_CurrentSection_ShowsOnlyCurrentByDefault()
    {
        var section = Section("Passports", ("p1", true), ("p2", false), ("v1", true), ("v2", false));

        var visible = PersonDocumentCopiesCatalogFilter.GetVisibleRecords(section, false, false);

        Assert.Equal(2, visible.Count);
        Assert.All(visible, record => Assert.True(record.IsCurrent));
    }

    [Fact]
    public void GetVisibleRecords_NoCurrent_ShowsMostRecentOnly()
    {
        var section = Section("Education", ("e1", false), ("e2", false));

        var visible = PersonDocumentCopiesCatalogFilter.GetVisibleRecords(section, false, false);

        Assert.Single(visible);
        Assert.Equal("e1", visible[0].RecordKey);
        Assert.True(PersonDocumentCopiesCatalogFilter.ShowsRecentFallback(section, false, false));
    }

    [Fact]
    public void GetVisibleRecords_SectionExpanded_ShowsAllCurrentFirst()
    {
        var section = Section("Passports", ("p-old", false), ("p-current", true));

        var visible = PersonDocumentCopiesCatalogFilter.GetVisibleRecords(section, false, true);

        Assert.Equal(2, visible.Count);
        Assert.Equal("p-current", visible[0].RecordKey);
        Assert.Equal(0, PersonDocumentCopiesCatalogFilter.GetHiddenCount(section, false, true));
    }

    [Fact]
    public void GetVisibleRecords_UncategorizedSection_CapsAtFive()
    {
        var section = Section(
            "PersonDocuments",
            ("d1", false),
            ("d2", false),
            ("d3", false),
            ("d4", false),
            ("d5", false),
            ("d6", false));

        var visible = PersonDocumentCopiesCatalogFilter.GetVisibleRecords(section, false, false);

        Assert.Equal(5, visible.Count);
        Assert.Equal(1, PersonDocumentCopiesCatalogFilter.GetHiddenCount(section, false, false));
    }

    [Fact]
    public void GetVisibleRecords_ShowAll_ShowsEverything()
    {
        var section = Section("Passports", ("p1", true), ("p2", false));

        var visible = PersonDocumentCopiesCatalogFilter.GetVisibleRecords(section, true, false);

        Assert.Equal(2, visible.Count);
        Assert.Equal(0, PersonDocumentCopiesCatalogFilter.GetHiddenCount(section, true, false));
    }
}
