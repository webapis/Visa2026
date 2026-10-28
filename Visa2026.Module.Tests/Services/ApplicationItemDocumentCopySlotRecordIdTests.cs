using System;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationItemLinkedDocuments;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationItemDocumentCopySlotRecordIdTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Visa.")]
    [InlineData("Passport.aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    [InlineData("Visa.not-a-guid")]
    public void TryParseSlotRecordId_RejectsBlankBadPrefixOrInvalidGuid(string? slotKey)
    {
        Assert.False(ApplicationItemDocumentCopyPdfMerger.TryParseSlotRecordId(slotKey!, "Visa.", out var id));
        Assert.Equal(Guid.Empty, id);
    }

    [Fact]
    public void TryParseSlotRecordId_AcceptsNFormatAndDashedGuid()
    {
        var id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        Assert.True(ApplicationItemDocumentCopyPdfMerger.TryParseSlotRecordId(
            "Visa." + id.ToString("N"),
            "Visa.",
            out var parsedN));
        Assert.Equal(id, parsedN);

        Assert.True(ApplicationItemDocumentCopyPdfMerger.TryParseSlotRecordId(
            "visa." + id,
            "Visa.",
            out var parsedDashed));
        Assert.Equal(id, parsedDashed);
    }

    [Fact]
    public void VisaIdForFile_FallsBackToSlotKey_WhenSnapshotHasNoMatchingVisaGroup()
    {
        var visaId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
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
                            HasContent = true,
                        },
                    },
                },
            },
        };

        Assert.Equal(
            visaId,
            ApplicationItemDocumentCopyPdfMerger.VisaIdForFile(
                snapshot,
                Guid.NewGuid(),
                "Visa." + visaId.ToString("N")));
    }
}
