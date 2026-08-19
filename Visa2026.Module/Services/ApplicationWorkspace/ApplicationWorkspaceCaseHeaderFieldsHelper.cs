using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;

namespace Visa2026.Module.Services.ApplicationWorkspace;

public static class ApplicationWorkspaceCaseHeaderFieldsHelper
{
    public const string VisaType = "VisaType";
    public const string VisaCategory = "VisaCategory";
    public const string VisaPeriod = "VisaPeriod";
    public const string BorderZone = "BorderZone";
    public const string MigrationService = "MigrationService";
    public const string StartDate = "StartDate";
    public const string EndDate = "EndDate";
    public const string FromCity = "FromCity";
    public const string ToCity = "ToCity";
    public const string Region = "Region";
    public const string City = "City";
    public const string Project = "Project";
    public const string Urgency = "Urgency";
    public const string WorkPermitLocation = "WorkPermitLocation";
    public const string EntryCheckPoint = "EntryCheckPoint";

    public static IReadOnlyList<ApplicationWorkspaceCaseHeaderField> Build(
        ApplicationProfileInstance application,
        ApplicationProfile? profile,
        IObjectSpace? objectSpace)
    {
        ArgumentNullException.ThrowIfNull(application);

        var catalogs = objectSpace == null ? Catalogs.Empty : Catalogs.Load(objectSpace);
        var fields = new List<ApplicationWorkspaceCaseHeaderField>();

        AddLookup(fields, VisaType, "Visa type", "blue", "🛂",
            Visible(profile, p => p.RequireVisaType, ApplicationProfileConfigurationResolver.ShowVisaType, application),
            application.VisaType?.ID, LookupLabel(application.VisaType), catalogs.VisaTypes, readOnly: false);

        AddLookup(fields, VisaCategory, "Category", "purple", "◆",
            Visible(profile, p => p.RequireVisaCategory, ApplicationProfileConfigurationResolver.ShowVisaCategory, application),
            application.VisaCategory?.ID, LookupLabel(application.VisaCategory), catalogs.VisaCategories, readOnly: false);

        AddLookup(fields, VisaPeriod, "Period", "green", "📅",
            Visible(profile, p => p.RequireVisaPeriod, ApplicationProfileConfigurationResolver.ShowVisaPeriod, application),
            application.VisaPeriod?.ID, LookupLabel(application.VisaPeriod), catalogs.VisaPeriods, readOnly: false);

        AddLookup(fields, Project, "Project", "orange", "💼",
            Visible(profile, p => p.RequireProject, ApplicationProfileConfigurationResolver.ShowProjectContract, application),
            application.ProjectContract?.ID, LookupLabel(application.ProjectContract), catalogs.ProjectContracts, readOnly: false);

        AddDate(fields, StartDate, "Start date", "green", "📅",
            Visible(profile, p => p.RequireStartDate, ApplicationProfileConfigurationResolver.ShowBusinessTrips, application),
            application.BusinessTripStartDate);

        AddLookup(fields, EntryCheckPoint, "Entry check point", "blue", "📍",
            Visible(profile, p => p.RequireEntryCheckPoint, ApplicationProfileConfigurationResolver.ShowEntryCheckPoint, application),
            application.EntryCheckPoint?.ID, LookupLabel(application.EntryCheckPoint), catalogs.CheckPoints, readOnly: false);

        AddLookup(fields, Urgency, "Urgency", "orange", "⚡",
            Visible(profile, p => p.RequireUrgency, ApplicationProfileConfigurationResolver.ShowUrgency, application),
            application.Urgency?.ID, LookupLabel(application.Urgency), catalogs.Urgencies, readOnly: false);

        AddLookup(fields, BorderZone, "Border zone", "teal", "📍",
            Visible(profile, p => p.RequireBorderZone, ApplicationProfileConfigurationResolver.ShowBorderZoneLocation, application),
            MatchOptionId(application.BorderZoneLocation, catalogs.BorderZones),
            DisplayOrDash(application.BorderZoneLocation_NameTm, application.BorderZoneLocation),
            catalogs.BorderZones, readOnly: false);

        AddDate(fields, EndDate, "End date", "green", "📅",
            Visible(profile, p => p.RequireEndDate, ApplicationProfileConfigurationResolver.ShowBusinessTrips, application),
            application.BusinessTripEndDate);

        AddLookup(fields, MigrationService, "Migration service", "teal", "🏛",
            Visible(profile, p => p.RequireMigrationService, ApplicationProfileConfigurationResolver.ShowMigrationService, application),
            application.MigrationService?.ID, LookupLabel(application.MigrationService), catalogs.MigrationServices, readOnly: false);

        AddLookup(fields, FromCity, "From city", "purple", "📍",
            Visible(profile, p => p.RequireRegionCity, ApplicationProfileConfigurationResolver.ShowFromCity, application),
            application.FromCity?.ID, LookupLabel(application.FromCity), catalogs.Cities, readOnly: false);

        AddLookup(fields, ToCity, "To city", "purple", "📍",
            Visible(profile, p => p.RequireRegionCity, ApplicationProfileConfigurationResolver.ShowToCity, application),
            application.ToCity?.ID, LookupLabel(application.ToCity), catalogs.Cities, readOnly: false);

        AddLookup(fields, Region, "Region", "purple", "📍",
            Visible(profile, p => p.RequireRegion, ApplicationProfileConfigurationResolver.ShowRegion, application),
            application.Region?.ID, LookupLabel(application.Region), catalogs.Regions, readOnly: false);

        AddLookup(fields, City, "City", "purple", "📍",
            Visible(profile, p => p.RequireCity, ApplicationProfileConfigurationResolver.ShowCity, application),
            application.City?.ID, LookupLabel(application.City), catalogs.Cities, readOnly: false);

        AddLookup(fields, WorkPermitLocation, "Work permit location", "blue", "🏢",
            Visible(profile, p => p.RequireWorkPermitLocation, ApplicationProfileConfigurationResolver.ShowMovementPermitLocation, application),
            application.MovementPermitLocation?.ID, LookupLabel(application.MovementPermitLocation),
            catalogs.MovementPermitLocations, readOnly: false);

        return fields;
    }

