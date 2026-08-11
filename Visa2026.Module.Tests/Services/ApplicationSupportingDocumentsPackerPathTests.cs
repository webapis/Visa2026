using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public sealed class ApplicationSupportingDocumentsPackerPathTests
{
    [Fact]
    public void BuildItemSlug_FormatsIndexAndSanitizesNames()
    {
        var person = new Person
        {
            FirstName = "Ada Lovelace",
            LastName = "Path/Seg"
        };

        var slug = ApplicationSupportingDocumentsPacker.BuildItemSlug(3, person);

        Assert.Equal("03_Ada_Lovelace_Path_Seg", slug);
    }

    [Fact]
    public void BuildItemSlug_NullPersonParts_UseNA()
    {
        var slug = ApplicationSupportingDocumentsPacker.BuildItemSlug(1, new Person());

        Assert.Equal("01_NA_NA", slug);
    }

    [Fact]
    public void ReserveZipEntryPath_FirstClaimWins_ThenSuffixesDuplicates()
    {
        var reserved = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var first = ApplicationSupportingDocumentsPacker.ReserveZipEntryPath(reserved, "Passport/scan.pdf");
        var second = ApplicationSupportingDocumentsPacker.ReserveZipEntryPath(reserved, "Passport\\scan.pdf");
        var third = ApplicationSupportingDocumentsPacker.ReserveZipEntryPath(reserved, "Passport/scan.pdf");

        Assert.Equal("Passport/scan.pdf", first);
        Assert.Equal("Passport/scan_2.pdf", second);
        Assert.Equal("Passport/scan_3.pdf", third);
        Assert.Equal(3, reserved.Count);
    }

    [Fact]
    public void ReserveZipEntryPath_TrimsOverlongPaths()
    {
        var reserved = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var longStem = new string('a', 240);
        var path = $"Diplomas/{longStem}.pdf";

        var reservedPath = ApplicationSupportingDocumentsPacker.ReserveZipEntryPath(reserved, path);

        Assert.True(reservedPath.Length <= 220);
        Assert.EndsWith(".pdf", reservedPath, StringComparison.Ordinal);
        Assert.Contains("_tr", reservedPath, StringComparison.Ordinal);
        Assert.Contains(reservedPath, reserved);
    }

    [Fact]
    public void PackagingNotesConstant_IsArchiveRootFile()
    {
        Assert.Equal("PACKAGING_NOTES.txt", ApplicationSupportingDocumentsPacker.PackagingNotesZipRelativePath);
        Assert.Equal("PDF_Form", ApplicationSupportingDocumentsPacker.FilledApplicationFormsZipFolderName);
        Assert.Equal("Passport/CurrentPassports.pdf", ApplicationSupportingDocumentsPacker.MergedCurrentPassportsZipRelativePath);
    }
}
