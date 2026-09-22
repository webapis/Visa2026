using System;
using System.Collections.Generic;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;
using Visa2026.Module.Services.PersonDossier;
using Visa2026.Module.Services.PersonLinkedDocuments;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Director hand-over ZIP entry naming: visa folder override, leaf from record label, uniqueness.
/// </summary>
public sealed class PersonExportPackerTests
{
    [Fact]
    public void BuildEntryName_UsesSectionLabelAndRecordLabel()
    {
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var section = new PersonLinkedDocumentSection
        {
            SectionId = "Passports",
            SectionLabel = "Passports"
        };
        var record = new PersonLinkedDocumentRecord
        {
            RecordKey = "passport-1",
            RecordLabel = "Passport U40412139",
            SourceObjectType = typeof(Passport)
        };

        var entry = PersonExportPacker.BuildEntryName(section, record, "scan.pdf", "en-US", used);

        Assert.Equal("Passports/Passport U40412139.pdf", entry);
        Assert.Contains(entry, used);
    }

    [Fact]
    public void BuildEntryName_VisaNestedUnderPassport_UsesVisasFolder()
    {
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var section = new PersonLinkedDocumentSection
        {
            SectionId = "Passports",
            SectionLabel = "Passports"
        };
        var record = new PersonLinkedDocumentRecord
        {
            RecordKey = "visa-1",
            RecordLabel = "Visa A1742149",
            SourceObjectType = typeof(Visa),
            IsNested = true
        };

        var entry = PersonExportPacker.BuildEntryName(section, record, "opaque-upload.pdf", "en-US", used);

        string visasFolder = VisaUiMessages.Get("PersonDocumentCopies.Section.Visas", "en-US");
        Assert.Equal($"{visasFolder}/Visa A1742149.pdf", entry);
        Assert.StartsWith("Visas/", entry, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildEntryName_DuplicateLeafInSameFolder_AddsNumericSuffix()
    {
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var section = new PersonLinkedDocumentSection
        {
            SectionId = "Education",
            SectionLabel = "Education"
        };
        var first = new PersonLinkedDocumentRecord
        {
            RecordKey = "edu-1",
            RecordLabel = "Diploma"
        };
        var second = new PersonLinkedDocumentRecord
        {
            RecordKey = "edu-2",
            RecordLabel = "Diploma"
        };

        var a = PersonExportPacker.BuildEntryName(section, first, null, "en-US", used);
        var b = PersonExportPacker.BuildEntryName(section, second, null, "en-US", used);

        Assert.Equal("Education/Diploma.pdf", a);
        Assert.Equal("Education/Diploma_2.pdf", b);
    }

    [Fact]
    public void BuildEntryName_SameLeafInDifferentFolders_BothAllowed()
    {
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var passports = new PersonLinkedDocumentSection { SectionLabel = "Passports" };
        var education = new PersonLinkedDocumentSection { SectionLabel = "Education" };
        var record = new PersonLinkedDocumentRecord { RecordLabel = "Scan" };

        var a = PersonExportPacker.BuildEntryName(passports, record, null, "en-US", used);
        var b = PersonExportPacker.BuildEntryName(education, record, null, "en-US", used);

        Assert.Equal("Passports/Scan.pdf", a);
        Assert.Equal("Education/Scan.pdf", b);
    }

    [Fact]
    public void BuildEntryName_EmptyRecordLabel_FallsBackToMergedFileName()
    {
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var section = new PersonLinkedDocumentSection { SectionLabel = "Documents" };
        var record = new PersonLinkedDocumentRecord { RecordLabel = "   " };

        var entry = PersonExportPacker.BuildEntryName(section, record, "merged-scan.pdf", "en-US", used);

        Assert.Equal("Documents/merged-scan.pdf", entry);
    }

    [Fact]
    public void BuildZipFileName_UsesSanitizedPersonNameAndStamp()
    {
        var person = new Person
        {
            FirstName = "Serdar",
            LastName = "Ashirov"
        };
        var stamp = new DateTime(2026, 8, 1, 10, 5, 0);

        var name = PersonExportPacker.BuildZipFileName(person, stamp);

        Assert.Equal("Dossier_Serdar Ashirov_20260801_1005.zip", name);
    }

    [Fact]
    public void BuildZipFileName_EmptyName_FallsBackToPerson()
    {
        var person = new Person();
        var stamp = new DateTime(2026, 1, 2, 3, 4, 0);

        var name = PersonExportPacker.BuildZipFileName(person, stamp);

        Assert.Equal("Dossier_Person_20260102_0304.zip", name);
    }
}
