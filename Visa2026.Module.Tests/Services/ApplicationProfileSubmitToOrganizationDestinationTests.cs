using System;
using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Destination encode/parse for RequireSubmitToOrganization profiles (Seretmezlik / org hand-in).
/// Wrong prefixes or Guid shapes would bind the wrong ministry vs migration service.
/// </summary>
public class ApplicationProfileSubmitToOrganizationDestinationTests
{
    [Fact]
    public void IsRequired_ReadsProfileFlag()
    {
        Assert.False(ApplicationProfileSubmitToOrganizationHelper.IsRequired((ApplicationProfile?)null));
        Assert.False(ApplicationProfileSubmitToOrganizationHelper.IsRequired(new ApplicationProfile()));
        Assert.True(ApplicationProfileSubmitToOrganizationHelper.IsRequired(
            new ApplicationProfile { RequireSubmitToOrganization = true }));

        Assert.False(ApplicationProfileSubmitToOrganizationHelper.IsRequired((ApplicationProfileInstance?)null));
        Assert.True(ApplicationProfileSubmitToOrganizationHelper.IsRequired(
            new ApplicationProfileInstance
            {
                ApplicationProfile = new ApplicationProfile { RequireSubmitToOrganization = true },
            }));
    }

    [Fact]
    public void FormatDestinationLabel_PrefersMinistryShortNameThenNameThenMigration()
    {
        Assert.Null(ApplicationProfileSubmitToOrganizationHelper.FormatDestinationLabel(null));

        var ministryShort = new ApplicationProfileInstance
        {
            SubmitToApprovingMinistry = new ApprovingMinistry
            {
                ShortNameTm = " TNGIZ ",
                NameTm = "Long",
            },
        };
        Assert.Equal("TNGIZ", ApplicationProfileSubmitToOrganizationHelper.FormatDestinationLabel(ministryShort));

        var ministryNameOnly = new ApplicationProfileInstance
        {
            SubmitToApprovingMinistry = new ApprovingMinistry { ShortNameTm = "  ", NameTm = " Ministry " },
        };
        Assert.Equal("Ministry", ApplicationProfileSubmitToOrganizationHelper.FormatDestinationLabel(ministryNameOnly));

        var migration = new ApplicationProfileInstance
        {
            SubmitToMigrationService = new MigrationService { NameTm = " Migration A " },
        };
        Assert.Equal("Migration A", ApplicationProfileSubmitToOrganizationHelper.FormatDestinationLabel(migration));
    }

    [Fact]
    public void HasDestination_TrueOnlyWhenLabelPresent()
    {
        Assert.False(ApplicationProfileSubmitToOrganizationHelper.HasDestination(null));
        Assert.False(ApplicationProfileSubmitToOrganizationHelper.HasDestination(new ApplicationProfileInstance()));
        Assert.True(ApplicationProfileSubmitToOrganizationHelper.HasDestination(
            new ApplicationProfileInstance
            {
                SubmitToMigrationService = new MigrationService { NameTm = "MS" },
            }));
    }

    [Fact]
    public void EncodeValue_UsesMinistryAndMigrationPrefixes()
    {
        var ministryId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var migrationId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        Assert.Equal(string.Empty, ApplicationProfileSubmitToOrganizationHelper.EncodeValue(null));
        Assert.Equal(string.Empty, ApplicationProfileSubmitToOrganizationHelper.EncodeValue(new ApplicationProfileInstance()));

        Assert.Equal(
            ApplicationProfileSubmitToOrganizationHelper.MinistryValuePrefix + ministryId.ToString("D"),
            ApplicationProfileSubmitToOrganizationHelper.EncodeValue(
                new ApplicationProfileInstance { SubmitToApprovingMinistryId = ministryId }));

        Assert.Equal(
            ApplicationProfileSubmitToOrganizationHelper.MigrationValuePrefix + migrationId.ToString("D"),
            ApplicationProfileSubmitToOrganizationHelper.EncodeValue(
                new ApplicationProfileInstance { SubmitToMigrationServiceId = migrationId }));

        // Navigation Id wins when FK fields are empty.
        Assert.Equal(
            ApplicationProfileSubmitToOrganizationHelper.MinistryValuePrefix + ministryId.ToString("D"),
            ApplicationProfileSubmitToOrganizationHelper.EncodeValue(
                new ApplicationProfileInstance
                {
                    SubmitToApprovingMinistry = new ApprovingMinistry { ID = ministryId },
                }));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ministry:")]
    [InlineData("migration:not-a-guid")]
    [InlineData("other:aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")]
    public void TryParseValue_RejectsBlankAndMalformed(string? value)
    {
        Assert.False(ApplicationProfileSubmitToOrganizationHelper.TryParseValue(value, out _, out _));
    }

    [Fact]
    public void TryParseValue_AcceptsPrefixedAndLegacyPlainGuid()
    {
        var ministryId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var migrationId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var plainId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        Assert.True(ApplicationProfileSubmitToOrganizationHelper.TryParseValue(
            "ministry:" + ministryId.ToString("D"), out var ministryKind, out var parsedMinistry));
        Assert.Equal("ministry", ministryKind);
        Assert.Equal(ministryId, parsedMinistry);

        Assert.True(ApplicationProfileSubmitToOrganizationHelper.TryParseValue(
            "Migration:" + migrationId.ToString("D"), out var migrationKind, out var parsedMigration));
        Assert.Equal("migration", migrationKind);
        Assert.Equal(migrationId, parsedMigration);

        Assert.True(ApplicationProfileSubmitToOrganizationHelper.TryParseValue(
            plainId.ToString("D"), out var unknownKind, out var parsedPlain));
        Assert.Equal("unknown", unknownKind);
        Assert.Equal(plainId, parsedPlain);
    }

    [Fact]
    public void TryApply_NullApplicationOrObjectSpace_FailsReadyGate()
    {
        Assert.False(ApplicationProfileSubmitToOrganizationHelper.TryApply(
            null!, null!, "ministry:" + Guid.NewGuid(), out var error));
        Assert.Equal("Application is not ready.", error);

        Assert.False(ApplicationProfileSubmitToOrganizationHelper.TryApply(
            null!, new ApplicationProfileInstance(), "ministry:" + Guid.NewGuid(), out error));
        Assert.Equal("Application is not ready.", error);

        // ObjectSpace is checked before blank/malformed value handling.
        Assert.False(ApplicationProfileSubmitToOrganizationHelper.TryApply(
            null!, new ApplicationProfileInstance(), "  ", out error));
        Assert.Equal("Application is not ready.", error);
    }
}
