using System;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.HeaderLinkedDocuments;
using Visa2026.Module.Services.PreviewSlot;
using Visa2026.Module.Services.WordReports;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public sealed class VisaPreviewSlotOccupantKeysTests
{
    private static readonly Guid AppId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid ItemA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ItemB = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid PersonId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public void ForResminamalar_EmptyApplication_ReturnsEmptySentinel()
    {
        var key = VisaPreviewSlotOccupantKeys.ForResminamalar(new ResminamalarSlotRequest
        {
            ApplicationId = Guid.Empty,
            Scope = WordReportPackageScope.Application,
        });

        Assert.Equal("resminamalar:empty", key);
    }

    [Fact]
    public void ForResminamalar_ApplicationScope_UsesAppPrefix()
    {
        var key = VisaPreviewSlotOccupantKeys.ForResminamalar(new ResminamalarSlotRequest
        {
            ApplicationId = AppId,
            Scope = WordReportPackageScope.Application,
            ApplicationItemIds = [ItemA],
        });

        Assert.Equal($"resminamalar:app:{AppId:N}", key);
    }

    [Fact]
    public void ForResminamalar_ItemScope_DropsEmptyGuids_AndOrders_PreservingDuplicates()
    {
        var key = VisaPreviewSlotOccupantKeys.ForResminamalar(new ResminamalarSlotRequest
        {
            ApplicationId = AppId,
            Scope = WordReportPackageScope.ApplicationItem,
            ApplicationItemIds = [ItemB, Guid.Empty, ItemA, ItemB],
        });

        // Empty Guids dropped; remaining ids sorted ascending; duplicates kept (occupant key identity).
        Assert.Equal(
            $"resminamalar:items:{AppId:N}:{ItemA:N},{ItemB:N},{ItemB:N}",
            key);
    }

    [Fact]
    public void ForDocumentCopies_Empty_ReturnsSentinel_AndOrdersIds()
    {
        Assert.Equal("document-copies:empty", VisaPreviewSlotOccupantKeys.ForDocumentCopies(Array.Empty<Guid>()));
        Assert.Equal("document-copies:empty", VisaPreviewSlotOccupantKeys.ForDocumentCopies([Guid.Empty]));

        var key = VisaPreviewSlotOccupantKeys.ForDocumentCopies(new DocumentCopiesSlotRequest
        {
            ApplicationItemIds = [ItemB, ItemA],
        });
        Assert.Equal($"document-copies:items:{ItemA:N},{ItemB:N}", key);
    }

    [Fact]
    public void ForProgressLetters_EmptyVsApp()
    {
        Assert.Equal("progress-letters:empty", VisaPreviewSlotOccupantKeys.ForProgressLetters(Guid.Empty));
        Assert.Equal(
            $"progress-letters:app:{AppId:N}",
            VisaPreviewSlotOccupantKeys.ForProgressLetters(new ProgressLettersSlotRequest { ApplicationId = AppId }));
    }

    [Fact]
    public void ForPersonDocumentCopies_SingleVsMulti()
    {
        Assert.Equal(
            "person-document-copies:empty",
            VisaPreviewSlotOccupantKeys.ForPersonDocumentCopies(Array.Empty<Guid>()));

        Assert.Equal(
            $"person-document-copies:person:{PersonId:N}",
            VisaPreviewSlotOccupantKeys.ForPersonDocumentCopies([PersonId]));

        var other = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        Assert.Equal(
            $"person-document-copies:persons:{PersonId:N},{other:N}",
            VisaPreviewSlotOccupantKeys.ForPersonDocumentCopies([other, PersonId]));
    }

    [Fact]
    public void ForHeaderDocumentCopies_FamilyPrefixes()
    {
        Assert.Equal(
            "header-document-copies:empty",
            VisaPreviewSlotOccupantKeys.ForHeaderDocumentCopies(null!));

        Assert.Equal(
            "header-document-copies:empty",
            VisaPreviewSlotOccupantKeys.ForHeaderDocumentCopies(new HeaderDocumentCopiesSlotRequest
            {
                Family = HeaderDocumentCopiesFamily.Invitation,
                ParentId = Guid.Empty,
            }));

        Assert.Equal(
            $"work-permit-document-copies:work-permit:{AppId:N}",
            VisaPreviewSlotOccupantKeys.ForHeaderDocumentCopies(new HeaderDocumentCopiesSlotRequest
            {
                Family = HeaderDocumentCopiesFamily.WorkPermit,
                ParentId = AppId,
            }));

        Assert.Equal(
            $"invitation-document-copies:invitation:{AppId:N}",
            VisaPreviewSlotOccupantKeys.ForHeaderDocumentCopies(new HeaderDocumentCopiesSlotRequest
            {
                Family = HeaderDocumentCopiesFamily.Invitation,
                ParentId = AppId,
            }));

        Assert.Equal(
            $"rejection-document-copies:rejection:{AppId:N}",
            VisaPreviewSlotOccupantKeys.ForHeaderDocumentCopies(new HeaderDocumentCopiesSlotRequest
            {
                Family = HeaderDocumentCopiesFamily.Rejection,
                ParentId = AppId,
            }));

        Assert.Equal(
            $"border-zone-document-copies:border-zone:{AppId:N}",
            VisaPreviewSlotOccupantKeys.ForHeaderDocumentCopies(new HeaderDocumentCopiesSlotRequest
            {
                Family = HeaderDocumentCopiesFamily.BorderZone,
                ParentId = AppId,
            }));
    }

    [Fact]
    public void ForFile_And_ForPlaceholderManual()
    {
        Assert.Equal($"file:Passport:{ItemA:N}", VisaPreviewSlotOccupantKeys.ForFile(" Passport ", ItemA));
        Assert.Equal("placeholder-manual:all", VisaPreviewSlotOccupantKeys.ForPlaceholderManual(null));
        Assert.Equal(
            $"placeholder-manual:root:{UserReportBoType.Application}",
            VisaPreviewSlotOccupantKeys.ForPlaceholderManual(UserReportBoType.Application));
    }
}
