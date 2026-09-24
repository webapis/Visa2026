using System;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationWorkspace;
using Visa2026.Module.Services.OfficerShell;
using Xunit;

namespace Visa2026.Module.Tests.Services.OfficerShell;

public class ApplicationProfileInstanceExclusionTests
{
    private static readonly int[] TwoLegs = [1, 2];

    [Theory]
    [InlineData("1_REVIEW_STARTED", ApplicationProfileInstanceExclusionAddresseeKind.Ministry, 1)]
    [InlineData("1_REVIEW_APPROVED", ApplicationProfileInstanceExclusionAddresseeKind.Ministry, 2)]
    [InlineData("2_REVIEW_REJECTED", ApplicationProfileInstanceExclusionAddresseeKind.Ministry, 2)]
    [InlineData("2_REVIEW_APPROVED", ApplicationProfileInstanceExclusionAddresseeKind.MigrationService, null)]
    [InlineData("PROCESS_STARTED", ApplicationProfileInstanceExclusionAddresseeKind.MigrationService, null)]
    [InlineData(null, ApplicationProfileInstanceExclusionAddresseeKind.MigrationService, null)]
    public void ResolveCurrentHolder_FollowsLatestProgressState(
        string? stateCode,
        ApplicationProfileInstanceExclusionAddresseeKind expectedKind,
        int? expectedLeg)
    {
        var holder = ApplicationProfileInstanceExclusionService.ResolveCurrentHolder(stateCode, TwoLegs);

        Assert.Equal(expectedKind, holder.Kind);
        Assert.Equal(expectedLeg, holder.Leg);
    }

    [Fact]
    public void ResolveCurrentHolder_SingleLegApproved_GoesToMigrationService()
    {
        var holder = ApplicationProfileInstanceExclusionService.ResolveCurrentHolder("1_REVIEW_APPROVED", [1]);

        Assert.Equal(ApplicationProfileInstanceExclusionAddresseeKind.MigrationService, holder.Kind);
        Assert.Null(holder.Leg);
    }

    [Theory]
    [InlineData("Türkmenistanyň Döwlet migrasiýa gullugy", "Türkmenistanyň Döwlet migrasiýa gullugyna")]
    [InlineData("Türkmenistanyň Energetika ministrligi", "Türkmenistanyň Energetika ministrligine")]
    public void TurkmenDative_AppendsHarmonizedSuffix(string name, string expected) =>
        Assert.Equal(expected, ApplicationProfileInstanceExclusionLetterBuilder.TurkmenDative(name));

    [Fact]
    public void TurkmenGenitive_AppendsHarmonizedSuffix() =>
        Assert.Equal(
            "Energetika ministrliginiň",
            ApplicationProfileInstanceExclusionLetterBuilder.TurkmenGenitive("Energetika ministrligi"));