    public static bool TryApply(
        ApplicationProfileInstance application,
        IObjectSpace objectSpace,
        ApplicationWorkspaceCaseHeaderFieldUpdate update,
        out string? error)
    {
        ArgumentNullException.ThrowIfNull(application);
        ArgumentNullException.ThrowIfNull(objectSpace);
        error = null;

        var key = update?.Key ?? string.Empty;
        var value = update?.Value;
        var profile = application.ApplicationProfile;

        switch (key)
        {
            case VisaType:
                if (!Visible(profile, p => p.RequireVisaType, ApplicationProfileConfigurationResolver.ShowVisaType, application))
                    return Hidden(out error);
                return SetLookup<VisaType>(objectSpace, value, item => application.VisaType = item!, out error);
            case VisaCategory:
                if (!Visible(profile, p => p.RequireVisaCategory, ApplicationProfileConfigurationResolver.ShowVisaCategory, application))
                    return Hidden(out error);
                return SetLookup<VisaCategory>(objectSpace, value, item => application.VisaCategory = item!, out error);
            case VisaPeriod:
                if (!Visible(profile, p => p.RequireVisaPeriod, ApplicationProfileConfigurationResolver.ShowVisaPeriod, application))
                    return Hidden(out error);
                return SetLookup<VisaPeriod>(objectSpace, value, item => application.VisaPeriod = item!, out error);
            case BorderZone:
                if (!Visible(profile, p => p.RequireBorderZone, ApplicationProfileConfigurationResolver.ShowBorderZoneLocation, application))
                    return Hidden(out error);
                return SetBorderZone(objectSpace, value, application, out error);
            case EntryCheckPoint:
                if (!Visible(profile, p => p.RequireEntryCheckPoint, ApplicationProfileConfigurationResolver.ShowEntryCheckPoint, application))
                    return Hidden(out error);
                return SetLookup<CheckPoint>(objectSpace, value, item => application.EntryCheckPoint = item, out error);
            case MigrationService:
                if (!Visible(profile, p => p.RequireMigrationService, ApplicationProfileConfigurationResolver.ShowMigrationService, application))
                    return Hidden(out error);
                return SetLookup<MigrationService>(objectSpace, value, item => application.MigrationService = item!, out error);
            case StartDate:
                if (!Visible(profile, p => p.RequireStartDate, ApplicationProfileConfigurationResolver.ShowBusinessTrips, application))
                    return Hidden(out error);
                return SetDate(value, date => application.BusinessTripStartDate = date, out error);
            case EndDate:
                if (!Visible(profile, p => p.RequireEndDate, ApplicationProfileConfigurationResolver.ShowBusinessTrips, application))
                    return Hidden(out error);
                return SetDate(value, date => application.BusinessTripEndDate = date, out error);
            case FromCity:
                if (!Visible(profile, p => p.RequireRegionCity, ApplicationProfileConfigurationResolver.ShowFromCity, application))
                    return Hidden(out error);
                return SetLookup<City>(objectSpace, value, item => application.FromCity = item!, out error);
            case ToCity:
                if (!Visible(profile, p => p.RequireRegionCity, ApplicationProfileConfigurationResolver.ShowToCity, application))
                    return Hidden(out error);
                return SetLookup<City>(objectSpace, value, item => application.ToCity = item!, out error);
            case Region:
                if (!Visible(profile, p => p.RequireRegion, ApplicationProfileConfigurationResolver.ShowRegion, application))
                    return Hidden(out error);
                return SetLookup<Region>(objectSpace, value, item =>
                {
                    application.Region = item;
                    if (application.City?.Region != null && item != null && application.City.Region.ID != item.ID)
                        application.City = null;
                }, out error);
            case City:
                if (!Visible(profile, p => p.RequireCity, ApplicationProfileConfigurationResolver.ShowCity, application))
                    return Hidden(out error);
                return SetLookup<City>(objectSpace, value, item =>
                {
                    application.City = item;
                    if (item?.Region != null)
                        application.Region = item.Region;
                }, out error);
            case Project:
                if (!Visible(profile, p => p.RequireProject, ApplicationProfileConfigurationResolver.ShowProjectContract, application))
                    return Hidden(out error);
                return SetLookup<ProjectContract>(objectSpace, value, item => application.ProjectContract = item!, out error);
            case Urgency:
                if (!Visible(profile, p => p.RequireUrgency, ApplicationProfileConfigurationResolver.ShowUrgency, application))
                    return Hidden(out error);
                return SetLookup<Urgency>(objectSpace, value, item => application.Urgency = item!, out error);
            case WorkPermitLocation:
                if (!Visible(profile, p => p.RequireWorkPermitLocation, ApplicationProfileConfigurationResolver.ShowMovementPermitLocation, application))
                    return Hidden(out error);
                return SetLookup<MovementPermitLocation>(objectSpace, value, item => application.MovementPermitLocation = item!, out error);
            default:
                error = "That field cannot be edited here.";
                return false;
        }
    }

