using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Base;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.DatabaseUpdate.LookupCatalogs;

/// <summary>Nested <c>ApplicationTypeNames</c> on tenant <c>application-migration-sla-profile.json</c>.</summary>
internal sealed class ApplicationMigrationSlaProfileCatalogFile
{
    public List<ApplicationMigrationSlaProfileCatalogRow> Rows { get; set; } = new();
}

internal sealed class ApplicationMigrationSlaProfileCatalogRow
{
    public string Code { get; set; } = string.Empty;

    public string NameTm { get; set; } = string.Empty;

    public int? MaxDaysInReview { get; set; }

    public int? WarningDaysBeforeMax { get; set; }

    public List<string> ApplicationTypeNames { get; set; } = new();
}

internal static class ApplicationMigrationSlaProfileTypeLinkCatalogLoader
{
    private const string FileName = "application-migration-sla-profile.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static ApplicationMigrationSlaProfileCatalogFile? Load()
    {
        var json = LookupCatalogResourceLoader.TryReadTenantOverlayText(FileName)
            ?? LookupCatalogResourceLoader.TryReadEmbeddedLookupCatalogText("tenant/" + FileName);
        if (string.IsNullOrWhiteSpace(json))
            return null;

        return JsonSerializer.Deserialize<ApplicationMigrationSlaProfileCatalogFile>(json, JsonOptions);
    }
}

internal static class ApplicationMigrationSlaProfileTypeLinkCatalogSync
{
    public static int Sync(IObjectSpace objectSpace)
    {
        var catalog = ApplicationMigrationSlaProfileTypeLinkCatalogLoader.Load();
        if (catalog?.Rows is not { Count: > 0 })
        {
            Tracing.Tracer.LogText(
                "ApplicationMigrationSlaProfileTypeLinkCatalogSync: no rows in tenant/application-migration-sla-profile.json.");
            return 0;
        }

        var profiles = objectSpace.GetObjectsQuery<ApplicationMigrationSlaProfile>().ToList();
        var profileIndex = ApplicationMigrationSlaProfileTypeLinkResolver.BuildProfileIndex(profiles);
        var applicationTypes = objectSpace.GetObjectsQuery<ApplicationType>().ToList();
        var linked = 0;
        var skippedMissingProfile = 0;
        var skippedMissingType = 0;

        foreach (var row in catalog.Rows)
        {
            if (row.ApplicationTypeNames is not { Count: > 0 })
                continue;

            var profile = ApplicationMigrationSlaProfileTypeLinkResolver.TryResolveProfile(profileIndex, row.Code)
                ?? profiles.FirstOrDefault(p =>
                    LookupCatalogMatchHelper.KeysEqual(p.NameTm, row.NameTm));
            if (profile == null)
            {
                skippedMissingProfile++;
                Tracing.Tracer.LogText(
                    $"ApplicationMigrationSlaProfileTypeLinkCatalogSync: profile '{row.Code}' not found — type links skipped.");
                continue;
            }

            foreach (var typeName in row.ApplicationTypeNames.Where(n => !string.IsNullOrWhiteSpace(n)))
            {
                var applicationType = ApplicationMigrationSlaProfileTypeLinkResolver.TryResolveApplicationType(
                    applicationTypes,
                    typeName.Trim());
                if (applicationType == null)
                {
                    skippedMissingType++;
                    Tracing.Tracer.LogText(
                        $"ApplicationMigrationSlaProfileTypeLinkCatalogSync: application type '{typeName}' not found for profile '{row.Code}'.");
                    continue;
                }

                if (ReferenceEquals(applicationType.MigrationSlaProfile, profile))
                    continue;

                applicationType.MigrationSlaProfile = profile;
                linked++;
            }
        }

        Tracing.Tracer.LogText(
            "ApplicationMigrationSlaProfileTypeLinkCatalogSync: "
            + $"linked={linked}, missingProfile={skippedMissingProfile}, missingType={skippedMissingType}.");

        return linked;
    }
}