    [Fact]
    public void DefaultTemplate_MergesLetterAndLoopsExcludedPeople()
    {
        var exclusion = new ApplicationProfileInstanceExclusion
        {
            LetterNumber = "01/-02",
            LetterDate = new DateTime(2026, 1, 15),
            AddresseeName = "Türkmenistanyň Döwlet migrasiýa gullugyna",
            ReferenceMinistryName = "Energetika ministrligi",
            ReferenceLetterDate = new DateTime(2026, 1, 15),
            ReferenceLetterNumber = "7/202",
            OriginalRosterCount = 13,
            Subject = "köp gezeklik wizalaryny we iş rugsatnamalaryny uzaltmak",
        };
        exclusion.People.Add(new ApplicationProfileInstanceExclusionPerson { Sequence = 1, FullName = "Enes Can Uzun", PassportNumber = "U34537060" });
        exclusion.People.Add(new ApplicationProfileInstanceExclusionPerson { Sequence = 2, FullName = "Celil Kocaeli", PassportNumber = "U32377166" });

        var data = ApplicationProfileInstanceExclusionLetterBuilder.BuildMergeData(exclusion, instance: null);
        var docx = ApplicationProfileInstanceExclusionLetterBuilder.Merge(
            ApplicationProfileInstanceExclusionLetterBuilder.BuildDefaultTemplate(),
            data);

        var text = ReadText(docx);
        Assert.DoesNotContain("{{", text, StringComparison.Ordinal);
        Assert.Contains("01/-02", text, StringComparison.Ordinal);
        Assert.Contains("15.01.2026", text, StringComparison.Ordinal);
        Assert.Contains("Energetika ministrliginiň", text, StringComparison.Ordinal);
        Assert.Contains("13 adamlyk", text, StringComparison.Ordinal);
        Assert.Contains("Enes Can Uzun – U34537060", text, StringComparison.Ordinal);
        Assert.Contains("Celil Kocaeli – U32377166", text, StringComparison.Ordinal);
        Assert.Contains("iki işgäriň", text, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildFileName_ReplacesInvalidCharacters() =>
        Assert.Equal(
            "Seretmezlik_01_-02.docx",
            ApplicationProfileInstanceExclusionLetterBuilder.BuildFileName(
                new ApplicationProfileInstanceExclusion { LetterNumber = "01/-02" }));

    [Fact]
    public void CaseView_ActivePeople_SkipsExcluded()
    {
        var view = new ApplicationWorkspaceCaseView
        {
            People =
            [
                new ApplicationWorkspaceCasePerson { Index = 1, PersonId = Guid.NewGuid(), Name = "A" },
                new ApplicationWorkspaceCasePerson
                {
                    Index = 2,
                    PersonId = Guid.NewGuid(),
                    Name = "B",
                    ExcludedLetterNumber = "01/-02",
                    ExcludedLetterDate = new DateTime(2026, 1, 15),
                },
            ],
        };

        Assert.Single(view.ActivePeople);
        Assert.Equal("A", view.ActivePeople[0].Name);
        Assert.Equal(1, view.ExcludedPeopleCount);
    }

    [Fact]
    public void DefaultRosterTemplate_RepeatsRowWithLivePersonFields()
    {
        var exclusion = new ApplicationProfileInstanceExclusion { LetterNumber = "01/-02", LetterDate = new DateTime(2026, 1, 15) };
        exclusion.People.Add(new ApplicationProfileInstanceExclusionPerson
        {
            Sequence = 1,
            FullName = "Enes Can Uzun",
            PassportNumber = "U34537060",
            Person = new Person { DateOfBirth = new DateTime(1990, 2, 1), Nationality = new Country { Name = "Turkey", NameTm = "Türkiýe" } },
        });
        exclusion.People.Add(new ApplicationProfileInstanceExclusionPerson { Sequence = 2, FullName = "Celil Kocaeli", PassportNumber = "U32377166" });

        var docx = ApplicationProfileInstanceExclusionLetterBuilder.Merge(
            ApplicationProfileInstanceExclusionLetterBuilder.BuildDefaultRosterTemplate(),
            ApplicationProfileInstanceExclusionLetterBuilder.BuildMergeData(exclusion, instance: null));

        var text = ReadText(docx);
        Assert.DoesNotContain("{{", text, StringComparison.Ordinal);
        Assert.Contains("SANAW", text, StringComparison.Ordinal);
        Assert.Contains("01.02.1990", text, StringComparison.Ordinal);
        Assert.Contains("Türkiýe", text, StringComparison.Ordinal);
        Assert.Contains("Celil Kocaeli", text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(ApplicationProfileInstanceExclusionTemplateKind.Letter)]
    [InlineData(ApplicationProfileInstanceExclusionTemplateKind.Roster)]
    public void ValidateTemplate_BuiltInTemplatesAreValid(ApplicationProfileInstanceExclusionTemplateKind kind)
    {
        var builtIn = ApplicationProfileInstanceExclusionLetterBuilder.GetBuiltInTemplate(kind);

        var validation = ApplicationProfileInstanceExclusionLetterBuilder.ValidateTemplate(kind, builtIn.FileName, builtIn.Content);

        Assert.True(validation.IsValid, string.Join("; ", validation.Errors.Concat(validation.UnknownTokens)));
    }

    [Fact]
    public void ValidateTemplate_LetterMustBeWord()
    {
        var validation = ApplicationProfileInstanceExclusionLetterBuilder.ValidateTemplate(
            ApplicationProfileInstanceExclusionTemplateKind.Letter, "letter.xlsx", BuildWorkbook(("A1", "{{ds.LetterNumber}}")));

        Assert.False(validation.IsValid);
    }

    [Fact]
    public void ValidateTemplate_ReportsUnknownPlaceholders()
    {
        var validation = ApplicationProfileInstanceExclusionLetterBuilder.ValidateTemplate(
            ApplicationProfileInstanceExclusionTemplateKind.Roster,
            "roster.xlsx",
            BuildWorkbook(("A1", "{{ds.Bogus}}"), ("A2", "{{#ds.People}}{{.Index}}"), ("B2", "{{.FullName}}{{/ds.People}}")));

        Assert.Equal(["{{ds.Bogus}}"], validation.UnknownTokens);
        Assert.False(validation.IsValid);
    }

    [Fact]
    public void ValidateTemplate_RosterWithoutLoopIsRejected()
    {
        var validation = ApplicationProfileInstanceExclusionLetterBuilder.ValidateTemplate(
            ApplicationProfileInstanceExclusionTemplateKind.Roster, "roster.xlsx", BuildWorkbook(("A1", "{{ds.LetterNumber}}")));

        Assert.NotEmpty(validation.Errors);
    }

    [Fact]
    public void MergeExcel_RepeatsLoopRowAndKeepsRowsBelow()
    {
        var template = BuildWorkbook(
            ("A1", "Sanaw № {{ds.LetterNumber}}"),
            ("A2", "{{#ds.People}}{{.Index}}"),
            ("B2", "{{.FullName}}"),
            ("C2", "{{.PassportNumber}}{{/ds.People}}"),
            ("A3", "{{ds.SignatoryName}}"));

        var merged = ApplicationProfileInstanceExclusionLetterBuilder.MergeExcel(
            template,
            ApplicationProfileInstanceExclusionLetterBuilder.BuildSampleMergeData());

        using var stream = new MemoryStream(merged);
        using var workbook = new ClosedXML.Excel.XLWorkbook(stream);
        var sheet = workbook.Worksheet(1);
        Assert.Equal("Sanaw № 01/-02", sheet.Cell("A1").GetString());
        Assert.Equal("1", sheet.Cell("A2").GetString());
        Assert.Equal("Enes Can Uzun", sheet.Cell("B2").GetString());
        Assert.Equal("U34537060", sheet.Cell("C2").GetString());
        Assert.Equal("2", sheet.Cell("A3").GetString());
        Assert.Equal("Mehmet ÇIRAK", sheet.Cell("A4").GetString());
    }

    private static byte[] BuildWorkbook(params (string Address, string Value)[] cells)
    {
        using var workbook = new ClosedXML.Excel.XLWorkbook();
        var sheet = workbook.AddWorksheet("Sanaw");
        foreach (var (address, value) in cells)
            sheet.Cell(address).Value = value;
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static string ReadText(byte[] docx)
    {
        using var stream = new MemoryStream(docx);
        using var document = WordprocessingDocument.Open(stream, false);
        var paragraphs = document.MainDocumentPart!.Document.Body!
            .Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>()
            .Select(p => p.InnerText);
        return string.Join("\n", paragraphs);
    }
}
