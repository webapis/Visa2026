using System;
using Microsoft.Extensions.Logging;
using Visa2026.Module.Services.PersonLinkedDocuments;
using Visa2026.Module.Tests.TestSupport;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Early validation gates for person document-copy PDF merge — must fail closed before ObjectSpace / PDF work.
/// </summary>
public class PersonDocumentCopyPdfMergerGateTests
{
    private static PersonDocumentCopyPdfMerger CreateMerger() =>
        new(null!, new NoOpLogger());

    [Fact]
    public void TryBuildMergedPdf_EmptyPersonId_ReturnsFalse()
    {
        var ok = CreateMerger().TryBuildMergedPdf(
            Guid.Empty,
            "Passport.",
            "Passport",
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
            Guid.NewGuid(),
            recordKey!,
            "Passport",
            out _,
            out _));
    }

    [Fact]
    public void TryBuildMergedPdf_NullObjectSpace_ReturnsFalse()
    {
        var snapshot = new PersonLinkedDocumentsSnapshot { PersonId = Guid.NewGuid() };

        Assert.False(CreateMerger().TryBuildMergedPdf(
            null!,
            snapshot,
            "Passport.",
            "Passport",
            out _,
            out _));
    }

    [Fact]
    public void TryBuildMergedPdf_NullSnapshot_ReturnsFalse()
    {
        Assert.False(CreateMerger().TryBuildMergedPdf(
            DisposeOnlyObjectSpaceStub.Create(),
            null!,
            "Passport.",
            "Passport",
            out _,
            out _));
    }

    [Fact]
    public void TryBuildMergedPdf_UnknownRecordKey_ReturnsFalse()
    {
        var snapshot = new PersonLinkedDocumentsSnapshot
        {
            PersonId = Guid.NewGuid(),
            Sections =
            [
                new PersonLinkedDocumentSection
                {
                    SectionId = "passports",
                    Records =
                    [
                        new PersonLinkedDocumentRecord
                        {
                            RecordKey = "Passport.other",
                            Files =
                            [
                                new PersonLinkedDocumentFile
                                {
                                    FileDataId = Guid.NewGuid(),
                                    HasContent = true,
                                },
                            ],
                        },
                    ],
                },
            ],
        };

        Assert.False(CreateMerger().TryBuildMergedPdf(
            DisposeOnlyObjectSpaceStub.Create(),
            snapshot,
            "Passport.missing",
            "Passport",
            out _,
            out _));
    }

    [Fact]
    public void TryBuildMergedPdf_RecordWithNoUsableFiles_ReturnsFalse()
    {
        var snapshot = new PersonLinkedDocumentsSnapshot
        {
            PersonId = Guid.NewGuid(),
            Sections =
            [
                new PersonLinkedDocumentSection
                {
                    SectionId = "passports",
                    Records =
                    [
                        new PersonLinkedDocumentRecord
                        {
                            RecordKey = "Passport.",
                            Files =
                            [
                                new PersonLinkedDocumentFile
                                {
                                    FileDataId = Guid.Empty,
                                    HasContent = true,
                                },
                                new PersonLinkedDocumentFile
                                {
                                    FileDataId = Guid.NewGuid(),
                                    HasContent = false,
                                },
                            ],
                        },
                    ],
                },
            ],
        };

        Assert.False(CreateMerger().TryBuildMergedPdf(
            DisposeOnlyObjectSpaceStub.Create(),
            snapshot,
            "Passport.",
            "Passport",
            out _,
            out _));
    }

    private sealed class NoOpLogger : ILogger<PersonDocumentCopyPdfMerger>
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
