using System;
using System.Collections.Generic;
using System.Linq;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;
using Visa2026.Module.Services.ApplicationItemLinkedDocuments;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationItemDocumentCopiesPersonCatalogTests
{
    [Fact]
    public void FilterRosterIds_NullSelection_ReturnsAllRoster()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();

        var filtered = ApplicationItemDocumentCopiesPersonCatalog.FilterRosterIds([a, b], null);

        Assert.Equal(new[] { a, b }, filtered);
    }

    [Fact]
    public void FilterRosterIds_EmptySelection_ReturnsNone()
    {
        var a = Guid.NewGuid();

        var filtered = ApplicationItemDocumentCopiesPersonCatalog.FilterRosterIds(
            [a],
            Array.Empty<Guid>());

        Assert.Empty(filtered);
    }

    [Fact]
    public void FilterRosterIds_Subset_KeepsRosterOrder()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var c = Guid.NewGuid();

        var filtered = ApplicationItemDocumentCopiesPersonCatalog.FilterRosterIds(
            [a, b, c],
            [c, a]);

        Assert.Equal(new[] { a, c }, filtered);
    }

    [Fact]
    public void CountReadySlots_CountsFilesWithContent()
    {
        var line = new ApplicationItemLinkedDocumentsLineSnapshot
        {
            ApplicationItemId = Guid.NewGuid(),
            LineLabel = "Andy",
            Groups =
            [
                new ApplicationItemLinkedDocumentGroup
                {
                    SlotKey = "Passport.Current",
                    Files =
                    [
                        new ApplicationItemLinkedDocumentFile { HasContent = true, FileDataId = Guid.NewGuid() }
                    ]
                },
                new ApplicationItemLinkedDocumentGroup
                {
                    SlotKey = "Visa.Current",
                    LinkMissing = true
                },
                new ApplicationItemLinkedDocumentGroup
                {
                    SlotKey = "Education",
                    Files = Array.Empty<ApplicationItemLinkedDocumentFile>()
                }
            ]
        };

        var (ready, total) = ApplicationItemDocumentCopiesPersonCatalog.CountReadySlots(line);

        Assert.Equal(3, total);
        Assert.Equal(1, ready);
    }

    [Fact]
    public void PersonSectionId_UsesStablePrefix()
    {
        var id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        Assert.Equal("person:aaaaaaaabbbbccccddddeeeeeeeeeeee", ApplicationItemDocumentCopiesPersonCatalog.PersonSectionId(id));
    }

    [Fact]
    public void DisplayPersonName_DropsStandaloneHyphenTokens()
    {
        Assert.Equal("Andy Pramasta", ApplicationItemDocumentCopiesPersonCatalog.DisplayPersonName("Andy Pramasta -"));
        Assert.Equal("Jean-Luc Picard", ApplicationItemDocumentCopiesPersonCatalog.DisplayPersonName("Jean-Luc Picard"));
        Assert.Equal("—", ApplicationItemDocumentCopiesPersonCatalog.DisplayPersonName(" - "));
    }
}

public class ApplicationItemLinkedDocumentsLinkedRecordResolverTests
{
    [Fact]
    public void SlotKey_UsesFamilyAndSourceId()
    {
        var id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        Assert.Equal(
            "Passport.aaaaaaaabbbbccccddddeeeeeeeeeeee",
            ApplicationItemLinkedDocumentsLinkedRecordResolver.SlotKey("Passport", id));
    }

    [Fact]
    public void PackageRules_IncludeLinkedRecordKeys()
    {
        var options = ApplicationItemDocumentPackageOptions.CreateDefaults();
        var id = Guid.NewGuid();

        Assert.True(ApplicationItemDocumentCopiesPackageSlotRules.IsSlotIncludedInPackage(
            ApplicationItemLinkedDocumentsLinkedRecordResolver.SlotKey("Passport", id),
            options));
        Assert.True(ApplicationItemDocumentCopiesPackageSlotRules.IsSlotIncludedInPackage(
            ApplicationItemLinkedDocumentsLinkedRecordResolver.SlotKey("Visa", id),
            options));
        Assert.True(ApplicationItemDocumentCopiesPackageSlotRules.IsSlotIncludedInPackage(
            ApplicationItemLinkedDocumentsLinkedRecordResolver.SlotKey("Education", id),
            options));
        Assert.False(ApplicationItemDocumentCopiesPackageSlotRules.IsSlotIncludedInPackage(
            "Passport.Previous",
            new ApplicationItemDocumentPackageOptions { IncludePassportCopies = false }));
    }

