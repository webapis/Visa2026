using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Base;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.DatabaseUpdate.LookupCatalogs;

public sealed class ApplicationProfileApprovalLegVersionTenantCatalogFile
{
    public List<ApplicationProfileApprovalLegVersionTenantCatalogRow> Rows { get; set; } = new();
}

public sealed class ApplicationProfileApprovalLegVersionTenantCatalogRow
{
    public string ApplicationTypeName { get; set; } = string.Empty;

    public string ProfileCode { get; set; } = string.Empty;

    public string? ProfileName { get; set; }

    public string? SignOff { get; set; }

    public List<ApplicationProfileApprovalLegVersionTenantCatalogVersionRow> Versions { get; set; } = new();
}

public sealed class ApplicationProfileApprovalLegVersionTenantCatalogVersionRow
{
    public string Name { get; set; } = string.Empty;

    public string ApprovalLegProfileCode { get; set; } = string.Empty;

    public bool IsDefault { get; set; }

    public int Sequence { get; set; } = 1;

    public int SourceAppCount { get; set; }
}

internal static class ApplicationProfileApprovalLegVersionTenantCatalogLoader
{
    private const string DefaultFileName = "application-profile-approval-leg-versions.json";
    private const string CalikFileName = "application-profile-approval-leg-versions.calik-energi.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true,
    };

    public static ApplicationProfileApprovalLegVersionTenantCatalogFile? Load()
    {
        var json = LookupCatalogResourceLoader.TryReadTenantOverlayText(CalikFileName)
            ?? LookupCatalogResourceLoader.TryReadTenantOverlayText(DefaultFileName)
            ?? LookupCatalogResourceLoader.TryReadEmbeddedLookupCatalogText("tenant/" + CalikFileName)
            ?? LookupCatalogResourceLoader.TryReadEmbeddedLookupCatalogText("tenant/" + DefaultFileName);

        if (string.IsNullOrWhiteSpace(json))
            return null;

        return JsonSerializer.Deserialize<ApplicationProfileApprovalLegVersionTenantCatalogFile>(json, JsonOptions);
    }

    public static bool TryLoadRows(out List<ApplicationProfileApprovalLegVersionTenantCatalogRow> rows)
    {
        rows = new List<ApplicationProfileApprovalLegVersionTenantCatalogRow>();
        var catalog = Load();
        if (catalog?.Rows == null || catalog.Rows.Count == 0)
            return false;

        rows = catalog.Rows
            .Where(r => r != null && IsApproved(r.SignOff))
            .ToList();
        return rows.Count > 0;
    }

    internal static bool IsApproved(string? signOff) =>
        string.Equals(signOff?.Trim(), "approved", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Shared catalog redesign: seed only sets <see cref="ApplicationProfile.DefaultApprovalLegProfile"/>
/// from the frequency matrix Default. Does not copy legs onto the profile (use Configuration ApprovalLegProfile).
/// Clears legacy nested ApprovalLegVersions on synced profiles so the wizard does not show duplicates.
/// </summary>
internal static class ApplicationProfileApprovalLegVersionTenantCatalogSync
{
    public readonly record struct Result(int ProfilesTouched, int DefaultsSet, int NestedCleared, int Skipped);

    public static Result Sync(IObjectSpace objectSpace)
    {
        if (!ApplicationProfileApprovalLegVersionTenantCatalogLoader.TryLoadRows(out var rows))
        {
            Tracing.Tracer.LogText(
                "ApplicationProfileApprovalLegVersionTenantCatalogSync: no approved rows in tenant approval-leg-versions JSON.");
            return default;
        }

        var profiles = objectSpace.GetObjectsQuery<ApplicationProfile>().ToList();
        var sharedProfiles = objectSpace.GetObjectsQuery<ApprovalLegProfile>()
            .Where(p => p.IsActive)
            .ToList();

        int touched = 0, defaultsSet = 0, nestedCleared = 0, skipped = 0;

        foreach (var row in rows)
        {
            var profile = FindProfile(profiles, row);
            if (profile == null)
            {
                skipped++;
                Tracing.Tracer.LogText(
                    $"ApplicationProfileApprovalLegVersionTenantCatalogSync: profile not found for {row.ApplicationTypeName}/{row.ProfileCode}.");
                continue;
            }

            if (profile.ProgressRoute != ApplicationProfileInstanceProgressRouteKind.ViaMinistries)
            {
                skipped++;
                continue;
            }

            touched++;

            var defaultCode = row.Versions?
                .OrderByDescending(v => v.IsDefault)
                .ThenBy(v => v.Sequence <= 0 ? int.MaxValue : v.Sequence)
                .Select(v => v.ApprovalLegProfileCode?.Trim())
                .FirstOrDefault(c => !string.IsNullOrWhiteSpace(c));

            if (!string.IsNullOrWhiteSpace(defaultCode))
            {
                var shared = sharedProfiles.FirstOrDefault(p =>
                    string.Equals(p.Code?.Trim(), defaultCode, StringComparison.OrdinalIgnoreCase));
                if (shared == null)
                {
                    skipped++;
                    Tracing.Tracer.LogText(
                        $"ApplicationProfileApprovalLegVersionTenantCatalogSync: ApprovalLegProfile '{defaultCode}' missing for {row.ProfileCode}.");
                }
                else if (profile.DefaultApprovalLegProfile?.ID != shared.ID)
                {
                    profile.DefaultApprovalLegProfile = shared;
                    defaultsSet++;
                }
            }

            foreach (var version in (profile.ApprovalLegVersions ?? []).ToList())
            {
                objectSpace.Delete(version);
                nestedCleared++;
            }

            foreach (var leg in (profile.ApprovalLegs ?? []).Where(l => l.ApprovalLegVersion != null).ToList())
                objectSpace.Delete(leg);
        }

        Tracing.Tracer.LogText(
            $"ApplicationProfileApprovalLegVersionTenantCatalogSync: profiles={touched}, defaults={defaultsSet}, nestedCleared={nestedCleared}, skipped={skipped}.");
        return new Result(touched, defaultsSet, nestedCleared, skipped);
    }

    private static ApplicationProfile? FindProfile(
        IReadOnlyList<ApplicationProfile> profiles,
        ApplicationProfileApprovalLegVersionTenantCatalogRow row)
    {
        var code = row.ProfileCode?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(code))
            return null;

        return profiles.FirstOrDefault(p =>
                   string.Equals(p.Code?.Trim(), code, StringComparison.OrdinalIgnoreCase)
                   && p.DefaultProjectContractId == null
                   && !ApplicationProfileCatalogGroupKey.NameLooksLikeContractVariant(p.Name))
               ?? profiles.FirstOrDefault(p =>
                   string.Equals(p.Code?.Trim(), code, StringComparison.OrdinalIgnoreCase)
                   && p.DefaultProjectContractId == null)
               ?? profiles.FirstOrDefault(p =>
                   string.Equals(p.Code?.Trim(), code, StringComparison.OrdinalIgnoreCase));
    }
}