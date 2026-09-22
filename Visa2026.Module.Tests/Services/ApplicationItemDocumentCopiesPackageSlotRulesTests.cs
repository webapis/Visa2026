using System;
using System.Collections.Generic;
using Visa2026.Module.Services;
using Visa2026.Module.Services.ApplicationItemLinkedDocuments;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public sealed class ApplicationItemDocumentCopiesPackageSlotRulesTests
{
    [Theory]
    [InlineData("Passport.Current", true, false, false, false, false, false, false, false)]
    [InlineData("Visa.Current", false, true, false, false, false, false, false, false)]
    [InlineData("WorkPermit.Current", false, false, true, false, false, false, false, false)]
    [InlineData("Education.Current", false, false, false, true, false, false, false, false)]
    [InlineData("AddressOfResidence.Current", false, false, false, false, true, false, false, false)]
    [InlineData("MedicalRecord.Current", false, false, false, false, false, true, false, false)]
    [InlineData("Invitation.Current", false, false, false, false, false, false, true, false)]
    [InlineData("FamilyRelationship.Current", false, false, false, false, false, false, false, true)]
    public void IsSlotIncludedInPackage_HonorsMatchingIncludeFlags(
        string slotKey,
        bool passport,
        bool visa,
        bool workPermit,
        bool diploma,
        bool address,
        bool medical,
        bool invitation,
        bool family)
    {
        var options = new ApplicationItemDocumentPackageOptions
        {
            IncludePassportCopies = passport,
            IncludeVisaCopies = visa,
            IncludeWorkPermitCopies = workPermit,
            IncludeDiplomaFiles = diploma,
            IncludeAddressOfResidenceCopies = address,
            IncludeMedicalRecordCopies = medical,
            IncludeInvitationCopies = invitation,
            IncludeFamilyRelationshipCopies = family
        };

        Assert.True(ApplicationItemDocumentCopiesPackageSlotRules.IsSlotIncludedInPackage(slotKey, options));
    }

    [Theory]
    [InlineData("Passport.Current")]
    [InlineData("Visa.Current")]
    [InlineData("Education.Current")]
    [InlineData("Unknown.Slot")]
    [InlineData("")]
    [InlineData("   ")]
    public void IsSlotIncludedInPackage_WhenFlagsOffOrUnknown_ReturnsFalse(string slotKey)
    {
        var options = new ApplicationItemDocumentPackageOptions
        {
            IncludePassportCopies = false,
            IncludeVisaCopies = false,
            IncludeWorkPermitCopies = false,
            IncludeDiplomaFiles = false,
            IncludeAddressOfResidenceCopies = false,
            IncludeMedicalRecordCopies = false,
            IncludeInvitationCopies = false,
            IncludeFamilyRelationshipCopies = false
        };

        Assert.False(ApplicationItemDocumentCopiesPackageSlotRules.IsSlotIncludedInPackage(slotKey, options));
    }

    [Fact]
    public void IsSlotIncludedInPackage_EducationPrefixOnlyExactCurrent_IsIncluded()
    {
        var options = ApplicationItemDocumentPackageOptions.CreateDefaults();

        Assert.True(ApplicationItemDocumentCopiesPackageSlotRules.IsSlotIncludedInPackage(
            "Education.Current",
            options));
        Assert.False(ApplicationItemDocumentCopiesPackageSlotRules.IsSlotIncludedInPackage(
            "Education.Other",
            options));
    }

    [Fact]
    public void ReadinessSummary_ExcludedSlotsAreIgnored()
    {
        var options = ApplicationItemDocumentPackageOptions.CreateDefaults();
        options.IncludePassportCopies = false;

        var summary = ApplicationItemDocumentCopiesReadinessSummary.Compute(
            [
                Group("Passport.Current", files: 0, missing: 1),
                Group("Visa.Current", files: 1, missing: 0)
            ],
            options,
            includeApplicationFormSlot: true);

        Assert.Equal(2, summary.ReadySlotCount); // form + visa
        Assert.Equal(0, summary.PartialSlotCount);
        Assert.Equal(0, summary.GapSlotCount);
        Assert.False(summary.HasPackagingGaps);
    }

    [Fact]
    public void ReadinessSummary_ClassifiesReadyPartialAndGap()
    {
        var summary = ApplicationItemDocumentCopiesReadinessSummary.Compute(
            [
                Group("Passport.Current", files: 1, missing: 0),
                Group("Visa.Current", files: 1, missing: 1),
                Group("WorkPermit.Current", files: 0, missing: 1)
            ],
            ApplicationItemDocumentPackageOptions.CreateDefaults(),
            includeApplicationFormSlot: false);

        Assert.Equal(1, summary.ReadySlotCount);
        Assert.Equal(1, summary.PartialSlotCount);
        Assert.Equal(1, summary.GapSlotCount);
        Assert.True(summary.HasPackagingGaps);
    }

    [Fact]
    public void ReadinessSummary_NullGroups_StillCountsApplicationFormWhenEnabled()
    {
        var withForm = ApplicationItemDocumentCopiesReadinessSummary.Compute(
            null,
            includeApplicationFormSlot: true);
        var withoutForm = ApplicationItemDocumentCopiesReadinessSummary.Compute(
            Array.Empty<ApplicationItemLinkedDocumentMergedGroup>(),
            includeApplicationFormSlot: false);

        Assert.Equal(1, withForm.ReadySlotCount);
        Assert.Equal(0, withoutForm.ReadySlotCount);
        Assert.False(withForm.HasPackagingGaps);
    }

    private static ApplicationItemLinkedDocumentMergedGroup Group(string slotKey, int files, int missing)
    {
        var fileEntries = new List<ApplicationItemLinkedDocumentFileEntry>();
        for (var i = 0; i < files; i++)
        {
            fileEntries.Add(new ApplicationItemLinkedDocumentFileEntry
            {
                ApplicationItemId = Guid.NewGuid(),
                LineLabel = $"line-{i}",
                File = new ApplicationItemLinkedDocumentFile
                {
                    FileName = $"f{i}.pdf",
                    HasContent = true,
                    SizeBytes = 10
                }
            });
        }

        var missingEntries = new List<ApplicationItemLinkedDocumentMissingLineEntry>();
        for (var i = 0; i < missing; i++)
        {
            missingEntries.Add(new ApplicationItemLinkedDocumentMissingLineEntry
            {
                ApplicationItemId = Guid.NewGuid(),
                LineLabel = $"missing-{i}",
                LinkMissing = true
            });
        }

        return new ApplicationItemLinkedDocumentMergedGroup
        {
            SlotKey = slotKey,
            SlotLabel = slotKey,
            Files = fileEntries,
            MissingLines = missingEntries
        };
    }
}
