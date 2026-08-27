using Visa2026.Module.BusinessObjects;
using Visa2026.Module.DatabaseUpdate.LookupCatalogs;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Builds read-only <see cref="ApplicationProfile"/> preview rows from the ApplicationType configuration catalog.
/// Used by VISA2014 Wave 0 catalog export (tenant JSON proposal).
/// </summary>
public static class ApplicationProfileCatalogPreviewHelper
{
    public static bool TryBuild(string? applicationTypeName, out ApplicationProfileCatalogPreviewRow row)
    {
        row = null!;
        if (string.IsNullOrWhiteSpace(applicationTypeName))
            return false;

        if (!ApplicationTypeConfigurationSeed.TryGetByName(applicationTypeName.Trim(), out var configRow))
            return false;

        var type = new ApplicationType { Name = configRow.Name };
        if (ApplicationTypeSelectionCodeSeed.TryGetByName(configRow.Name, out var selectionCode))
            type.SelectionCode = selectionCode;

        ApplicationTypeConfigurationApplier.Apply(type, configRow, overwriteShowFlags: true);

        var profile = new ApplicationProfile();
        ApplicationProfileFromApplicationTypeMapper.Apply(profile, type);

        row = ApplicationProfileCatalogPreviewRow.From(profile, configRow);
        return true;
    }

    public static bool TryBuild(
        string? applicationTypeName,
        string? projectContractCode,
        out ApplicationProfileCatalogPreviewRow row)
    {
        row = null!;
        if (!TryBuildProfileEntity(applicationTypeName, out var profile, out var configRow)
            || profile == null
            || configRow == null)
            return false;

        if (!ApplicationProfileCatalogGrouping.TryResolveGroupKey(
                applicationTypeName,
                projectContractCode,
                out var groupKey))
            return false;

        var contractForPreview = groupKey.Granularity == ApplicationProfileCatalogGranularity.TypeAndContract
            ? groupKey.ProjectContractCode
            : null;

        row = ApplicationProfileCatalogPreviewRow.From(
            profile,
            configRow,
            contractForPreview,
            groupKey.Granularity);
        return true;
    }

    /// <summary>Builds a transient profile for tenant JSON export (not persisted).</summary>
    internal static bool TryBuildProfileEntity(
        string? applicationTypeName,
        out ApplicationProfile? profile,
        out ApplicationTypeConfigurationRow? configRow)
    {
        profile = null;
        configRow = null;
        if (string.IsNullOrWhiteSpace(applicationTypeName))
            return false;

        if (!ApplicationTypeConfigurationSeed.TryGetByName(applicationTypeName.Trim(), out var row))
            return false;

        var type = new ApplicationType { Name = row.Name };
        if (ApplicationTypeSelectionCodeSeed.TryGetByName(row.Name, out var selectionCode))
            type.SelectionCode = selectionCode;

        ApplicationTypeConfigurationApplier.Apply(type, row, overwriteShowFlags: true);

        var built = new ApplicationProfile();
        ApplicationProfileFromApplicationTypeMapper.Apply(built, type);
        profile = built;
        configRow = row;
        return true;
    }

    public static bool TryBuildTenantCatalogRow(
        string? applicationTypeName,
        out ApplicationProfileTenantCatalogRow? catalogRow) =>
        TryBuildTenantCatalogRow(applicationTypeName, projectContractCode: null, out catalogRow);

    public static bool TryBuildTenantCatalogRow(
        string? applicationTypeName,
        string? projectContractCode,
        out ApplicationProfileTenantCatalogRow? catalogRow)
    {
        catalogRow = null;
        if (!TryBuildProfileEntity(applicationTypeName, out var profile, out var configRow)
            || profile == null
            || configRow == null)
            return false;

        if (!ApplicationProfileCatalogGrouping.TryResolveGroupKey(
                applicationTypeName,
                projectContractCode,
                out var groupKey))
            return false;

        var contractForRow = groupKey.Granularity == ApplicationProfileCatalogGranularity.TypeAndContract
            ? groupKey.ProjectContractCode
            : null;

        catalogRow = ApplicationProfileTenantCatalogRow.FromProfile(
            profile,
            applicationTypeName!.Trim(),
            string.Empty,
            contractForRow,
            groupKey.Granularity);
        ApplicationProfileCalikPersonLastCountSeeds.Apply(catalogRow);
        return true;
    }
}

