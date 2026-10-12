using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Happy-path raster→PDF for document-copy slices (FitA4 + PassportPage).
/// Gate coverage lives in open PR #64.
/// </summary>
public class SupportingDocumentsPdfSharpHelperRasterSuccessTests
{
    // 1×1 PNG (valid raster for PdfSharpCore / Spire fallback).
    private static readonly byte[] TinyPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    private static readonly ILogger Logger = new NoOpLogger();

    [Fact]
    public void TryWriteSinglePagePdfFromRasterBytes_FitA4_WritesPdf()
    {
        using var output = new MemoryStream();

        Assert.True(SupportingDocumentsPdfSharpHelper.TryWriteSinglePagePdfFromRasterBytes(
            TinyPng,
            output,
            Logger,
            DocumentCopyPrintLayout.FitA4));
        Assert.True(output.Length > 0);
        Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString(output.ToArray().AsSpan(0, 4)));
    }

    [Fact]
    public void TryWriteSlice_PassportPage_Raster_WritesPdf()
    {
        using var output = new MemoryStream();
        var layout = DocumentCopyPrintLayout.DefaultFor("Passport.");

        Assert.True(SupportingDocumentsPdfSharpHelper.TryWriteSlice(
            TinyPng,
            output,
            Logger,
            layout));
        Assert.True(output.Length > 0);
        Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString(output.ToArray().AsSpan(0, 4)));
    }

    private sealed class NoOpLogger : ILogger
    {
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => false;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }
}
