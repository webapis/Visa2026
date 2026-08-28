using System.Collections.Generic;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;
using Visa2026.Module.Services.PersonDossier;
using Visa2026.Module.Services.PersonLinkedDocuments;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Director hand-over ZIP notes and entry paths — gaps/notes and Visa→Visas folder override.
/// Distinct from open PR #7 naming/sanitizer coverage.
/// </summary>
public sealed class PersonExportPackerNotesAndEntryTests
{
    [Fact]
    public void BuildNotes_NoGaps_IncludesTitleAndNoGapsLine()
    {
        var notes = PersonExportPacker.BuildNotes([], "en-US");

        Assert.Contains(VisaUiMessages.Get("PersonDossier.Export.Notes.Title", "en-US"), notes);
        Assert.Contains(VisaUiMessages.Get("PersonDossier.Export.Notes.NoGaps", "en-US"), notes);
        Assert.DoesNotContain(VisaUiMessages.Get("PersonDossier.Export.Notes.GapsHeader", "en-US"), notes);
    }

    [Fact]
    public void BuildNotes_WithGaps_ListsBulletLines()
    {
        var notes = PersonExportPacker.BuildNotes(["Passports — U1", "Visas — V2"], "en-US");

        Assert.Contains(VisaUiMessages.Get("PersonDossier.Export.Notes.GapsHeader", "en-US"), notes);
        Assert.Contains("  - Passports — U1", notes);
        Assert.Contains("  - Visas — V2", notes);
    }

    [Fact]
    public void BuildEntryName_UsesRecordLabelUnderSectionFolder()
    {
        var section = new PersonLinkedDocumentSection
        {
            SectionId = "passports",
            SectionLabel = "Passports",
            SortOrder = 1,
        };
        var record = new PersonLinkedDocumentRecord
        {
            RecordKey = "Passport:1",
            RecordLabel = "Passport U40412139",
            SourceObjectType = typeof(Passport),
        };
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var entry = PersonExportPacker.BuildEntryName(section, record, "scan.pdf", "en-US", used);

        Assert.Equal("Passports/Passport U40412139.pdf", entry);
        Assert.Contains(entry, used);
    }

    [Fact]
    public void BuildEntryName_VisaRecord_UsesVisasFolderOverride()
    {
        // Catalog nests visas under Passports; hand-over ZIP should put them under Visas/.
        var section = new PersonLinkedDocumentSection
        {
            SectionId = "passports",
            SectionLabel = "Passports",
            SortOrder = 1,
        };
        var record = new PersonLinkedDocumentRecord
        {
            RecordKey = "Visa:1",
            RecordLabel = "Visa AS-1",
            SourceObjectType = typeof(Visa),
        };
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var entry = PersonExportPacker.BuildEntryName(section, record, null, "en-US", used);

        Assert.StartsWith("Visas/", entry, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith("Visa AS-1.pdf", entry, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildEntryName_DuplicateLeaf_SuffixesWithinFolder()
    {
        var section = new PersonLinkedDocumentSection
        {
            SectionId = "edu",
            SectionLabel = "Education",
            SortOrder = 2,
        };
        var record = new PersonLinkedDocumentRecord
        {
            RecordKey = "Education:1",
            RecordLabel = "Diploma",
            SourceObjectType = typeof(Education),
        };
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Education/Diploma.pdf",
        };

        var entry = PersonExportPacker.BuildEntryName(section, record, null, "en-US", used);

        Assert.Equal("Education/Diploma_2.pdf", entry);
    }
}
