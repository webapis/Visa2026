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
    public const string BusinessTripAddress = "BusinessTripAddress";
    public const string Purpose = "Purpose";
    public const int PurposeMaxLength = 700;
    public const string Project = "Project";
    public const string Urgency = "Urgency";
    public const string WorkPermitLocation = "WorkPermitLocation";
    public const string EntryCheckPoint = "EntryCheckPoint";
    public const string InstanceNumber = "InstanceNumber";
    public const string InstanceDate = "InstanceDate";
    public const int InstanceNumberMaxLength = 100;

    public static IReadOnlyList<ApplicationWorkspaceCaseHeaderField> Build(
        ApplicationProfileInstance application,
        ApplicationProfile? profile,
        IObjectSpace? objectSpace,
        bool loadLookupCatalogs = false)
    {
        ArgumentNullException.ThrowIfNull(application);

        // Lookup catalogs (cities, contracts, …) are only needed for Edit dropdowns.
        var catalogs = loadLookupCatalogs && objectSpace != null
            ? Catalogs.Load(objectSpace)
            : Catalogs.Empty;
        var fields = new List<ApplicationWorkspaceCaseHeaderField>();

        AddShortText(fields, InstanceNumber, "Application number", "blue", "№",
            visible: true,
            FormatInstanceNumber(application),
            InstanceNumberMaxLength);

        AddDate(fields, InstanceDate, "Application date", "green", "📅",
            visible: true,
            application.ApplicationDate == default ? null : application.ApplicationDate);

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

        AddCommaSeparatedMultiSelect(fields, BorderZone, "Border zone", "teal", "📍",
            Visible(profile, p => p.RequireBorderZone, ApplicationProfileConfigurationResolver.ShowBorderZoneLocation, application),
            application.BorderZoneLocation,
            FormatBorderZoneDisplay(application.BorderZoneLocation_NameTm, application.BorderZoneLocation),
            catalogs.BorderZoneNames, readOnly: false);

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

        AddLookup(fields, BusinessTripAddress, "Business trip address", "purple", "📍",
            Visible(profile, p => p.RequireBusinessTripAddress, ApplicationProfileConfigurationResolver.ShowBusinessTripAddress, application),
            application.BusinessTripAddress?.ID,
            FormatBusinessTripAddress(application.BusinessTripAddress),
            catalogs.BusinessTripAddresses, readOnly: false);

        AddText(fields, Purpose, "Purpose", "blue", "📝",
            Visible(profile, p => p.RequirePurpose, ApplicationProfileConfigurationResolver.ShowPurpose, application),
            application.Purpose);

        AddCommaSeparatedMultiSelect(fields, WorkPermitLocation, "Work permit location", "blue", "🏢",
            Visible(profile, p => p.RequireWorkPermitLocation, ApplicationProfileConfigurationResolver.ShowMovementPermitLocation, application),
            application.MovementPermitLocation,
            application.MovementPermitLocation_NameTm,
            catalogs.WorkPermittedLocationNames, readOnly: false);

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
            case InstanceNumber:
                return TrySetInstanceNumber(application, value, out error);
            case InstanceDate:
                return TrySetInstanceDate(application, value, out error);
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
                return SetBorderZone(value, application, out error);
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
            case BusinessTripAddress:
                if (!Visible(profile, p => p.RequireBusinessTripAddress, ApplicationProfileConfigurationResolver.ShowBusinessTripAddress, application))
                    return Hidden(out error);
                return SetLookup<BusinessTripAddress>(objectSpace, value, item => application.BusinessTripAddress = item, out error);
            case Purpose:
                if (!Visible(profile, p => p.RequirePurpose, ApplicationProfileConfigurationResolver.ShowPurpose, application))
                    return Hidden(out error);
                return SetText(value, PurposeMaxLength, text => application.Purpose = text, out error);
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
                return SetWorkPermitLocation(value, application, out error);
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

    private static void AddText(
        List<ApplicationWorkspaceCaseHeaderField> fields,
        string key,
        string label,
        string tone,
        string glyph,
        bool visible,
        string? text)
    {
        if (!visible)
            return;

        var value = text?.Trim() ?? string.Empty;
        fields.Add(new ApplicationWorkspaceCaseHeaderField
        {
            Key = key,
            Label = label,
            Kind = ApplicationWorkspaceCaseHeaderFieldKind.Text,
            Tone = tone,
            Glyph = glyph,
            Value = value,
            DisplayValue = string.IsNullOrWhiteSpace(value) ? "—" : value,
            ReadOnly = false,
        });
    }

    private static void AddShortText(
        List<ApplicationWorkspaceCaseHeaderField> fields,
        string key,
        string label,
        string tone,
        string glyph,
        bool visible,
        string? text,
        int maxLength)
    {
        if (!visible)
            return;

        var value = text?.Trim() ?? string.Empty;
        fields.Add(new ApplicationWorkspaceCaseHeaderField
        {
            Key = key,
            Label = label,
            Kind = ApplicationWorkspaceCaseHeaderFieldKind.ShortText,
            Tone = tone,
            Glyph = glyph,
            Value = value,
            DisplayValue = string.IsNullOrWhiteSpace(value) ? "—" : value,
            ReadOnly = false,
            MaxLength = maxLength,
        });
    }

    private static void AddCommaSeparatedMultiSelect(
        List<ApplicationWorkspaceCaseHeaderField> fields,
        string key,
        string label,
        string tone,
        string glyph,
        bool visible,
        string? storedValue,
        string displayValue,
        IReadOnlyList<string> catalogOptions,
        bool readOnly)
    {
        if (!visible)
            return;

        fields.Add(new ApplicationWorkspaceCaseHeaderField
        {
            Key = key,
            Label = label,
            Kind = ApplicationWorkspaceCaseHeaderFieldKind.CommaSeparatedMultiSelect,
            Tone = tone,
            Glyph = glyph,
            Value = storedValue?.Trim() ?? string.Empty,
            DisplayValue = string.IsNullOrWhiteSpace(displayValue) ? "—" : displayValue,
            MultiSelectOptions = catalogOptions,
            ReadOnly = readOnly,
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

    private static bool SetBorderZone(string? value, ApplicationProfileInstance application, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            application.BorderZoneLocation = null;
            return true;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > 500)
        {
            error = "Border zone selection is too long.";
            return false;
        }

        application.BorderZoneLocation = trimmed;
        return true;
    }

    private static bool SetWorkPermitLocation(string? value, ApplicationProfileInstance application, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            application.MovementPermitLocation = null;
            return true;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > 500)
        {
            error = "Work permit location selection is too long.";
            return false;
        }

        application.MovementPermitLocation = trimmed;
        return true;
    }

    private static string FormatBorderZoneDisplay(string? preferred, string? fallback)
    {
        if (!string.IsNullOrWhiteSpace(preferred) && !BorderZoneSelectionHelper.IsNoneValue(preferred))
            return preferred.Trim();
        if (!string.IsNullOrWhiteSpace(fallback) && !BorderZoneSelectionHelper.IsNoneValue(fallback))
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

    private static bool SetText(string? value, int maxLength, Action<string?> assign, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            assign(null);
            return true;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            error = $"Text cannot exceed {maxLength} characters.";
            return false;
        }

        assign(trimmed);
        return true;
    }

    internal static bool TrySetInstanceNumber(ApplicationProfileInstance application, string? value, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            error = "Enter an application number.";
            return false;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > InstanceNumberMaxLength)
        {
            error = $"Application number cannot exceed {InstanceNumberMaxLength} characters.";
            return false;
        }

        ApplicationManualNumberParser.Parse(trimmed, out var full, out var prefix, out var number);
        application.FullApplicationNumber = full;
        if (!string.IsNullOrEmpty(prefix))
            application.AppNumberPrefix = prefix;
        if (!string.IsNullOrEmpty(number))
        {
            if (number.Length > 50)
            {
                error = "Application number sequence cannot exceed 50 characters.";
                return false;
            }

            application.ApplicationNumber = number;
        }
        else
        {
            if (full.Length > 50)
            {
                error = "Application number cannot exceed 50 characters.";
                return false;
            }

            application.ApplicationNumber = full;
        }

        application.IsManualEntry = true;
        return true;
    }

    internal static bool TrySetInstanceDate(ApplicationProfileInstance application, string? value, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            error = "Enter an application date.";
            return false;
        }

        if (!SetDate(value, date =>
            {
                if (date == null)
                    return;
                application.ApplicationDate = date.Value;
                application.Year = date.Value.Year;
                application.Month = date.Value.Month;
            }, out error))
            return false;

        if (application.ApplicationDate == default)
        {
            error = "Enter an application date.";
            return false;
        }

        return true;
    }

    private static string FormatInstanceNumber(ApplicationProfileInstance application)
    {
        if (!string.IsNullOrWhiteSpace(application.FullApplicationNumber))
            return application.FullApplicationNumber.Trim();
        if (!string.IsNullOrWhiteSpace(application.ApplicationNumber))
            return application.ApplicationNumber.Trim();
        return string.Empty;
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

    private static string FormatBusinessTripAddress(BusinessTripAddress? address)
    {
        if (address == null)
            return string.Empty;
        if (!string.IsNullOrWhiteSpace(address.FullAddress))
            return address.FullAddress.Trim();
        return LookupLabel(address.City);
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
        public IReadOnlyList<ApplicationWorkspaceLookupOption> BusinessTripAddresses { get; init; } = [];
        public IReadOnlyList<ApplicationWorkspaceLookupOption> CheckPoints { get; init; } = [];
        public IReadOnlyList<string> BorderZoneNames { get; init; } = [];
        public IReadOnlyList<string> WorkPermittedLocationNames { get; init; } = [];

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
            BusinessTripAddresses = LoadBusinessTripAddresses(objectSpace),
            CheckPoints = LoadItems<CheckPoint>(objectSpace),
            BorderZoneNames = CommaSeparatedCatalogHelper.LoadCatalogNames(
                objectSpace,
                typeof(BorderZoneName),
                BorderZoneSelectionHelper.NoneValue),
            WorkPermittedLocationNames = CommaSeparatedCatalogHelper.LoadCatalogNames(
                objectSpace,
                typeof(WorkPermittedLocationName),
                string.Empty),
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

        private static IReadOnlyList<ApplicationWorkspaceLookupOption> LoadBusinessTripAddresses(IObjectSpace objectSpace)
        {
            return objectSpace.GetObjects(typeof(BusinessTripAddress))
                .Cast<BusinessTripAddress>()
                .Select(item => new ApplicationWorkspaceLookupOption
                {
                    Id = item.ID,
                    DisplayName = FormatBusinessTripAddress(item),
                })
                .Where(item => !string.IsNullOrWhiteSpace(item.DisplayName))
                .OrderBy(item => item.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }
    }
}