public sealed class ApplicationProfileCatalogPreviewRow
{
    public required string ApplicationTypeName { get; init; }

    public required string ProfileCode { get; init; }

    public required string ProfileName { get; init; }

    public string? ProfileDescription { get; init; }

    public string? SelectionCode { get; init; }

    public ApplicationProfileInstanceProgressRouteKind ProgressRoute { get; init; }

    public ApplicationProfileActionFamily ActionFamily { get; init; }

    public ApplicationProfileRegistrationKind RegistrationKind { get; init; }

    public bool ForEmployee { get; init; }

    public bool ForFamilyMember { get; init; }

    public bool ForTemporaryVisitor { get; init; }

    public bool ProduceInvitation { get; init; }

    public bool ProduceWorkPermit { get; init; }

    public bool ProduceVisa { get; init; }

    public bool ProduceBorderZone { get; init; }

    public bool ProduceWorkLocation { get; init; }

    public bool ProduceRejection { get; init; }

    public bool CancelInvitations { get; init; }

    public bool CancelWorkPermits { get; init; }

    public bool CancelVisas { get; init; }

    public bool CancelBorderZonePermits { get; init; }

    public bool CancelApplicationProfileInstances { get; init; }

    public bool ChangeInvitations { get; init; }

    public bool ChangeWorkPermits { get; init; }

    public bool ChangeVisas { get; init; }

    public bool ChangeBorderZonePermits { get; init; }

    public bool ChangeApplicationProfileInstances { get; init; }

    public int MinistrySlaDays { get; init; }

    public int MigrationSlaDays { get; init; }

    public string MigrationSlaProfileCode { get; init; } = string.Empty;

    public bool RequirePersonPassport { get; init; }

    public bool RequirePersonEducation { get; init; }

    public bool RequirePersonPosition { get; init; }

    public bool RequirePersonAddressOfResidence { get; init; }

    public string? DefaultProjectContractCode { get; init; }

    public ApplicationProfileCatalogGranularity Granularity { get; init; }

    public string ProfileCatalogKey { get; init; } = string.Empty;

    internal static ApplicationProfileCatalogPreviewRow From(
        ApplicationProfile profile,
        ApplicationTypeConfigurationRow configRow,
        string? projectContractCode = null,
        ApplicationProfileCatalogGranularity granularity = ApplicationProfileCatalogGranularity.TypeOnly) =>
        new()
        {
            ApplicationTypeName = configRow.Name,
            ProfileCode = profile.Code ?? string.Empty,
            ProfileName = granularity == ApplicationProfileCatalogGranularity.TypeAndContract
                && !string.IsNullOrWhiteSpace(projectContractCode)
                    ? $"{profile.Name ?? string.Empty} ({projectContractCode.Trim()})"
                    : profile.Name ?? string.Empty,
            ProfileDescription = profile.Description,
            SelectionCode = profile.SelectionCode,
            ProgressRoute = profile.ProgressRoute,
            ActionFamily = profile.ActionFamily,
            RegistrationKind = profile.RegistrationKind,
            ForEmployee = profile.ForEmployee,
            ForFamilyMember = profile.ForFamilyMember,
            ForTemporaryVisitor = profile.ForTemporaryVisitor,
            ProduceInvitation = profile.ProduceInvitation,
            ProduceWorkPermit = profile.ProduceWorkPermit,
            ProduceVisa = profile.ProduceVisa,
            ProduceBorderZone = profile.ProduceBorderZone,
            ProduceWorkLocation = profile.ProduceWorkLocation,
            ProduceRejection = profile.ProduceRejection,
            CancelInvitations = profile.CancelInvitations,
            CancelWorkPermits = profile.CancelWorkPermits,
            CancelVisas = profile.CancelVisas,
            CancelBorderZonePermits = profile.CancelBorderZonePermits,
            CancelApplicationProfileInstances = profile.CancelApplicationProfileInstances,
            ChangeInvitations = profile.ChangeInvitations,
            ChangeWorkPermits = profile.ChangeWorkPermits,
            ChangeVisas = profile.ChangeVisas,
            ChangeBorderZonePermits = profile.ChangeBorderZonePermits,
            ChangeApplicationProfileInstances = profile.ChangeApplicationProfileInstances,
            MinistrySlaDays = profile.MinistrySlaDays,
            MigrationSlaDays = profile.MigrationSlaDays,
            MigrationSlaProfileCode = string.Empty,
            RequirePersonPassport = profile.RequirePersonPassport,
            RequirePersonEducation = profile.RequirePersonEducation,
            RequirePersonPosition = profile.RequirePersonPosition,
            RequirePersonAddressOfResidence = profile.RequirePersonAddressOfResidence,
            DefaultProjectContractCode = projectContractCode,
            Granularity = granularity,
            ProfileCatalogKey = ApplicationProfileCatalogGroupKey.BuildCatalogKey(
                configRow.Name,
                granularity == ApplicationProfileCatalogGranularity.TypeAndContract
                    ? projectContractCode
                    : null),
        };

