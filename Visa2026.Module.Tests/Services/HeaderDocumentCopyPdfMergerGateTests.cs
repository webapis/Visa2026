using System;
using Microsoft.Extensions.Logging;
using Visa2026.Module.Services.HeaderLinkedDocuments;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Early validation gates for header (Invitation/WP/Rejection/BorderZone/Visa) document-copy PDF merge.
/// </summary>
public class HeaderDocumentCopyPdfMergerGateTests
{
    private static HeaderDocumentCopyPdfMerger CreateMerger() =>
        new(null!, new NoOpLogger());

    [Theory]
    [InlineData(HeaderDocumentCopiesFamily.Invitation)]
    [InlineData(HeaderDocumentCopiesFamily.WorkPermit)]
    [InlineData(HeaderDocumentCopiesFamily.Rejection)]
    [InlineData(HeaderDocumentCopiesFamily.BorderZone)]
    [InlineData(HeaderDocumentCopiesFamily.Visa)]
    public void TryBuildMergedPdf_EmptyParentId_ReturnsFalse(HeaderDocumentCopiesFamily family)
    {
        var ok = CreateMerger().TryBuildMergedPdf(
            family,
            Guid.Empty,
            "Invitation.",
            "Invitation",
            out var content,
            out var fileName);

        Assert.False(ok);
        Assert.Null(content);
        Assert.Null(fileName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryBuildMergedPdf_BlankRecordKey_ReturnsFalse(string? recordKey)
    {
        Assert.False(CreateMerger().TryBuildMergedPdf(
            HeaderDocumentCopiesFamily.Invitation,
            Guid.NewGuid(),
            recordKey!,
            "Invitation",
            out _,
            out _));
    }

    [Fact]
    public void TryBuildMergedPdf_UnknownFamily_EmptyParentStillFailsClosed()
    {
        // Unknown enum values never create an ObjectSpace; empty parentId still short-circuits first.
        Assert.False(CreateMerger().TryBuildMergedPdf(
            (HeaderDocumentCopiesFamily)999,
            Guid.Empty,
            "Invitation.",
            "Invitation",
            out _,
            out _));
    }

    private sealed class NoOpLogger : ILogger<HeaderDocumentCopyPdfMerger>
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
