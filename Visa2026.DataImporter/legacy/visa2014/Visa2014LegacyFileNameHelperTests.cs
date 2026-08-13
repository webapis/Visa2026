using System.Text;
using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

public class Visa2014LegacyFileNameHelperTests
{
    private static readonly byte[] PdfMagic = Encoding.ASCII.GetBytes("%PDF-1.4");
    private static readonly byte[] JpegMagic = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10];
    private static readonly byte[] PngMagic =
    [
        0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A
    ];

    [Theory]
    [InlineData(null, "unknown")]
    [InlineData("", "unknown")]
    [InlineData("   ", "unknown")]
    [InlineData("AB/CD:EF", "AB-CD-EF")]
    [InlineData("  pass--port  ", "pass-port")]
    [InlineData("Şäherýoly", "Saheryoly")]
    public void SanitizeToken_StripsDiacriticsAndInvalidChars(string? raw, string expected)
    {
        Assert.Equal(expected, Visa2014LegacyFileNameHelper.SanitizeToken(raw, "unknown"));
    }

    [Fact]
    public void BuildPassportCopyFileName_DefaultIndex_PdfExtension()
    {
        var name = Visa2014LegacyFileNameHelper.BuildPassportCopyFileName("P123", PdfMagic);

        Assert.Equal("passport-P123-copy.pdf", name);
    }

    [Fact]
    public void BuildVisaCopyFileName_IndexAboveOne_AddsSuffixAndJpeg()
    {
        var name = Visa2014LegacyFileNameHelper.BuildVisaCopyFileName("V-9", JpegMagic, copyIndex: 2);

        Assert.Equal("visa-V-9-copy-2.jpg", name);
    }

    [Fact]
    public void BuildDiplomaCopyFileName_UnknownToken_WhenNameBlank()
    {
        var name = Visa2014LegacyFileNameHelper.BuildDiplomaCopyFileName("  ", PngMagic);

        Assert.Equal("diploma-unknown-copy.png", name);
    }

    [Fact]
    public void BuildWorkPermitCopyFileName_DefaultsUnknownContentToPdf()
    {
        var name = Visa2014LegacyFileNameHelper.BuildWorkPermitCopyFileName("WP1", [0x00, 0x01]);

        Assert.Equal("work-permit-WP1-copy.pdf", name);
    }

    [Fact]
    public void BuildInvitationAndFamilyProof_UsePrefixes()
    {
        Assert.Equal(
            "invitation-INV1-copy.pdf",
            Visa2014LegacyFileNameHelper.BuildInvitationCopyFileName("INV1", PdfMagic));
        Assert.Equal(
            "family-proof-Ada-copy.pdf",
            Visa2014LegacyFileNameHelper.BuildFamilyProofCopyFileName("Ada", PdfMagic));
        Assert.Equal(
            "medical-Ada-copy.pdf",
            Visa2014LegacyFileNameHelper.BuildMedicalCopyFileName("Ada", PdfMagic));
    }
}
