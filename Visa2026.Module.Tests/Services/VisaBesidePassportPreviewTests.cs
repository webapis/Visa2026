using System;
using System.IO;
using Microsoft.Extensions.Logging.Abstractions;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;
using Visa2026.Module.Services.ApplicationItemLinkedDocuments;
using Visa2026.Module.Services.PersonLinkedDocuments;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class VisaBesidePassportPreviewTests
{
    [Fact]
    public void VisaBesidePassport_IsLandscapeA4()
    {
        using var output = new MemoryStream();
        var layout = DocumentCopyPrintLayout.DefaultFor("Passport:abc/Visa:def");

        Assert.True(SupportingDocumentsPdfSharpHelper.TryWriteVisaBesidePassport(
            TinyBmp(40, 60),
            TinyBmp(50, 80),
            output,
            NullLogger.Instance,
            layout));

        Assert.True(SupportingDocumentsPdfSharpHelper.TryReadPdfPageSize(
            output.ToArray(),
            out double width,
            out double height,
            out int pages));
        Assert.Equal(1, pages);
        Assert.Equal(DocumentCopyPrintLayout.A4HeightPt, width, 1);
        Assert.Equal(DocumentCopyPrintLayout.A4WidthPt, height, 1);
    }

    [Fact]
    public void VisaBesidePassport_Portrait_UsesPortraitA4()
    {
        using var output = new MemoryStream();
        var layout = DocumentCopyPrintLayout.DefaultFor("Passport:abc/Visa:def")
            .WithOrientation(DocumentCopyPageOrientation.Portrait);

        Assert.True(SupportingDocumentsPdfSharpHelper.TryWriteVisaBesidePassport(
            TinyBmp(40, 60),
            TinyBmp(50, 80),
            output,
            NullLogger.Instance,
            layout));

        Assert.True(SupportingDocumentsPdfSharpHelper.TryReadPdfPageSize(
            output.ToArray(),
            out double width,
            out double height,
            out int pages));
        Assert.Equal(1, pages);
        Assert.Equal(DocumentCopyPrintLayout.A4WidthPt, width, 1);
        Assert.Equal(DocumentCopyPrintLayout.A4HeightPt, height, 1);
    }

    [Fact]
    public void LatestPassportScan_UsesTheLastContentFile_OnTheOwningPassport()
    {
        var older = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var newer = Guid.Parse("22222222-2222-2222-2222-222222222222");
        const string passportKey = "Passport:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        var snapshot = new PersonLinkedDocumentsSnapshot
        {
            Sections = new[]
            {
                new PersonLinkedDocumentSection
                {
                    Records = new[]
                    {
                        new PersonLinkedDocumentRecord
                        {
                            RecordKey = passportKey,
                            Files = new[]
                            {
                                new PersonLinkedDocumentFile
                                {
                                    FileDataId = older,
                                    HasContent = true,
                                    FileName = "old.jpg"
                                },
                                new PersonLinkedDocumentFile
                                {
                                    FileDataId = Guid.Empty,
                                    HasContent = false,
                                    FileName = "empty.jpg"
                                },
                                new PersonLinkedDocumentFile
                                {
                                    FileDataId = newer,
                                    HasContent = true,
                                    FileName = "new.jpg"
                                }
                            }
                        }
                    }
                }
            }
        };

        var latest = PersonDocumentCopyPdfMerger.LatestPassportScan(
            snapshot,
            passportKey + "/Visa:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb");
        Assert.NotNull(latest);
        Assert.Equal(newer, latest!.FileDataId);
        Assert.Null(PersonDocumentCopyPdfMerger.LatestPassportScan(snapshot, "Visa.only"));
    }

    [Fact]
    public void VisaIdForFile_UsesTheVisaGroupThatContainsTheScan()
    {
        var visaId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var fileId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var snapshot = new ApplicationItemLinkedDocumentsSnapshot
        {
            Groups = new[]
            {
                new ApplicationItemLinkedDocumentGroup
                {
                    SlotKey = "Passport." + Guid.NewGuid().ToString("N"),
                    SourceObjectType = typeof(Passport),
                    SourceObjectId = Guid.NewGuid(),
                    Files = new[]
                    {
                        new ApplicationItemLinkedDocumentFile
                        {
                            FileDataId = Guid.NewGuid(),
                            HasContent = true
                        }
                    }
                },
                new ApplicationItemLinkedDocumentGroup
                {
                    SlotKey = "Visa." + visaId.ToString("N"),
                    SourceObjectType = typeof(Visa),
                    SourceObjectId = visaId,
                    Files = new[]
                    {
                        new ApplicationItemLinkedDocumentFile
                        {
                            FileDataId = fileId,
                            HasContent = true
                        }
                    }
                }
            }
        };

        Assert.Equal(
            visaId,
            ApplicationItemDocumentCopyPdfMerger.VisaIdForFile(snapshot, fileId, "Visa."));
        Assert.Null(
            ApplicationItemDocumentCopyPdfMerger.VisaIdForFile(snapshot, Guid.NewGuid(), "Visa."));
        Assert.Equal(
            visaId,
            ApplicationItemDocumentCopyPdfMerger.VisaIdForFile(null, Guid.Empty, "Visa." + visaId.ToString("N")));
    }

    private static byte[] TinyBmp(int width, int height)
    {
        int rowStride = ((width * 3 + 3) / 4) * 4;
        int pixelBytes = rowStride * height;
        int fileSize = 54 + pixelBytes;
        var bytes = new byte[fileSize];
        bytes[0] = (byte)'B';
        bytes[1] = (byte)'M';
        BitConverter.GetBytes(fileSize).CopyTo(bytes, 2);
        BitConverter.GetBytes(54).CopyTo(bytes, 10);
        BitConverter.GetBytes(40).CopyTo(bytes, 14);
        BitConverter.GetBytes(width).CopyTo(bytes, 18);
        BitConverter.GetBytes(height).CopyTo(bytes, 22);
        bytes[26] = 1;
        bytes[28] = 24;
        BitConverter.GetBytes(pixelBytes).CopyTo(bytes, 34);
        return bytes;
    }
}