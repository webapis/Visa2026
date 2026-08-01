using System;
using System.Collections.Generic;
using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Shared ZIP leaf naming for Resminamalar bundles and person export packages.
/// </summary>
public sealed class ZipEntryFileNameSanitizerTests
{
    [Theory]
    [InlineData("3/-433", "3_-433")]
    [InlineData("report<>name", "report_name")]
    [InlineData("  spaced  ", "spaced")]
    public void Sanitize_ReplacesInvalidAndSlashCharacters(string input, string expected)
    {
        Assert.Equal(expected, ZipEntryFileNameSanitizer.Sanitize(input));
    }

    [Fact]
    public void Sanitize_Empty_ReturnsFallback()
    {
        Assert.Equal("report.bin", ZipEntryFileNameSanitizer.Sanitize("   "));
    }

    [Fact]
    public void BuildReportEntryName_StripsDocxExtensionFromLabel()
    {
        var name = ZipEntryFileNameSanitizer.BuildReportEntryName("GT-15_Elyasow_uzt.docx", ".docx");

        Assert.Equal("GT-15_Elyasow_uzt.docx", name);
    }

    [Fact]
    public void ToBundleEntryName_StripsApplicationNumberAndDateSuffix()
    {
        var name = ZipEntryFileNameSanitizer.ToBundleEntryName(
            "Forma_16_3_-433_20260801.docx",
            "3/-433");

        Assert.Equal("Forma_16.docx", name);
    }

    [Fact]
    public void EnsureUnique_AddsSuffixOnCollision()
    {
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var first = ZipEntryFileNameSanitizer.EnsureUnique("Report.docx", used);
        var second = ZipEntryFileNameSanitizer.EnsureUnique("Report.docx", used);

        Assert.Equal("Report.docx", first);
        Assert.Equal("Report_2.docx", second);
    }

    [Fact]
    public void FlattenApplicationNumber_Blank_ReturnsAPP()
    {
        Assert.Equal("APP", ZipEntryFileNameSanitizer.FlattenApplicationNumber(" "));
    }
}
