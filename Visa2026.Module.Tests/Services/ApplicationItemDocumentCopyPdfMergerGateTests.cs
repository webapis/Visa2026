using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Visa2026.Module.Services.ApplicationItemLinkedDocuments;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Early validation gates for document-copy PDF merge / image-source load — must fail closed before ObjectSpace work.
/// </summary>
public class ApplicationItemDocumentCopyPdfMergerGateTests
{
    private static ApplicationItemDocumentCopyPdfMerger CreateMerger() =>
        new(null!, new NoOpLogger());

    [Fact]
    public void TryBuildMergedPdfForRoster_NullPersonIds_ReturnsFalse()
    {
        var ok = CreateMerger().TryBuildMergedPdfForRoster(
            null!,
            "Passport",
            "Passport",
            [DummyEntry()],
            out var content,
            out var fileName);

        Assert.False(ok);
        Assert.Null(content);
        Assert.Null(fileName);
    }

    [Fact]
    public void TryBuildMergedPdfForRoster_EmptyPersonIds_ReturnsFalse()
    {
        Assert.False(CreateMerger().TryBuildMergedPdfForRoster(
            Array.Empty<Guid>(),
            "Passport",
            "Passport",
            [DummyEntry()],
            out _,
            out _));
    }

    [Fact]
    public void TryBuildMergedPdfForRoster_BlankSlotKey_ReturnsFalse()
    {
        Assert.False(CreateMerger().TryBuildMergedPdfForRoster(
            [Guid.NewGuid()],
            "  ",
            "Passport",
            [DummyEntry()],
            out _,
            out _));
    }

    [Fact]
    public void TryBuildMergedPdfForRoster_OnlyEmptyGuids_ReturnsFalse()
    {
        Assert.False(CreateMerger().TryBuildMergedPdfForRoster(
            [Guid.Empty, Guid.Empty],
            "Passport",
            "Passport",
            [DummyEntry()],
            out _,
            out _));
    }

    [Fact]
    public void TryBuildMergedPdfForRoster_NullEntries_ReturnsFalse()
    {
        Assert.False(CreateMerger().TryBuildMergedPdfForRoster(
            [Guid.NewGuid()],
            "Passport",
            "Passport",
            null!,
            out _,
            out _));
    }

    [Fact]
    public void TryLoadValidatedSourceFiles_EmptyEntries_ReturnsFalse()
    {
        var ok = CreateMerger().TryLoadValidatedSourceFiles(
            [Guid.NewGuid()],
            Array.Empty<ApplicationItemLinkedDocumentFileEntry>(),
            Guid.NewGuid(),
            out var sources);

        Assert.False(ok);
        Assert.Empty(sources);
    }

    [Fact]
    public void TryLoadValidatedSourceFiles_NullPersonIds_ReturnsFalse()
    {
        var ok = CreateMerger().TryLoadValidatedSourceFiles(
            null!,
            [DummyEntry()],
            Guid.NewGuid(),
            out var sources);

        Assert.False(ok);
        Assert.Empty(sources);
    }

    [Fact]
    public void TryLoadValidatedSourceFiles_OnlyEmptyGuids_ReturnsFalse()
    {
        var ok = CreateMerger().TryLoadValidatedSourceFiles(
            [Guid.Empty],
            [DummyEntry()],
            Guid.NewGuid(),
            out var sources);

        Assert.False(ok);
        Assert.Empty(sources);
    }

    private static ApplicationItemLinkedDocumentFileEntry DummyEntry() =>
        new()
        {
            ApplicationItemId = Guid.NewGuid(),
            File = new ApplicationItemLinkedDocumentFile
            {
                FileDataId = Guid.NewGuid(),
                FileName = "scan.jpg",
            },
        };

    private sealed class NoOpLogger : ILogger<ApplicationItemDocumentCopyPdfMerger>
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