    [Fact]
    public void KindOrder_ListsPassportBeforeVisa()
    {
        Assert.True(
            ApplicationItemLinkedDocumentsLinkedRecordResolver.KindOrder(ApplicationProfileInstancePersonLinkKind.Passport)
            < ApplicationItemLinkedDocumentsLinkedRecordResolver.KindOrder(ApplicationProfileInstancePersonLinkKind.Visa));
    }
}

public class ApplicationItemDocumentCopiesTypeCatalogTests
{
    [Fact]
    public void FamilyKey_UsesPrefixAndBareFamily()
    {
        var id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        Assert.Equal("Passport", ApplicationItemDocumentCopiesTypeCatalog.FamilyKey($"Passport.{id:N}"));
        Assert.Equal("Visa", ApplicationItemDocumentCopiesTypeCatalog.FamilyKey("Visa.Current"));
        Assert.Equal("Education", ApplicationItemDocumentCopiesTypeCatalog.FamilyKey("Education"));
        Assert.Equal(
            "ApplicationForm",
            ApplicationItemDocumentCopiesTypeCatalog.FamilyKey("ApplicationForm"));
        Assert.Equal(
            "AddressOfResidence",
            ApplicationItemDocumentCopiesTypeCatalog.FamilyKey("AddressOfResidence.Lodging.abc"));
        Assert.Null(ApplicationItemDocumentCopiesTypeCatalog.FamilyKey("Unknown.abc"));
        Assert.Null(ApplicationItemDocumentCopiesTypeCatalog.FamilyKey(null));
    }

    [Fact]
    public void TryParseFamilyPreviewKey_RoundTrips()
    {
        var key = ApplicationItemDocumentCopiesTypeCatalog.FamilyPreviewKey("Passport");

        Assert.Equal("Family:Passport", key);
        Assert.True(ApplicationItemDocumentCopiesTypeCatalog.TryParseFamilyPreviewKey(key, out var family));
        Assert.Equal("Passport", family);
        Assert.False(ApplicationItemDocumentCopiesTypeCatalog.TryParseFamilyPreviewKey("Passport.abc", out _));
    }

    [Fact]
    public void Build_GroupsByFamily_OrdersPassportBeforeVisa()
    {
        var andy = Guid.NewGuid();
        var karan = Guid.NewGuid();
        var passportId = Guid.NewGuid();
        var visaId = Guid.NewGuid();
        var educationId = Guid.NewGuid();

        var sections = ApplicationItemDocumentCopiesTypeCatalog.Build(
        [
            Line(
                andy,
                "Andy Pramasta -",
                Group($"Passport.{passportId:N}", "K1450236", hasFile: true),
                Group($"Visa.{visaId:N}", "A7883333", hasFile: true),
                Group($"Education.{educationId:N}", "diploma", hasFile: false)),
            Line(
                karan,
                "Karan",
                Group($"Passport.{Guid.NewGuid():N}", "Passport X111", hasFile: true),
                Group($"Visa.{Guid.NewGuid():N}", "Visa B222", hasFile: false)),
        ]);

        Assert.Equal(new[] { "Passport", "Education", "Visa", "ApplicationForm" }, sections.Select(s => s.FamilyKey));
        Assert.Equal("Passport", sections[0].Title);
        Assert.Equal(2, sections[0].ReadyCount);
        Assert.Equal(2, sections[0].TotalCount);
        Assert.Equal(0, sections[1].ReadyCount);
        Assert.Equal(1, sections[1].TotalCount);
        Assert.Equal(1, sections[2].ReadyCount);
        Assert.Equal(2, sections[2].TotalCount);
        Assert.Equal("Andy Pramasta", sections[0].Rows[0].PersonName);
        Assert.Equal("K1450236", sections[0].Rows[0].RecordLabel);
        Assert.Equal("ApplicationForm", sections[^1].FamilyKey);
        Assert.Equal(2, sections[^1].ReadyCount);
        Assert.Equal(2, sections[^1].TotalCount);
        Assert.All(sections[^1].Rows, row => Assert.True(row.IsReady));
    }

