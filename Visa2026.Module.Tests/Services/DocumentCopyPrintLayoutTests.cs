using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class DocumentCopyPrintLayoutTests
{
    [Fact]
    public void Passport_KeepsOriginalSize_AlongBottom()
    {
        var layout = DocumentCopyPrintLayout.DefaultFor("Passport.");
        var rect = layout.Measure(200, 120);

        Assert.Equal(DocumentCopyPrintSize.PassportPage, layout.Size);
        Assert.Equal(DocumentCopyPageOrientation.Landscape, layout.Orientation);
        Assert.Equal(DocumentCopyPrintLayout.A4HeightPt, rect.PageWidthPt);
        Assert.Equal(DocumentCopyPrintLayout.A4WidthPt, rect.PageHeightPt);
        Assert.Equal(200, rect.Width, 1);
        Assert.Equal(120, rect.Height, 1);
        Assert.Equal(DocumentCopyPrintLayout.FitMarginPt, rect.X, 1);
        Assert.Equal(
            DocumentCopyPrintLayout.A4WidthPt - DocumentCopyPrintLayout.FitMarginPt - 120,
            rect.Y,
            1);
        Assert.True(rect.X + rect.Width < DocumentCopyPrintLayout.A4HeightPt / 2);
    }

    [Fact]
    public void Visa_KeepsOriginalSize_AlongBottom()
    {
        var layout = DocumentCopyPrintLayout.DefaultFor("Visa.");
        var rect = layout.Measure(180, 100);

        Assert.Equal(DocumentCopyPrintSize.Visa, layout.Size);
        Assert.Equal(DocumentCopyPageOrientation.Landscape, layout.Orientation);
        Assert.Equal(DocumentCopyPrintLayout.A4HeightPt, rect.PageWidthPt);
        Assert.Equal(DocumentCopyPrintLayout.A4WidthPt, rect.PageHeightPt);
        Assert.Equal(180, rect.Width, 1);
        Assert.Equal(100, rect.Height, 1);
        Assert.Equal(
            DocumentCopyPrintLayout.A4WidthPt - DocumentCopyPrintLayout.FitMarginPt - 100,
            rect.Y,
            1);
    }

    [Fact]
    public void OversizedScan_ShrinksToFit_StillAlongBottom()
    {
        var layout = DocumentCopyPrintLayout.DefaultFor("Passport.");
        var rect = layout.Measure(2000, 1000);

        double maxW = DocumentCopyPrintLayout.A4HeightPt - 2 * DocumentCopyPrintLayout.FitMarginPt;
        Assert.Equal(maxW, rect.Width, 1);
        Assert.Equal(maxW / 2, rect.Height, 1);
        Assert.Equal(DocumentCopyPrintLayout.FitMarginPt, rect.X, 1);
        Assert.Equal(
            DocumentCopyPrintLayout.A4WidthPt - DocumentCopyPrintLayout.FitMarginPt - rect.Height,
            rect.Y,
            1);
    }

    [Fact]
    public void Rotate90_KeepsOriginalSize_AlongBottom()
    {
        var layout = DocumentCopyPrintLayout.DefaultFor("Passport:abc").Rotate(90);
        var rect = layout.Measure(120, 200);

        Assert.Equal(90, rect.RotationDegrees);
        Assert.Equal(200, rect.Width, 1);
        Assert.Equal(120, rect.Height, 1);
        Assert.Equal(
            DocumentCopyPrintLayout.A4WidthPt - DocumentCopyPrintLayout.FitMarginPt - 120,
            rect.Y,
            1);
    }

    [Fact]
    public void Landscape_UsesLandscapeA4_StillAlongBottom()
    {
        var layout = new DocumentCopyPrintLayout(
            DocumentCopyPrintSize.PassportPage,
            0,
            DocumentCopyPageOrientation.Landscape);
        var rect = layout.Measure(200, 120);

        Assert.Equal(DocumentCopyPrintLayout.A4HeightPt, rect.PageWidthPt);
        Assert.Equal(DocumentCopyPrintLayout.A4WidthPt, rect.PageHeightPt);
        Assert.Equal(200, rect.Width, 1);
        Assert.Equal(120, rect.Height, 1);
        Assert.Equal(
            DocumentCopyPrintLayout.A4WidthPt - DocumentCopyPrintLayout.FitMarginPt - 120,
            rect.Y,
            1);
    }

    [Theory]
    [InlineData("Passport.", true, false)]
    [InlineData("Passport:abc", true, false)]
    [InlineData("CurrentPassports", true, false)]
    [InlineData("Visa.", false, true)]
    [InlineData("Passport:abc/Visa:def", false, true)]
    [InlineData("VisaDocument:abc", false, true)]
    [InlineData("CurrentVisas", false, true)]
    [InlineData("Education.", false, false)]
    [InlineData("Invitation.", false, false)]
    public void AppliesTo_PassportAndVisaOnly(string key, bool passport, bool visa)
    {
        Assert.Equal(passport, DocumentCopyPrintLayout.IsPassportKey(key));
        Assert.Equal(visa, DocumentCopyPrintLayout.IsVisaKey(key));
        Assert.Equal(passport || visa, DocumentCopyPrintLayout.AppliesTo(key));
    }

    [Fact]
    public void Education_StillFitsA4()
    {
        var rect = DocumentCopyPrintLayout.DefaultFor("Education.").Measure(1000, 1400);
        Assert.Equal(DocumentCopyPrintLayout.A4WidthPt, rect.PageWidthPt);
        Assert.Equal(DocumentCopyPrintLayout.A4HeightPt, rect.PageHeightPt);
        Assert.True(rect.Height > DocumentCopyPrintLayout.MillimetersToPoints(200));
    }
}
