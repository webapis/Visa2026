using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Fail-closed gates for document-copy PDF slice helpers (empty payloads / FitA4 cannot reflow).
/// Covers the shared path used by person, header, and ApplicationItem mergers after print-layout work.
/// </summary>
public class SupportingDocumentsPdfSharpHelperGateTests
{
    private static readonly ILogger Logger = new NoOpLogger();

    [Fact]
    public void TryWriteSlice_EmptyContent_ReturnsFalse()
    {
        using var output = new MemoryStream();
        Assert.False(SupportingDocumentsPdfSharpHelper.TryWriteSlice(
            Array.Empty<byte>(),
            output,
            Logger,
            DocumentCopyPrintLayout.DefaultFor("Passport.")));
        Assert.Equal(0, output.Length);
    }

    [Fact]
    public void TryWriteSinglePagePdfFromRasterBytes_EmptyContent_ReturnsFalse()
    {
        using var output = new MemoryStream();
        Assert.False(SupportingDocumentsPdfSharpHelper.TryWriteSinglePagePdfFromRasterBytes(
            Array.Empty<byte>(),
            output,
            Logger,
            DocumentCopyPrintLayout.FitA4));
        Assert.Equal(0, output.Length);
    }

    [Fact]
    public void TryWritePdfPagesAtPrintLayout_FitA4_ReturnsFalse()
    {
        using var output = new MemoryStream();
        var pdfish = "%PDF-1.4 fake-bytes"u8.ToArray();

        Assert.False(SupportingDocumentsPdfSharpHelper.TryWritePdfPagesAtPrintLayout(
            pdfish,
            output,
            Logger,
            DocumentCopyPrintLayout.FitA4));
        Assert.Equal(0, output.Length);
    }

    [Fact]
    public void MergePdfStreams_NullSources_Throws()
    {
        using var output = new MemoryStream();
        Assert.Throws<ArgumentNullException>(() =>
            SupportingDocumentsPdfSharpHelper.MergePdfStreams<MemoryStream>(null!, output));
    }

    [Fact]
    public void MergePdfStreams_EmptyList_Throws()
    {
        using var output = new MemoryStream();
        Assert.Throws<InvalidOperationException>(() =>
            SupportingDocumentsPdfSharpHelper.MergePdfStreams(Array.Empty<MemoryStream>(), output));
    }

    [Fact]
    public void MergePdfStreams_NullOutput_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            SupportingDocumentsPdfSharpHelper.MergePdfStreams(
                Array.Empty<MemoryStream>(),
                null!));
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
