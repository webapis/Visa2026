using System;
using System.Collections.Generic;
using System.Linq;
using Visa2026.Module.Localization;
using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public sealed class ApplicationSupportingDocumentsPackerNotesTests
{
    [Fact]
    public void BuildPackagingNotesText_EmptyGaps_WritesNoGapsLine()
    {
        var batchId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var completed = new DateTime(2026, 8, 12, 10, 0, 0, DateTimeKind.Utc);

        var text = ApplicationSupportingDocumentsPacker.BuildPackagingNotesText(
            Array.Empty<string>(),
            batchId,
            applicationId: null,
            completed,
            packagingCulture: "en-US");

        Assert.Contains("Visa2026 — PDF batch packaging notes", text);
        Assert.Contains($"Batch ID: {batchId}", text);
        Assert.DoesNotContain("Application ID:", text);
        Assert.Contains("Completed (UTC): 2026-08-12T10:00:00.0000000Z", text);
        Assert.Contains(
            VisaUiMessages.Get("Pdf.Packaging.NoGaps", "en-US"),
            text);
        Assert.DoesNotContain(
            VisaUiMessages.Get("Pdf.Packaging.GapsHeader", "en-US"),
            text);
    }

    [Fact]
    public void BuildPackagingNotesText_NullGaps_TreatedAsEmpty()
    {
        var text = ApplicationSupportingDocumentsPacker.BuildPackagingNotesText(
            null!,
            Guid.NewGuid(),
            applicationId: Guid.NewGuid(),
            DateTime.UtcNow,
            packagingCulture: "en-US");

        Assert.Contains("Application ID:", text);
        Assert.Contains(VisaUiMessages.Get("Pdf.Packaging.NoGaps", "en-US"), text);
    }

    [Fact]
    public void BuildPackagingNotesText_SkipsWhitespaceGapLines_AndBulletFormats()
    {
        var text = ApplicationSupportingDocumentsPacker.BuildPackagingNotesText(
            new List<string> { "  Missing passport scan  ", "   ", "", "Education diploma empty" },
            Guid.NewGuid(),
            applicationId: null,
            DateTime.UtcNow,
            packagingCulture: "en-US");

        Assert.Contains(VisaUiMessages.Get("Pdf.Packaging.GapsHeader", "en-US"), text);
        Assert.Contains("- Missing passport scan", text);
        Assert.Contains("- Education diploma empty", text);
        Assert.DoesNotContain("-    ", text);
        Assert.Equal(2, text.Split('\n').Count(line => line.StartsWith("- ", StringComparison.Ordinal)));
    }

    [Fact]
    public void BuildPackagingNotesText_UsesRequestedCulture()
    {
        var text = ApplicationSupportingDocumentsPacker.BuildPackagingNotesText(
            Array.Empty<string>(),
            Guid.NewGuid(),
            applicationId: null,
            DateTime.UtcNow,
            packagingCulture: "tr-TR");

        Assert.Contains(VisaUiMessages.Get("Pdf.Packaging.Header", "tr-TR"), text);
        Assert.Contains(VisaUiMessages.Get("Pdf.Packaging.NoGaps", "tr-TR"), text);
    }

    [Fact]
    public void BuildPackagingNotesText_TruncatesAtMaxChars_WithFooter()
    {
        var footer = VisaUiMessages.Get("Pdf.Packaging.TruncatedFooter", "en-US");
        var hugeLine = new string('x', 10_000);
        var gaps = Enumerable.Range(0, 20).Select(_ => hugeLine).ToList();

        var text = ApplicationSupportingDocumentsPacker.BuildPackagingNotesText(
            gaps,
            Guid.NewGuid(),
            applicationId: null,
            DateTime.UtcNow,
            packagingCulture: "en-US");

        Assert.True(text.Length <= 120_000 + footer.Length);
        Assert.EndsWith(footer, text);
        Assert.True(text.Length >= 120_000 - footer.Length);
    }
}