    private static bool Visible(
        ApplicationProfile? profile,
        Func<ApplicationProfile, bool> require,
        Func<ApplicationProfileInstance, bool> cfgShow,
        ApplicationProfileInstance application) =>
        profile != null ? require(profile) : cfgShow(application);

    private static void AddLookup(
        List<ApplicationWorkspaceCaseHeaderField> fields,
        string key,
        string label,
        string tone,
        string glyph,
        bool visible,
        Guid? selectedId,
        string displayValue,
        IReadOnlyList<ApplicationWorkspaceLookupOption> options,
        bool readOnly)
    {
        if (!visible)
            return;

        fields.Add(new ApplicationWorkspaceCaseHeaderField
        {
            Key = key,
            Label = label,
            Kind = ApplicationWorkspaceCaseHeaderFieldKind.Lookup,
            Tone = tone,
            Glyph = glyph,
            SelectedId = selectedId,
            Value = selectedId is Guid id && id != Guid.Empty ? id.ToString("D") : string.Empty,
            DisplayValue = string.IsNullOrWhiteSpace(displayValue) ? "—" : displayValue,
            Options = options,
            ReadOnly = readOnly,
        });
    }

    private static void AddDate(
        List<ApplicationWorkspaceCaseHeaderField> fields,
        string key,
        string label,
        string tone,
        string glyph,
        bool visible,
        DateTime? date)
    {
        if (!visible)
            return;

        fields.Add(new ApplicationWorkspaceCaseHeaderField
        {
            Key = key,
            Label = label,
            Kind = ApplicationWorkspaceCaseHeaderFieldKind.Date,
            Tone = tone,
            Glyph = glyph,
            Value = date.HasValue ? date.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : string.Empty,
            DisplayValue = date.HasValue ? date.Value.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) : "—",
            ReadOnly = false,
        });
    }

    private static bool SetLookup<T>(
        IObjectSpace objectSpace,
        string? value,
        Action<T?> assign,
        out string? error)
        where T : class
    {
        error = null;
        if (string.IsNullOrWhiteSpace(value) || !Guid.TryParse(value, out var id) || id == Guid.Empty)
        {
            assign(null);
            return true;
        }

        var item = objectSpace.GetObjectByKey<T>(id);
        if (item == null)
        {
            error = "The selected value is no longer available.";
            return false;
        }

        assign(item);
        return true;
    }

    private static bool SetBorderZone(
        IObjectSpace objectSpace,
        string? value,
        ApplicationProfileInstance application,
        out string? error)
    {
        if (string.IsNullOrWhiteSpace(value) || !Guid.TryParse(value, out var id) || id == Guid.Empty)
        {
            error = null;
            application.BorderZoneLocation = null;
            return true;
        }

        var item = objectSpace.GetObjectByKey<BorderZoneName>(id);
        if (item == null)
        {
            error = "The selected value is no longer available.";
            return false;
        }

        error = null;
        application.BorderZoneLocation = LookupLabel(item);
        return true;
    }

    private static Guid? MatchOptionId(string? stored, IReadOnlyList<ApplicationWorkspaceLookupOption> options)
    {
        if (string.IsNullOrWhiteSpace(stored) || options.Count == 0)
            return null;

        var first = stored.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault() ?? stored.Trim();
        var match = options.FirstOrDefault(option =>
            string.Equals(option.DisplayName, first, StringComparison.CurrentCultureIgnoreCase));
        return match?.Id;
    }

    private static string DisplayOrDash(string? preferred, string? fallback)
    {
        if (!string.IsNullOrWhiteSpace(preferred) && !string.Equals(preferred.Trim(), "Ýok", StringComparison.OrdinalIgnoreCase))
            return preferred.Trim();
        if (!string.IsNullOrWhiteSpace(fallback) && !string.Equals(fallback.Trim(), "Ýok", StringComparison.OrdinalIgnoreCase))
            return fallback.Trim();
        return string.Empty;
    }

    private static bool SetDate(string? value, Action<DateTime?> assign, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            assign(null);
            return true;
        }

        if (!DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            && !DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
        {
            error = "Enter a valid date.";
            return false;
        }

        assign(date.Date);
        return true;
    }

    private static bool Hidden(out string? error)
    {
        error = "That field is not required on this profile.";
        return false;
    }

    private static string LookupLabel(LookupBase? item)
    {
        if (item == null)
            return string.Empty;

        var localized = LookupLocalization.GetDisplayName(item);
        if (!string.IsNullOrWhiteSpace(localized))
            return localized;
        if (!string.IsNullOrWhiteSpace(item.NameTm))
            return item.NameTm;
#pragma warning disable CS0618
        if (!string.IsNullOrWhiteSpace(item.Name))
            return item.Name;
#pragma warning restore CS0618
        return string.IsNullOrWhiteSpace(item.Code) ? string.Empty : item.Code;
    }

    private sealed class Catalogs
    {
        public static Catalogs Empty { get; } = new();

        public IReadOnlyList<ApplicationWorkspaceLookupOption> VisaTypes { get; init; } = [];
        public IReadOnlyList<ApplicationWorkspaceLookupOption> VisaCategories { get; init; } = [];
        public IReadOnlyList<ApplicationWorkspaceLookupOption> VisaPeriods { get; init; } = [];
        public IReadOnlyList<ApplicationWorkspaceLookupOption> MigrationServices { get; init; } = [];
        public IReadOnlyList<ApplicationWorkspaceLookupOption> ProjectContracts { get; init; } = [];
        public IReadOnlyList<ApplicationWorkspaceLookupOption> Urgencies { get; init; } = [];
        public IReadOnlyList<ApplicationWorkspaceLookupOption> Cities { get; init; } = [];
        public IReadOnlyList<ApplicationWorkspaceLookupOption> Regions { get; init; } = [];
        public IReadOnlyList<ApplicationWorkspaceLookupOption> MovementPermitLocations { get; init; } = [];
        public IReadOnlyList<ApplicationWorkspaceLookupOption> CheckPoints { get; init; } = [];
        public IReadOnlyList<ApplicationWorkspaceLookupOption> BorderZones { get; init; } = [];

        public static Catalogs Load(IObjectSpace objectSpace) => new()
        {
            VisaTypes = LoadItems<VisaType>(objectSpace),
            VisaCategories = LoadItems<VisaCategory>(objectSpace),
            VisaPeriods = LoadItems<VisaPeriod>(objectSpace),
            MigrationServices = LoadItems<MigrationService>(objectSpace),
            ProjectContracts = LoadItems<ProjectContract>(objectSpace),
            Urgencies = LoadItems<Urgency>(objectSpace),
            Cities = LoadItems<City>(objectSpace),
            Regions = LoadItems<Region>(objectSpace),
            MovementPermitLocations = LoadItems<MovementPermitLocation>(objectSpace),
            CheckPoints = LoadItems<CheckPoint>(objectSpace),
            BorderZones = LoadItems<BorderZoneName>(objectSpace),
        };

        private static IReadOnlyList<ApplicationWorkspaceLookupOption> LoadItems<T>(IObjectSpace objectSpace)
            where T : LookupBase
        {
            return objectSpace.GetObjects(typeof(T))
                .Cast<T>()
                .Select(item => new ApplicationWorkspaceLookupOption
                {
                    Id = item.ID,
                    DisplayName = LookupLabel(item),
                })
                .Where(item => !string.IsNullOrWhiteSpace(item.DisplayName))
                .OrderBy(item => item.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }
    }
}