    public Dictionary<string, object?> ToExportDictionary(
        int applicationCount,
        int compositeCount,
        DateTime? firstApplicationDate,
        DateTime? lastApplicationDate,
        int withProjectContractCount,
        int distinctApprovalLegProfileCount,
        string? topApprovalLegProfile) =>
        new(StringComparer.Ordinal)
        {
            ["ApplicationTypeName"] = ApplicationTypeName,
            ["ProfileCatalogKey"] = ProfileCatalogKey,
            ["ProfileGranularity"] = Granularity.ToString(),
            ["DefaultProjectContractCode"] = DefaultProjectContractCode ?? string.Empty,
            ["ProfileCode"] = ProfileCode,
            ["ProfileName"] = ProfileName,
            ["ProfileDescription"] = ProfileDescription ?? string.Empty,
            ["SelectionCode"] = SelectionCode ?? string.Empty,
            ["ProgressRoute"] = ProgressRoute.ToString(),
            ["ActionFamily"] = ActionFamily.ToString(),
            ["RegistrationKind"] = RegistrationKind.ToString(),
            ["ForEmployee"] = ForEmployee,
            ["ForFamilyMember"] = ForFamilyMember,
            ["ForTemporaryVisitor"] = ForTemporaryVisitor,
            ["ProduceInvitation"] = ProduceInvitation,
            ["ProduceWorkPermit"] = ProduceWorkPermit,
            ["ProduceVisa"] = ProduceVisa,
            ["ProduceBorderZone"] = ProduceBorderZone,
            ["ProduceWorkLocation"] = ProduceWorkLocation,
            ["ProduceRejection"] = ProduceRejection,
            ["MinistrySlaDays"] = MinistrySlaDays,
            ["MigrationSlaDays"] = MigrationSlaDays,
            ["MigrationSlaProfileCode"] = MigrationSlaProfileCode,
            ["RequirePersonPassport"] = RequirePersonPassport,
            ["RequirePersonEducation"] = RequirePersonEducation,
            ["RequirePersonPosition"] = RequirePersonPosition,
            ["RequirePersonAddressOfResidence"] = RequirePersonAddressOfResidence,
            ["ApplicationCount"] = applicationCount,
            ["DistinctCompositeCount"] = compositeCount,
            ["FirstApplicationDate"] = firstApplicationDate?.ToString("yyyy-MM-dd") ?? string.Empty,
            ["LastApplicationDate"] = lastApplicationDate?.ToString("yyyy-MM-dd") ?? string.Empty,
            ["WithProjectContractCount"] = withProjectContractCount,
            ["DistinctApprovalLegProfileCount"] = distinctApprovalLegProfileCount,
            ["TopApprovalLegProfile"] = topApprovalLegProfile ?? string.Empty,
            ["Decision"] = string.Empty,
            ["SignOff"] = string.Empty,
        };
}