    [Fact]
    public void IsApplicationFormPreview_RecognizesSlotAndFamilyKeys()
    {
        Assert.True(ApplicationItemDocumentCopiesTypeCatalog.IsApplicationFormPreview("ApplicationForm"));
        Assert.True(ApplicationItemDocumentCopiesTypeCatalog.IsApplicationFormPreview("Family:ApplicationForm"));
        Assert.True(ApplicationItemDocumentCopiesTypeCatalog.IsApplicationFormPreview(
            "Family:ApplicationForm",
            "ApplicationForm"));
        Assert.False(ApplicationItemDocumentCopiesTypeCatalog.IsApplicationFormPreview("Family:Passport"));
        Assert.False(ApplicationItemDocumentCopiesTypeCatalog.IsApplicationFormPreview("Passport.abc"));
    }

    [Fact]
    public void CountReadySlotsIncludingApplicationForm_AddsGeneratedForm()
    {
        var line = new ApplicationItemLinkedDocumentsLineSnapshot
        {
            ApplicationItemId = Guid.NewGuid(),
            LineLabel = "Gabriel",
            Groups =
            [
                new ApplicationItemLinkedDocumentGroup
                {
                    SlotKey = "Passport.Current",
                    Files =
                    [
                        new ApplicationItemLinkedDocumentFile { HasContent = true, FileDataId = Guid.NewGuid() }
                    ]
                }
            ]
        };

        var (ready, total) = ApplicationItemDocumentCopiesPersonCatalog.CountReadySlotsIncludingApplicationForm(line);

        Assert.Equal(2, ready);
        Assert.Equal(2, total);
    }

    [Fact]
    public void CollectFamilyFiles_OnlyReadyMatchingFamily()
    {
        var andy = Guid.NewGuid();
        var karan = Guid.NewGuid();
        var passportFile = Guid.NewGuid();

        var files = ApplicationItemDocumentCopiesTypeCatalog.CollectFamilyFiles(
        [
            Line(
                andy,
                "Andy",
                Group($"Passport.{Guid.NewGuid():N}", "Passport A", hasFile: true, fileId: passportFile),
                Group($"Visa.{Guid.NewGuid():N}", "Visa A", hasFile: true)),
            Line(
                karan,
                "Karan",
                Group($"Passport.{Guid.NewGuid():N}", "Passport B", hasFile: false)),
        ],
        "Passport");

        Assert.Single(files);
        Assert.Equal(andy, files[0].ApplicationItemId);
        Assert.Equal(passportFile, files[0].File.FileDataId);
    }

    private static ApplicationItemLinkedDocumentsLineSnapshot Line(
        Guid personId,
        string label,
        params ApplicationItemLinkedDocumentGroup[] groups) =>
        new()
        {
            ApplicationItemId = personId,
            LineLabel = label,
            Groups = groups,
        };

    private static ApplicationItemLinkedDocumentGroup Group(
        string slotKey,
        string caption,
        bool hasFile,
        Guid? fileId = null) =>
        new()
        {
            SlotKey = slotKey,
            SlotLabel = caption,
            SourceCaption = caption,
            Files = hasFile
                ?
                [
                    new ApplicationItemLinkedDocumentFile
                    {
                        HasContent = true,
                        FileDataId = fileId ?? Guid.NewGuid(),
                    }
                ]
                : Array.Empty<ApplicationItemLinkedDocumentFile>(),
        };
}