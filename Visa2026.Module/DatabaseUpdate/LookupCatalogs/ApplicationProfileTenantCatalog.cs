using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Base;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.DatabaseUpdate;

namespace Visa2026.Module.DatabaseUpdate.LookupCatalogs;

public sealed class ApplicationProfileTenantCatalogFile
{
    public List<ApplicationProfileTenantCatalogRow> Rows { get; set; } = new();
}

public sealed class ApplicationProfileTenantCatalogRow
{
    public string ApplicationTypeName { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? SelectionCode { get; set; }

    public string ProgressRoute { get; set; } = nameof(ApplicationProfileInstanceProgressRouteKind.ViaMinistries);

    public string ActionFamily { get; set; } = nameof(ApplicationProfileActionFamily.Issuance);

    public string RegistrationKind { get; set; } = nameof(ApplicationProfileRegistrationKind.None);

    public bool ForEmployee { get; set; }

    public bool ForFamilyMember { get; set; }

    public bool ForTemporaryVisitor { get; set; }

    public bool ProduceInvitation { get; set; }

    public bool ProduceWorkPermit { get; set; }

    public bool ProduceVisa { get; set; }

    public bool ProduceBorderZone { get; set; }

    public bool ProduceWorkLocation { get; set; }

    public bool ProduceRejection { get; set; }

    public bool CancelInvitations { get; set; }

    public bool CancelWorkPermits { get; set; }

    public bool CancelVisas { get; set; }

    public bool CancelBorderZonePermits { get; set; }

    public bool CancelApplicationProfileInstances { get; set; }

    public int MinistrySlaDays { get; set; }

    public int MigrationSlaDays { get; set; }

    public string? MigrationSlaProfileCode { get; set; }

    public bool RequireVisaType { get; set; }

    public string? DefaultVisaTypeLocalizationKey { get; set; }

    public bool RequireVisaCategory { get; set; }

    public string? DefaultVisaCategoryLocalizationKey { get; set; }

    public bool RequireVisaPeriod { get; set; }

    public string? DefaultVisaPeriodLocalizationKey { get; set; }

    public bool RequireBorderZone { get; set; }

    public bool RequireMigrationService { get; set; }

    public bool RequireStartDate { get; set; }

    public bool RequireEndDate { get; set; }

    public bool RequireRegion { get; set; }

    public string? DefaultRegionLocalizationKey { get; set; }

    public bool RequireCity { get; set; }

    public string? DefaultCityLocalizationKey { get; set; }

    public bool RequireRegionCity { get; set; }

    public bool RequireBusinessTripAddress { get; set; }

    public bool RequireProject { get; set; }

    public bool RequireUrgency { get; set; }

    public string? DefaultUrgencyLocalizationKey { get; set; }

    public bool RequireWorkPermitLocation { get; set; }

    public bool RequireEntryDate { get; set; }

    public bool RequireEntryCheckPoint { get; set; }

    public bool RequirePersonPassport { get; set; }

    public bool RequirePersonEducation { get; set; }

    public bool RequirePersonPosition { get; set; }

    public bool RequirePersonAddressOfResidence { get; set; }

    public bool RequirePersonVisa { get; set; }

    public bool RequirePersonInvitationItem { get; set; }

    public bool RequirePersonWorkPermitItem { get; set; }

    public bool RequirePersonBorderZoneItem { get; set; }

    public bool RequirePersonSalary { get; set; }

    public bool RequirePersonMedical { get; set; }

    public bool RequirePersonRejectionItem { get; set; }

    public bool RequirePersonTravelHistory { get; set; }

    public bool IsActive { get; set; } = true;

    public string? SignOff { get; set; }

    public string? DefaultProjectContractCode { get; set; }

    public string? ProfileCatalogKey { get; set; }

    public static ApplicationProfileTenantCatalogRow FromProfile(
        ApplicationProfile profile,
        string applicationTypeName,
        string migrationSlaProfileCode) =>
        FromProfile(
            profile,
            applicationTypeName,
            migrationSlaProfileCode,
            projectContractCode: null,
            ApplicationProfileCatalogGranularity.TypeOnly);

    public static ApplicationProfileTenantCatalogRow FromProfile(
        ApplicationProfile profile,
        string applicationTypeName,
        string migrationSlaProfileCode,
        string? projectContractCode,
        ApplicationProfileCatalogGranularity granularity)
    {
        var displayName = profile.Name ?? string.Empty;
        if (granularity == ApplicationProfileCatalogGranularity.TypeAndContract
            && !string.IsNullOrWhiteSpace(projectContractCode))
        {
            displayName = $"{displayName} ({projectContractCode.Trim()})";
        }

        return new ApplicationProfileTenantCatalogRow
        {
            ApplicationTypeName = applicationTypeName,
            Code = profile.Code ?? string.Empty,
            Name = displayName,
            Description = profile.Description,
            SelectionCode = profile.SelectionCode,
            ProgressRoute = profile.ProgressRoute.ToString(),
            ActionFamily = profile.ActionFamily.ToString(),
            RegistrationKind = profile.RegistrationKind.ToString(),
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
            MinistrySlaDays = profile.MinistrySlaDays,
            MigrationSlaDays = profile.MigrationSlaDays,
            MigrationSlaProfileCode = migrationSlaProfileCode,
            RequireVisaType = profile.RequireVisaType,
            DefaultVisaTypeLocalizationKey = NullIfEmpty(profile.DefaultVisaType?.LocalizationKey),
            RequireVisaCategory = profile.RequireVisaCategory,
            DefaultVisaCategoryLocalizationKey = NullIfEmpty(profile.DefaultVisaCategory?.LocalizationKey),
            RequireVisaPeriod = profile.RequireVisaPeriod,
            DefaultVisaPeriodLocalizationKey = NullIfEmpty(profile.DefaultVisaPeriod?.LocalizationKey),
            RequireBorderZone = profile.RequireBorderZone,
            RequireMigrationService = profile.RequireMigrationService,
            RequireStartDate = profile.RequireStartDate,
            RequireEndDate = profile.RequireEndDate,
            RequireRegion = profile.RequireRegion,
            DefaultRegionLocalizationKey = NullIfEmpty(profile.DefaultRegion?.LocalizationKey),
            RequireCity = profile.RequireCity,
            DefaultCityLocalizationKey = NullIfEmpty(profile.DefaultCity?.LocalizationKey),
            RequireRegionCity = profile.RequireRegionCity,
            RequireBusinessTripAddress = profile.RequireBusinessTripAddress,
            RequireProject = profile.RequireProject,
            RequireUrgency = profile.RequireUrgency,
            DefaultUrgencyLocalizationKey = NullIfEmpty(profile.DefaultUrgency?.LocalizationKey),
            RequireWorkPermitLocation = profile.RequireWorkPermitLocation,
            RequireEntryDate = profile.RequireEntryDate,
            RequireEntryCheckPoint = profile.RequireEntryCheckPoint,
            RequirePersonPassport = profile.RequirePersonPassport,
            RequirePersonEducation = profile.RequirePersonEducation,
            RequirePersonPosition = profile.RequirePersonPosition,
            RequirePersonAddressOfResidence = profile.RequirePersonAddressOfResidence,
            RequirePersonVisa = profile.RequirePersonVisa,
            RequirePersonInvitationItem = profile.RequirePersonInvitationItem,
            RequirePersonWorkPermitItem = profile.RequirePersonWorkPermitItem,
            RequirePersonBorderZoneItem = profile.RequirePersonBorderZoneItem,
            RequirePersonSalary = profile.RequirePersonSalary,
            RequirePersonMedical = profile.RequirePersonMedical,
            RequirePersonRejectionItem = profile.RequirePersonRejectionItem,
            RequirePersonTravelHistory = profile.RequirePersonTravelHistory,
            IsActive = profile.IsActive,
            SignOff = string.Empty,
            DefaultProjectContractCode = string.IsNullOrWhiteSpace(projectContractCode)
                ? null
                : projectContractCode.Trim(),
            ProfileCatalogKey = ApplicationProfileCatalogGroupKey.BuildCatalogKey(
                applicationTypeName,
                granularity == ApplicationProfileCatalogGranularity.TypeAndContract
                    ? projectContractCode
                    : null),
        };
    }

    public static ApplicationProfileTenantCatalogRow FromPreview(ApplicationProfileCatalogPreviewRow preview) =>
        new()
        {
            ApplicationTypeName = preview.ApplicationTypeName,
            Code = preview.ProfileCode,
            Name = preview.ProfileName,
            Description = preview.ProfileDescription,
            SelectionCode = preview.SelectionCode,
            ProgressRoute = preview.ProgressRoute.ToString(),
            ActionFamily = preview.ActionFamily.ToString(),
            RegistrationKind = preview.RegistrationKind.ToString(),
            ForEmployee = preview.ForEmployee,
            ForFamilyMember = preview.ForFamilyMember,
            ForTemporaryVisitor = preview.ForTemporaryVisitor,
            ProduceInvitation = preview.ProduceInvitation,
            ProduceWorkPermit = preview.ProduceWorkPermit,
            ProduceVisa = preview.ProduceVisa,
            ProduceBorderZone = preview.ProduceBorderZone,
            ProduceWorkLocation = preview.ProduceWorkLocation,
            ProduceRejection = preview.ProduceRejection,
            CancelInvitations = preview.CancelInvitations,
            CancelWorkPermits = preview.CancelWorkPermits,
            CancelVisas = preview.CancelVisas,
            CancelBorderZonePermits = preview.CancelBorderZonePermits,
            CancelApplicationProfileInstances = preview.CancelApplicationProfileInstances,
            MinistrySlaDays = preview.MinistrySlaDays,
            MigrationSlaDays = preview.MigrationSlaDays,
            MigrationSlaProfileCode = preview.MigrationSlaProfileCode,
            RequirePersonPassport = preview.RequirePersonPassport,
            RequirePersonEducation = preview.RequirePersonEducation,
            RequirePersonPosition = preview.RequirePersonPosition,
            RequirePersonAddressOfResidence = preview.RequirePersonAddressOfResidence,
            IsActive = true,
            SignOff = string.Empty,
            DefaultProjectContractCode = preview.DefaultProjectContractCode,
            ProfileCatalogKey = preview.ProfileCatalogKey,
        };

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

internal static class ApplicationProfileTenantCatalogLoader
{
    private const string DefaultFileName = "application-profile.json";
    private const string CalikFileName = "application-profile.calik-energi.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true,
    };

    public static ApplicationProfileTenantCatalogFile? Load()
    {
        var json = LookupCatalogResourceLoader.TryReadTenantOverlayText(CalikFileName)
            ?? LookupCatalogResourceLoader.TryReadTenantOverlayText(DefaultFileName)
            ?? LookupCatalogResourceLoader.TryReadEmbeddedLookupCatalogText("tenant/" + CalikFileName)
            ?? LookupCatalogResourceLoader.TryReadEmbeddedLookupCatalogText("tenant/" + DefaultFileName);

        if (string.IsNullOrWhiteSpace(json))
            return null;

        return JsonSerializer.Deserialize<ApplicationProfileTenantCatalogFile>(json, JsonOptions);
    }

    public static bool TryLoadRows(out List<ApplicationProfileTenantCatalogRow> rows)
    {
        rows = new List<ApplicationProfileTenantCatalogRow>();
        var catalog = Load();
        if (catalog?.Rows == null || catalog.Rows.Count == 0)
            return false;

        rows = catalog.Rows.Where(r => r != null).ToList();
        return rows.Count > 0;
    }
}

internal static class ApplicationProfileTenantCatalogSync
{
    public readonly record struct Result(int Created, int Updated, int Skipped);

