using System;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Application form field 30 (last issued visa) formatting — regression coverage for empty parts and period edges.
/// </summary>
public class PdfLastIssuedVisaTextTests
{
    [Fact]
    public void Format_NullVisa_ReturnsNull()
    {
        Assert.Null(PdfLastIssuedVisaText.Format(null!));
    }

    [Fact]
    public void Format_EmptyVisa_ReturnsNull()
    {
        Assert.Null(PdfLastIssuedVisaText.Format(new Visa()));
    }

    [Fact]
    public void Format_PrefersNameTmOverLocalizationKey()
    {
        var visa = new Visa
        {
            VisaType = new VisaType { NameTm = "Işewürlik", LocalizationKey = "BS1" },
            VisaCategory = new VisaCategory { NameTm = "Bir gezeklik", LocalizationKey = "single" },
            VisaNumber = "  V-100  ",
            StartDate = new DateTime(2024, 1, 15),
            ExpirationDate = new DateTime(2024, 7, 15),
        };

        Assert.Equal("Işewürlik, Bir gezeklik, V-100, 15.01.2024-15.07.2024", PdfLastIssuedVisaText.Format(visa));
    }

    [Fact]
    public void Format_FallsBackToLocalizationKeyWhenNameTmBlank()
    {
        var visa = new Visa
        {
            VisaType = new VisaType { NameTm = "  ", LocalizationKey = "TR2" },
            VisaCategory = new VisaCategory { LocalizationKey = "multi" },
        };

        Assert.Equal("TR2, multi", PdfLastIssuedVisaText.Format(visa));
    }

    [Fact]
    public void Format_Period_StartOnly()
    {
        var visa = new Visa { StartDate = new DateTime(2025, 3, 1) };

        Assert.Equal("01.03.2025", PdfLastIssuedVisaText.Format(visa));
    }

    [Fact]
    public void Format_Period_EndOnly()
    {
        var visa = new Visa { ExpirationDate = new DateTime(2025, 12, 31) };

        Assert.Equal("31.12.2025", PdfLastIssuedVisaText.Format(visa));
    }

    [Fact]
    public void Format_NumberOnly_TrimsWhitespace()
    {
        var visa = new Visa { VisaNumber = "\tAB-9\n" };

        Assert.Equal("AB-9", PdfLastIssuedVisaText.Format(visa));
    }
}