    public static Result Sync(IObjectSpace objectSpace)
    {
        if (!TryLoadCatalogRows(objectSpace, out var rows))
            return default;

        var contracts = objectSpace.GetObjectsQuery<ProjectContract>().ToList();
        var existingProfiles = objectSpace.GetObjectsQuery<ApplicationProfile>().ToList();

        int created = 0, updated = 0, skipped = 0;

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.Code))
            {
                skipped++;
                continue;
            }

            var contract = ResolveProjectContract(contracts, row.DefaultProjectContractCode);
            var profile = FindExistingProfile(existingProfiles, row.Code, contract, row.DefaultProjectContractCode);
            if (profile == null)
            {
                profile = objectSpace.CreateObject<ApplicationProfile>();
                existingProfiles.Add(profile);
                created++;
            }
            else
            {
                updated++;
            }

            ApplyRow(objectSpace, profile, row, contracts);
        }

        if (created > 0 || updated > 0 || skipped > 0)
        {
            Tracing.Tracer.LogText(
                $"ApplicationProfileTenantCatalogSync: created={created}, updated={updated}, skipped={skipped}.");
        }

        return new Result(created, updated, skipped);
    }

    private static ApplicationProfile? FindExistingProfile(
        IReadOnlyList<ApplicationProfile> profiles,
        string code,
        ProjectContract? contract,
        string? legacyContractCode)
    {
        var contractId = contract?.ID;
        return ApplicationProfileCatalogGroupKey.FindProfile(
            profiles,
            code,
            contractId,
            legacyContractCode);
    }

    private static bool TryLoadCatalogRows(IObjectSpace objectSpace, out List<ApplicationProfileTenantCatalogRow> rows)
    {
        rows = new List<ApplicationProfileTenantCatalogRow>();
        var catalog = ApplicationProfileTenantCatalogLoader.Load();
        if (catalog?.Rows == null || catalog.Rows.Count == 0)
        {
            Tracing.Tracer.LogText(
                "ApplicationProfileTenantCatalogSync: no rows in tenant application-profile*.json (embedded or disk overlay).");
            return false;
        }

        rows = catalog.Rows.Where(r => r != null).ToList();
        return rows.Count > 0;
    }

    private static void ApplyRow(
        IObjectSpace objectSpace,
        ApplicationProfile profile,
        ApplicationProfileTenantCatalogRow row,
        IReadOnlyList<ProjectContract> contracts)
    {
        profile.Code = row.Code.Trim();
        profile.Name = string.IsNullOrWhiteSpace(row.Name) ? profile.Code : row.Name.Trim();
        profile.Description = string.IsNullOrWhiteSpace(row.Description) ? null : row.Description.Trim();
        profile.SelectionCode = string.IsNullOrWhiteSpace(row.SelectionCode) ? null : row.SelectionCode.Trim();

        profile.ProgressRoute = ParseEnum(row.ProgressRoute, ApplicationProfileInstanceProgressRouteKind.ViaMinistries);
        profile.ActionFamily = ParseEnum(row.ActionFamily, ApplicationProfileActionFamily.Issuance);
        profile.RegistrationKind = ApplicationProfileRegistrationKindHelper.Resolve(
            profile.ActionFamily,
            ParseEnum(row.RegistrationKind, ApplicationProfileRegistrationKind.None));

        profile.ForEmployee = row.ForEmployee;
        profile.ForFamilyMember = row.ForFamilyMember;
        profile.ForTemporaryVisitor = row.ForTemporaryVisitor;

        profile.ProduceInvitation = row.ProduceInvitation;
        profile.ProduceWorkPermit = row.ProduceWorkPermit;
        profile.ProduceVisa = row.ProduceVisa;
        profile.ProduceBorderZone = row.ProduceBorderZone;
        profile.ProduceWorkLocation = row.ProduceWorkLocation;
        profile.ProduceRejection = row.ProduceRejection;

        profile.CancelInvitations = row.CancelInvitations;
        profile.CancelWorkPermits = row.CancelWorkPermits;
        profile.CancelVisas = row.CancelVisas;
        profile.CancelBorderZonePermits = row.CancelBorderZonePermits;
        profile.CancelApplicationProfileInstances = row.CancelApplicationProfileInstances;

        profile.MinistrySlaDays = row.MinistrySlaDays > 0 ? row.MinistrySlaDays : 14;
        profile.MigrationSlaDays = row.MigrationSlaDays > 0 ? row.MigrationSlaDays : 14;

        profile.RequireVisaType = row.RequireVisaType;
        profile.DefaultVisaType = ResolveLookup<VisaType>(objectSpace, row.DefaultVisaTypeLocalizationKey);
        profile.RequireVisaCategory = row.RequireVisaCategory;
        profile.DefaultVisaCategory = ResolveLookup<VisaCategory>(objectSpace, row.DefaultVisaCategoryLocalizationKey);
        profile.RequireVisaPeriod = row.RequireVisaPeriod;
        profile.DefaultVisaPeriod = ResolveLookup<VisaPeriod>(objectSpace, row.DefaultVisaPeriodLocalizationKey);
        profile.RequireBorderZone = row.RequireBorderZone;
        profile.RequireMigrationService = row.RequireMigrationService;
        profile.RequireStartDate = row.RequireStartDate;
        profile.RequireEndDate = row.RequireEndDate;
        profile.RequireRegion = row.RequireRegion || row.RequireRegionCity;
        profile.DefaultRegion = ResolveLookup<Region>(objectSpace, row.DefaultRegionLocalizationKey);
        profile.RequireCity = row.RequireCity || row.RequireRegionCity;
        profile.DefaultCity = ResolveLookup<City>(objectSpace, row.DefaultCityLocalizationKey);
        profile.RequireRegionCity = row.RequireRegionCity;
        profile.RequireBusinessTripAddress = row.RequireBusinessTripAddress;
        profile.RequireProject = row.RequireProject;
        profile.DefaultProjectContract = ResolveProjectContract(contracts, row.DefaultProjectContractCode);
        profile.RequireUrgency = row.RequireUrgency;
        profile.DefaultUrgency = ResolveLookup<Urgency>(objectSpace, row.DefaultUrgencyLocalizationKey);
        profile.RequireWorkPermitLocation = row.RequireWorkPermitLocation;
        profile.RequireEntryDate = row.RequireEntryDate;
        profile.RequireEntryCheckPoint = row.RequireEntryCheckPoint;

        profile.RequirePersonPassport = row.RequirePersonPassport;
        profile.RequirePersonEducation = row.RequirePersonEducation;
        profile.RequirePersonPosition = row.RequirePersonPosition;
        profile.RequirePersonAddressOfResidence = row.RequirePersonAddressOfResidence;
        profile.RequirePersonVisa = row.RequirePersonVisa;
        profile.RequirePersonInvitationItem = row.RequirePersonInvitationItem;
        profile.RequirePersonWorkPermitItem = row.RequirePersonWorkPermitItem;
        profile.RequirePersonBorderZoneItem = row.RequirePersonBorderZoneItem;
        profile.RequirePersonSalary = row.RequirePersonSalary;
        profile.RequirePersonMedical = row.RequirePersonMedical;
        profile.RequirePersonRejectionItem = row.RequirePersonRejectionItem;
        profile.RequirePersonTravelHistory = row.RequirePersonTravelHistory;
        ApplicationProfileRegistrationKindHelper.ApplyRegistrationPersonDefaults(profile);

        profile.IsActive = row.IsActive;
    }

    private static ProjectContract? ResolveProjectContract(
        IReadOnlyList<ProjectContract> contracts,
        string? legacyContractCode)
    {
        if (string.IsNullOrWhiteSpace(legacyContractCode))
            return null;

        var code = legacyContractCode.Trim();
        var matches = contracts
            .Where(c => ProjectContractTitleMatches(c.NameTm, code)
                        || KeysEqual(c.Code, code))
            .ToList();

        if (matches.Count == 0)
            return null;

        if (matches.Count == 1)
            return matches[0];

        return matches.FirstOrDefault(c =>
                   c.NameTm.Contains("2 ylalaşyk", StringComparison.OrdinalIgnoreCase)
                   || c.NameTm.Contains("2 ylalasyk", StringComparison.OrdinalIgnoreCase))
               ?? matches[0];
    }

    private static bool ProjectContractTitleMatches(string? nameTm, string legacyCode)
    {
        if (string.IsNullOrWhiteSpace(nameTm))
            return false;

        var title = nameTm.Trim();
        var code = legacyCode.Trim();
        return title.StartsWith(code, StringComparison.OrdinalIgnoreCase)
               || KeysEqual(title, code);
    }

    private static TLookup? ResolveLookup<TLookup>(IObjectSpace objectSpace, string? localizationKey)
        where TLookup : LookupBase
    {
        if (string.IsNullOrWhiteSpace(localizationKey))
            return null;

        var key = localizationKey.Trim();
        return objectSpace.GetObjectsQuery<TLookup>()
            .AsEnumerable()
            .FirstOrDefault(item => KeysEqual(item.LocalizationKey, key) || KeysEqual(item.Code, key));
    }

    private static bool KeysEqual(string? left, string? right) =>
        string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);

    private static TEnum ParseEnum<TEnum>(string? value, TEnum fallback) where TEnum : struct, Enum =>
        Enum.TryParse<TEnum>(value?.Trim(), ignoreCase: true, out var parsed) ? parsed : fallback;
}
