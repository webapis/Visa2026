using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.EFCore;
using DevExpress.Persistent.BaseImpl.EF;
using Microsoft.EntityFrameworkCore;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.DatabaseUpdate.LookupCatalogs;
using Visa2026.Module.Localization;
using Visa2026.Module.Services;
using Visa2026.Module.Services.ApplicationProfileWizard;

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
    public const string FromRegion = "FromRegion";
    public const string ToRegion = "ToRegion";
    public const string Region = "Region";
    public const string City = "City";
    public const string BusinessTripAddress = "BusinessTripAddress";
    public const string BusinessTripAddressType = "BusinessTripAddressType";
    public const string BusinessTripLodging = "BusinessTripLodging";
    public const string BusinessTripHotel = "BusinessTripHotel";
    public const string BusinessTripHospital = "BusinessTripHospital";
    public const string BusinessTripOtherSite = "BusinessTripOtherSite";
    public const string BusinessTripPrivateHouseAddress = "BusinessTripPrivateHouseAddress";
    public const int BusinessTripPrivateHouseAddressMaxLength = 255;
    public const string Purpose = "Purpose";
    public const int PurposeMaxLength = 700;
    public const string Project = "Project";
    public const string Urgency = "Urgency";
    public const string WorkPermitLocation = "WorkPermitLocation";
    public const string EntryCheckPoint = "EntryCheckPoint";
    public const string InstanceNumber = "InstanceNumber";
    public const string InstanceDate = "InstanceDate";
    public const string ProcessNumber = "ProcessNumber";
    public const int InstanceNumberMaxLength = 100;

    /// <summary>Case summary right-frame fields: number, date, process number, urgency (when shown).</summary>
    public static readonly string[] IdentityFieldKeys =
    [
        InstanceNumber,
        InstanceDate,
        ProcessNumber,
        Urgency,
    ];

    public static bool IsIdentityField(string? key) =>
        !string.IsNullOrEmpty(key)
        && (string.Equals(key, InstanceNumber, StringComparison.Ordinal)
            || string.Equals(key, InstanceDate, StringComparison.Ordinal)
            || string.Equals(key, ProcessNumber, StringComparison.Ordinal)
            || string.Equals(key, Urgency, StringComparison.Ordinal));

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
        var instanceNumber = FormatInstanceNumber(application);
        var instanceDate = application.ApplicationDate == default ? (DateTime?)null : application.ApplicationDate;

        AddShortText(fields, InstanceNumber, "Application number", "blue", "№",
            visible: true,
            instanceNumber,
            InstanceNumberMaxLength,
            ApplicationWorkspaceCaseSummaryFill.Resolve(
                string.IsNullOrWhiteSpace(instanceNumber),
                !application.IsManualEntry));

        AddDate(fields, InstanceDate, "Application date", "green", "📅",
            visible: true,
            instanceDate,
            ApplicationWorkspaceCaseSummaryFill.Resolve(
                instanceDate == null,
                !application.IsManualEntry));

        var processNumber = application.ProcessNumber?.Trim() ?? string.Empty;
        AddShortText(fields, ProcessNumber, "Process number", "teal", "№",
            ApplicationProfileConfigurationResolver.ShowProcessNumber(application),
            processNumber,
            ApplicationProcessNumberHelper.MaxLength,
            ProcessNumberFill(application, processNumber));

        AddLookup(fields, VisaType, "Visa type", "blue", "🛂",
            Visible(profile, p => p.RequireVisaType, ApplicationProfileConfigurationResolver.ShowVisaType, application),
            application.VisaType?.ID, LookupLabel(application.VisaType), catalogs.VisaTypes, readOnly: false,
            LookupFill(application.VisaType?.ID, DefaultId(profile?.DefaultVisaType?.ID, profile?.DefaultVisaTypeId)));

        AddLookup(fields, VisaCategory, "Category", "purple", "◆",
            Visible(profile, p => p.RequireVisaCategory, ApplicationProfileConfigurationResolver.ShowVisaCategory, application),
            application.VisaCategory?.ID, LookupLabel(application.VisaCategory), catalogs.VisaCategories, readOnly: false,
            LookupFill(application.VisaCategory?.ID, DefaultId(profile?.DefaultVisaCategory?.ID, profile?.DefaultVisaCategoryId)));

        AddLookup(fields, VisaPeriod, "Period", "green", "📅",
            Visible(profile, p => p.RequireVisaPeriod, ApplicationProfileConfigurationResolver.ShowVisaPeriod, application),
            application.VisaPeriod?.ID, LookupLabel(application.VisaPeriod), catalogs.VisaPeriods, readOnly: false,
            LookupFill(application.VisaPeriod?.ID, DefaultId(profile?.DefaultVisaPeriod?.ID, profile?.DefaultVisaPeriodId)));

        AddLookup(fields, Project, "Project", "orange", "💼",
            Visible(profile, p => p.RequireProject, ApplicationProfileConfigurationResolver.ShowProjectContract, application),
            application.ProjectContract?.ID, LookupLabel(application.ProjectContract), catalogs.ProjectContracts, readOnly: false,
            LookupFill(application.ProjectContract?.ID, DefaultId(profile?.DefaultProjectContract?.ID, profile?.DefaultProjectContractId)));

        AddDate(fields, StartDate, "Start date", "green", "📅",
            Visible(profile, p => p.RequireStartDate, ApplicationProfileConfigurationResolver.ShowBusinessTrips, application),
            application.BusinessTripStartDate,
            DateFill(application.BusinessTripStartDate, defaultDate: null));

        AddLookup(fields, EntryCheckPoint, "Entry check point", "blue", "📍",
            Visible(profile, p => p.RequireEntryCheckPoint, ApplicationProfileConfigurationResolver.ShowEntryCheckPoint, application),
            application.EntryCheckPoint?.ID, LookupLabel(application.EntryCheckPoint), catalogs.CheckPoints, readOnly: false,
            LookupFill(application.EntryCheckPoint?.ID, DefaultId(profile?.DefaultEntryCheckPoint?.ID, profile?.DefaultEntryCheckPointId)));

        AddLookup(fields, Urgency, "Urgency", "orange", "⚡",
            Visible(profile, p => p.RequireUrgency, ApplicationProfileConfigurationResolver.ShowUrgency, application),
            application.Urgency?.ID, LookupLabel(application.Urgency), catalogs.Urgencies, readOnly: false,
            LookupFill(application.Urgency?.ID, DefaultId(profile?.DefaultUrgency?.ID, profile?.DefaultUrgencyId)));

        AddCommaSeparatedMultiSelect(fields, BorderZone, "Border zone", "teal", "📍",
            Visible(profile, p => ApplicationProfileConfigurationResolver.RequireBorderZoneWhenProducingInvitationOrVisa(
                    p.ProduceInvitation, p.ProduceVisa, p.RequireBorderZone),
                ApplicationProfileConfigurationResolver.ShowBorderZoneLocation, application),
            application.BorderZoneLocation,
            FormatBorderZoneDisplay(application.BorderZoneLocation_NameTm, application.BorderZoneLocation),
            catalogs.BorderZoneNames, readOnly: false,
            BorderZoneFill(application.BorderZoneLocation, profile?.DefaultBorderZoneLocation));

        AddDate(fields, EndDate, "End date", "green", "📅",
            Visible(profile, p => p.RequireEndDate, ApplicationProfileConfigurationResolver.ShowBusinessTrips, application),
            application.BusinessTripEndDate,
            DateFill(application.BusinessTripEndDate, defaultDate: null));

        AddLookup(fields, MigrationService, "Migration service", "teal", "🏛",
            Visible(profile, p => p.RequireMigrationService, ApplicationProfileConfigurationResolver.ShowMigrationService, application),
            application.MigrationService?.ID, LookupLabel(application.MigrationService), catalogs.MigrationServices, readOnly: false,
            LookupFill(application.MigrationService?.ID, DefaultId(profile?.DefaultMigrationService?.ID, profile?.DefaultMigrationServiceId)));

        AddLookup(fields, FromRegion, "From region", "purple", "📍",
            Visible(profile, p => p.RequireFromRegion, ApplicationProfileConfigurationResolver.ShowFromRegion, application),
            application.FromRegion?.ID, LookupLabel(application.FromRegion), catalogs.Regions, readOnly: false,
            FromGeoLookupFill(
                application,
                application.FromRegion?.ID,
                DefaultId(profile?.DefaultFromRegion?.ID, profile?.DefaultFromRegionId)));

        AddLookup(fields, FromCity, "From city", "purple", "📍",
            Visible(profile, p => p.RequireFromCity, ApplicationProfileConfigurationResolver.ShowFromCity, application),
            application.FromCity?.ID, LookupLabel(application.FromCity),
            CitiesForSelectedRegion(catalogs.Cities, catalogs.RegionCatalog, application.FromRegion?.ID),
            readOnly: false,
            FromGeoLookupFill(
                application,
                application.FromCity?.ID,
                DefaultId(profile?.DefaultFromCity?.ID, profile?.DefaultFromCityId)));

        AddLookup(fields, ToRegion, "To region", "purple", "📍",
            Visible(profile, p => p.RequireToRegion, ApplicationProfileConfigurationResolver.ShowToRegion, application),
            application.ToRegion?.ID, LookupLabel(application.ToRegion), catalogs.Regions, readOnly: false,
            LookupFill(application.ToRegion?.ID, DefaultId(profile?.DefaultToRegion?.ID, profile?.DefaultToRegionId)));

        AddLookup(fields, ToCity, "To city", "purple", "📍",
            Visible(profile, p => p.RequireToCity, ApplicationProfileConfigurationResolver.ShowToCity, application),
            application.ToCity?.ID, LookupLabel(application.ToCity),
            CitiesForSelectedRegion(catalogs.Cities, catalogs.RegionCatalog, application.ToRegion?.ID),
            readOnly: false,
            LookupFill(application.ToCity?.ID, DefaultId(profile?.DefaultToCity?.ID, profile?.DefaultToCityId)));

        var destinationCityId = application.ToCity?.ID
#pragma warning disable CS0618
            ?? application.City?.ID;
#pragma warning restore CS0618

        var showTripAddress = Visible(profile, p => p.RequireBusinessTripAddress, ApplicationProfileConfigurationResolver.ShowBusinessTripAddress, application);
        if (showTripAddress)
        {
            AddLookup(fields, BusinessTripAddressType, "Business trip address type", "purple", "📍",
                visible: true,
                application.BusinessTripAddressType is ResidenceType type
                    ? ResidenceTypeOptionId(type)
                    : null,
                application.BusinessTripAddressType?.ToString() ?? string.Empty,
                catalogs.ResidenceTypes,
                readOnly: false,
                LookupFill(
                    application.BusinessTripAddressType is ResidenceType t ? ResidenceTypeOptionId(t) : null,
                    profile?.DefaultBusinessTripAddressType is ResidenceType dt ? ResidenceTypeOptionId(dt) : null));

            switch (application.BusinessTripAddressType)
            {
                case ResidenceType.Lodging:
                    AddLookup(fields, BusinessTripLodging, "Business trip lodging", "purple", "📍",
                        visible: true,
                        application.BusinessTripLodging?.ID,
                        application.BusinessTripLodging?.FullAddress ?? string.Empty,
                        FilterSitesByCity(
                            catalogs.Lodgings,
                            destinationCityId,
                            application.ToCity?.NameTm),
                        readOnly: false,
                        LookupFill(application.BusinessTripLodging?.ID, DefaultId(profile?.DefaultBusinessTripLodging?.ID, profile?.DefaultBusinessTripLodgingId)));
                    break;
                case ResidenceType.Hotel:
                    AddLookup(fields, BusinessTripHotel, "Business trip hotel", "purple", "📍",
                        visible: true,
                        application.BusinessTripHotel?.ID,
                        application.BusinessTripHotel?.Name ?? string.Empty,
                        FilterSitesByCity(
                            catalogs.Hotels,
                            destinationCityId,
                            application.ToCity?.NameTm),
                        readOnly: false,
                        LookupFill(application.BusinessTripHotel?.ID, DefaultId(profile?.DefaultBusinessTripHotel?.ID, profile?.DefaultBusinessTripHotelId)));
                    break;
                case ResidenceType.Hospital:
                    AddLookup(fields, BusinessTripHospital, "Business trip hospital", "purple", "📍",
                        visible: true,
                        application.BusinessTripHospital?.ID,
                        application.BusinessTripHospital?.Name ?? string.Empty,
                        FilterSitesByCity(
                            catalogs.Hospitals,
                            destinationCityId,
                            application.ToCity?.NameTm),
                        readOnly: false,
                        LookupFill(application.BusinessTripHospital?.ID, DefaultId(profile?.DefaultBusinessTripHospital?.ID, profile?.DefaultBusinessTripHospitalId)));
                    break;
                case ResidenceType.Other:
                    AddLookup(fields, BusinessTripOtherSite, "Business trip other site", "purple", "📍",
                        visible: true,
                        application.BusinessTripOtherSite?.ID,
                        application.BusinessTripOtherSite?.FullAddress ?? string.Empty,
                        FilterSitesByCity(
                            catalogs.OtherSites,
                            destinationCityId,
                            application.ToCity?.NameTm),
                        readOnly: false,
                        LookupFill(application.BusinessTripOtherSite?.ID, DefaultId(profile?.DefaultBusinessTripOtherSite?.ID, profile?.DefaultBusinessTripOtherSiteId)));
                    break;
                case ResidenceType.PrivateHouse:
                    AddShortText(fields, BusinessTripPrivateHouseAddress, "Business trip address", "purple", "📍",
                        visible: true,
                        application.BusinessTripPrivateHouseAddress,
                        BusinessTripPrivateHouseAddressMaxLength,
                        TextFill(application.BusinessTripPrivateHouseAddress, profile?.DefaultBusinessTripPrivateHouseAddress));
                    break;
                default:
#pragma warning disable CS0618
                    // Dual-read legacy catalog until officers re-pick Type + site.
                    if (application.BusinessTripAddress != null)
                    {
                        AddLookup(fields, BusinessTripAddress, "Business trip address (legacy)", "purple", "📍",
                            visible: true,
                            application.BusinessTripAddress?.ID,
                            FormatBusinessTripAddress(application.BusinessTripAddress),
                            catalogs.BusinessTripAddresses, readOnly: true,
                            LookupFill(application.BusinessTripAddress?.ID, null));
                    }
#pragma warning restore CS0618
                    break;
            }
        }

        AddText(fields, Purpose, "Purpose", "blue", "📝",
            Visible(profile, p => p.RequirePurpose, ApplicationProfileConfigurationResolver.ShowPurpose, application),
            application.Purpose,
            TextFill(application.Purpose, profile?.DefaultPurpose));

        AddCommaSeparatedMultiSelect(fields, WorkPermitLocation, "Work permit location", "blue", "🏢",
            Visible(profile, p => ApplicationProfileConfigurationResolver.RequireWorkPermitLocationWhenProducingWorkPermit(
                    p.ProduceWorkPermit, p.RequireWorkPermitLocation),
                ApplicationProfileConfigurationResolver.ShowMovementPermitLocation, application),
            application.MovementPermitLocation,
            application.MovementPermitLocation_NameTm,
            catalogs.WorkPermittedLocationNames, readOnly: false,
            MultiSelectFill(application.MovementPermitLocation, profile?.DefaultWorkPermitLocation));

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
            case ProcessNumber:
                if (!ApplicationProfileConfigurationResolver.ShowProcessNumber(application))
                    return Hidden(out error);
                return ApplicationProcessNumberHelper.TryAssign(
                    objectSpace,
                    application,
                    value,
                    requireWhenVisible: ApplicationProcessNumberHelper.HasProcessStartedStep(application),
                    out error);
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
                if (!Visible(profile, p => ApplicationProfileConfigurationResolver.RequireBorderZoneWhenProducingInvitationOrVisa(
                        p.ProduceInvitation, p.ProduceVisa, p.RequireBorderZone),
                    ApplicationProfileConfigurationResolver.ShowBorderZoneLocation, application))
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
            case FromRegion:
                if (!Visible(profile, p => p.RequireFromRegion, ApplicationProfileConfigurationResolver.ShowFromRegion, application))
                    return Hidden(out error);
                return SetLookup<Region>(objectSpace, value, item =>
                {
                    application.FromRegion = item;
                    if (application.FromCity?.Region != null && item != null && application.FromCity.Region.ID != item.ID)
                        application.FromCity = null;
                }, out error);
            case FromCity:
                if (!Visible(profile, p => p.RequireFromCity, ApplicationProfileConfigurationResolver.ShowFromCity, application))
                    return Hidden(out error);
                return SetLookup<City>(objectSpace, value, item =>
                {
                    application.FromCity = item;
                    var region = ResolveCityRegion(objectSpace, item);
                    if (region != null)
                        application.FromRegion = region;
                }, out error);
            case ToRegion:
                if (!Visible(profile, p => p.RequireToRegion, ApplicationProfileConfigurationResolver.ShowToRegion, application))
                    return Hidden(out error);
                return SetLookup<Region>(objectSpace, value, item =>
                {
                    application.ToRegion = item;
                    if (application.ToCity?.Region != null && item != null && application.ToCity.Region.ID != item.ID)
                        application.ToCity = null;
                }, out error);
            case ToCity:
                if (!Visible(profile, p => p.RequireToCity, ApplicationProfileConfigurationResolver.ShowToCity, application))
                    return Hidden(out error);
                return SetLookup<City>(objectSpace, value, item =>
                {
                    application.ToCity = item;
                    if (item?.Region != null)
                        application.ToRegion = item.Region;
                    ClearTripSitesIfCityMismatch(application, item);
                }, out error);
            case BusinessTripAddressType:
                if (!Visible(profile, p => p.RequireBusinessTripAddress, ApplicationProfileConfigurationResolver.ShowBusinessTripAddress, application))
                    return Hidden(out error);
                return SetBusinessTripAddressType(application, value, out error);
            case BusinessTripLodging:
                if (!Visible(profile, p => p.RequireBusinessTripAddress, ApplicationProfileConfigurationResolver.ShowBusinessTripAddress, application)
                    || application.BusinessTripAddressType != ResidenceType.Lodging)
                    return Hidden(out error);
                return SetLookup<Lodging>(objectSpace, value, item =>
                {
                    application.BusinessTripAddressType = ResidenceType.Lodging;
                    BusinessTripDestinationHelper.ClearSitesExcept(application, ResidenceType.Lodging);
                    application.BusinessTripLodging = item;
#pragma warning disable CS0618
                    application.BusinessTripAddress = null;
#pragma warning restore CS0618
                }, out error);
            case BusinessTripHotel:
                if (!Visible(profile, p => p.RequireBusinessTripAddress, ApplicationProfileConfigurationResolver.ShowBusinessTripAddress, application)
                    || application.BusinessTripAddressType != ResidenceType.Hotel)
                    return Hidden(out error);
                return SetLookup<Hotel>(objectSpace, value, item =>
                {
                    application.BusinessTripAddressType = ResidenceType.Hotel;
                    BusinessTripDestinationHelper.ClearSitesExcept(application, ResidenceType.Hotel);
                    application.BusinessTripHotel = item;
#pragma warning disable CS0618
                    application.BusinessTripAddress = null;
#pragma warning restore CS0618
                }, out error);
            case BusinessTripHospital:
                if (!Visible(profile, p => p.RequireBusinessTripAddress, ApplicationProfileConfigurationResolver.ShowBusinessTripAddress, application)
                    || application.BusinessTripAddressType != ResidenceType.Hospital)
                    return Hidden(out error);
                return SetLookup<Hospital>(objectSpace, value, item =>
                {
                    application.BusinessTripAddressType = ResidenceType.Hospital;
                    BusinessTripDestinationHelper.ClearSitesExcept(application, ResidenceType.Hospital);
                    application.BusinessTripHospital = item;
#pragma warning disable CS0618
                    application.BusinessTripAddress = null;
#pragma warning restore CS0618
                }, out error);
            case BusinessTripOtherSite:
                if (!Visible(profile, p => p.RequireBusinessTripAddress, ApplicationProfileConfigurationResolver.ShowBusinessTripAddress, application)
                    || application.BusinessTripAddressType != ResidenceType.Other)
                    return Hidden(out error);
                return SetLookup<OtherSite>(objectSpace, value, item =>
                {
                    application.BusinessTripAddressType = ResidenceType.Other;
                    BusinessTripDestinationHelper.ClearSitesExcept(application, ResidenceType.Other);
                    application.BusinessTripOtherSite = item;
#pragma warning disable CS0618
                    application.BusinessTripAddress = null;
#pragma warning restore CS0618
                }, out error);
            case BusinessTripPrivateHouseAddress:
                if (!Visible(profile, p => p.RequireBusinessTripAddress, ApplicationProfileConfigurationResolver.ShowBusinessTripAddress, application)
                    || application.BusinessTripAddressType != ResidenceType.PrivateHouse)
                    return Hidden(out error);
                return SetText(value, BusinessTripPrivateHouseAddressMaxLength, text =>
                {
                    application.BusinessTripAddressType = ResidenceType.PrivateHouse;
                    BusinessTripDestinationHelper.ClearSitesExcept(application, ResidenceType.PrivateHouse);
                    application.BusinessTripPrivateHouseAddress = text;
#pragma warning disable CS0618
                    application.BusinessTripAddress = null;
#pragma warning restore CS0618
                }, out error);
#pragma warning disable CS0618
            case BusinessTripAddress:
                if (!Visible(profile, p => p.RequireBusinessTripAddress, ApplicationProfileConfigurationResolver.ShowBusinessTripAddress, application))
                    return Hidden(out error);
                error = "Pick address type and a lodging/hotel/hospital/other site instead of the legacy catalog.";
                return false;
#pragma warning restore CS0618
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
                if (!Visible(profile, p => ApplicationProfileConfigurationResolver.RequireWorkPermitLocationWhenProducingWorkPermit(
                        p.ProduceWorkPermit, p.RequireWorkPermitLocation),
                    ApplicationProfileConfigurationResolver.ShowMovementPermitLocation, application))
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
        bool readOnly,
        ApplicationWorkspaceCaseSummaryFillState fillState)
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
            FillState = fillState,
        });
    }

    private static void AddDate(
        List<ApplicationWorkspaceCaseHeaderField> fields,
        string key,
        string label,
        string tone,
        string glyph,
        bool visible,
        DateTime? date,
        ApplicationWorkspaceCaseSummaryFillState fillState)
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
            FillState = fillState,
        });
    }

    private static void AddText(
        List<ApplicationWorkspaceCaseHeaderField> fields,
        string key,
        string label,
        string tone,
        string glyph,
        bool visible,
        string? text,
        ApplicationWorkspaceCaseSummaryFillState fillState)
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
            FillState = fillState,
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
        int maxLength,
        ApplicationWorkspaceCaseSummaryFillState fillState)
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
            FillState = fillState,
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
        bool readOnly,
        ApplicationWorkspaceCaseSummaryFillState fillState)
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
            FillState = fillState,
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

    private static Region? ResolveCityRegion(IObjectSpace objectSpace, City? city)
    {
        if (city == null)
            return null;
        if (city.Region != null)
            return city.Region;

        try
        {
            objectSpace.ReloadObject(city);
        }
        catch (Exception)
        {
            return city.Region;
        }

        return city.Region;
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
        return BorderZoneSelectionHelper.NoneValue;
    }

    private static ApplicationWorkspaceCaseSummaryFillState BorderZoneFill(string? stored, string? defaultStored)
    {
        var storedNone = string.IsNullOrWhiteSpace(stored) || BorderZoneSelectionHelper.IsNoneValue(stored);
        var defaultNone = string.IsNullOrWhiteSpace(defaultStored) || BorderZoneSelectionHelper.IsNoneValue(defaultStored);
        if (storedNone)
        {
            return defaultNone
                ? ApplicationWorkspaceCaseSummaryFillState.Default
                : ApplicationWorkspaceCaseSummaryFillState.Empty;
        }

        return MultiSelectFill(stored, defaultStored);
    }

    private static ApplicationWorkspaceCaseSummaryFillState ProcessNumberFill(
        ApplicationProfileInstance application,
        string processNumber)
    {
        if (!string.IsNullOrWhiteSpace(processNumber))
            return ApplicationWorkspaceCaseSummaryFillState.Officer;

        return ApplicationProcessNumberHelper.HasProcessStartedStep(application)
            ? ApplicationWorkspaceCaseSummaryFillState.Empty
            : ApplicationWorkspaceCaseSummaryFillState.Default;
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

        application.IsManualEntry = true;
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

    private static ApplicationWorkspaceCaseSummaryFillState LookupFill(Guid? selectedId, Guid? defaultId)
    {
        var empty = selectedId == null || selectedId == Guid.Empty;
        var matches = !empty
            && defaultId is Guid id
            && id != Guid.Empty
            && selectedId == id;
        return ApplicationWorkspaceCaseSummaryFill.Resolve(empty, matches);
    }

    /// <summary>
    /// VISA2014 (and other IsManualEntry) rows have no origin geography. Empty From region/city
    /// must not count as Case summary readiness gaps or paint red tiles; officer-created
    /// (auto-numbered) instances still treat empty From* as required.
    /// </summary>
    private static ApplicationWorkspaceCaseSummaryFillState FromGeoLookupFill(
        ApplicationProfileInstance application,
        Guid? selectedId,
        Guid? defaultId)
    {
        var empty = selectedId == null || selectedId == Guid.Empty;
        if (empty && application.IsManualEntry)
            return ApplicationWorkspaceCaseSummaryFillState.Default;

        return LookupFill(selectedId, defaultId);
    }

    private static ApplicationWorkspaceCaseSummaryFillState DateFill(DateTime? date, DateTime? defaultDate)
    {
        var empty = date == null || date.Value == default;
        var matches = !empty
            && defaultDate is DateTime expected
            && expected != default
            && date.Value.Date == expected.Date;
        return ApplicationWorkspaceCaseSummaryFill.Resolve(empty, matches);
    }

    private static ApplicationWorkspaceCaseSummaryFillState TextFill(string? value, string? defaultValue)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        var defaultTrimmed = defaultValue?.Trim() ?? string.Empty;
        return ApplicationWorkspaceCaseSummaryFill.Resolve(
            string.IsNullOrWhiteSpace(trimmed),
            !string.IsNullOrWhiteSpace(defaultTrimmed)
                && string.Equals(trimmed, defaultTrimmed, StringComparison.Ordinal));
    }

    private static ApplicationWorkspaceCaseSummaryFillState MultiSelectFill(string? stored, string? defaultStored)
    {
        var selected = NormalizeMultiSelect(stored);
        var defaults = NormalizeMultiSelect(defaultStored);
        return ApplicationWorkspaceCaseSummaryFill.Resolve(
            selected.Count == 0,
            selected.Count > 0
                && defaults.Count == selected.Count
                && selected.SequenceEqual(defaults, StringComparer.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<string> NormalizeMultiSelect(string? stored)
    {
        if (string.IsNullOrWhiteSpace(stored) || CommaSeparatedSelectionHelper.IsNoneValue(stored))
            return [];

        return CommaSeparatedSelectionHelper.ParseSelected(stored)
            .Select(item => item.Trim())
            .Where(item => item.Length > 0)
            .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static Guid? DefaultId(Guid? navigationId, Guid? foreignKey)
    {
        if (navigationId is Guid id && id != Guid.Empty)
            return id;
        if (foreignKey is Guid fk && fk != Guid.Empty)
            return fk;
        return null;
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

    private static Guid ResidenceTypeOptionId(ResidenceType type) =>
        // Must not use 0-only Guid for Lodging (enum 0) — LookupFill treats Guid.Empty as empty.
        Guid.Parse($"00000000-0000-0000-b7a1-{(int)type:D12}");

    private static bool TryParseResidenceTypeOptionId(string? value, out ResidenceType type)
    {
        type = default;
        if (!Guid.TryParse(value, out var id))
            return false;
        foreach (ResidenceType candidate in Enum.GetValues(typeof(ResidenceType)))
        {
            if (ResidenceTypeOptionId(candidate) == id)
            {
                type = candidate;
                return true;
            }
        }

        return false;
    }

    private static bool SetBusinessTripAddressType(
        ApplicationProfileInstance application,
        string? value,
        out string? error)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            application.BusinessTripAddressType = null;
            BusinessTripDestinationHelper.ClearSitesExcept(application, keep: null);
#pragma warning disable CS0618
            application.BusinessTripAddress = null;
#pragma warning restore CS0618
            error = null;
            return true;
        }

        if (!TryParseResidenceTypeOptionId(value, out var type))
        {
            error = "Choose a valid business trip address type.";
            return false;
        }

        application.BusinessTripAddressType = type;
        BusinessTripDestinationHelper.ClearSitesExcept(application, type);
#pragma warning disable CS0618
        application.BusinessTripAddress = null;
#pragma warning restore CS0618
        error = null;
        return true;
    }

    internal static IReadOnlyList<ApplicationWorkspaceLookupOption> CitiesForSelectedRegion(
        IReadOnlyList<ApplicationProfileWizardLookupItem> cities,
        IReadOnlyList<ApplicationProfileWizardLookupItem> regions,
        Guid? regionId)
    {
        if (cities == null || cities.Count == 0)
            return Array.Empty<ApplicationWorkspaceLookupOption>();

        return ApplicationProfileWizardLookupData.CitiesForRegion(cities, regions, regionId)
            .Select(item => new ApplicationWorkspaceLookupOption
            {
                Id = item.Id,
                DisplayName = item.DisplayName,
            })
            .ToList();
    }

    internal static IReadOnlyList<ApplicationWorkspaceLookupOption> FilterSitesByCity(
        IReadOnlyList<SiteCatalogOption> sites,
        Guid? cityId,
        string? cityName = null)
    {
        IEnumerable<SiteCatalogOption> query = sites;
        var hasCityId = cityId is Guid id && id != Guid.Empty;
        var hasCityName = !string.IsNullOrWhiteSpace(cityName);
        if (hasCityId || hasCityName)
        {
            query = sites.Where(site =>
                (hasCityId && site.CityId == cityId)
                || (hasCityName && LookupCatalogMatchHelper.KeysEqual(site.CityName, cityName)));
        }

        return query
            .Select(s => new ApplicationWorkspaceLookupOption { Id = s.Id, DisplayName = s.DisplayName })
            .Where(s => !string.IsNullOrWhiteSpace(s.DisplayName))
            .OrderBy(s => s.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    internal sealed record SiteCatalogOption(Guid Id, string DisplayName, Guid? CityId, string? CityName = null);

    private static void ClearTripSitesIfCityMismatch(ApplicationProfileInstance application, City? toCity)
    {
        if (toCity == null)
            return;

        if (application.BusinessTripLodging?.City != null
            && application.BusinessTripLodging.City.ID != toCity.ID)
            application.BusinessTripLodging = null;
        if (application.BusinessTripHotel?.City != null
            && application.BusinessTripHotel.City.ID != toCity.ID)
            application.BusinessTripHotel = null;
        if (application.BusinessTripHospital?.City != null
            && application.BusinessTripHospital.City.ID != toCity.ID)
            application.BusinessTripHospital = null;
        if (application.BusinessTripOtherSite?.City != null
            && application.BusinessTripOtherSite.City.ID != toCity.ID)
            application.BusinessTripOtherSite = null;
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
        public IReadOnlyList<ApplicationProfileWizardLookupItem> Cities { get; init; } = [];
        public IReadOnlyList<ApplicationProfileWizardLookupItem> RegionCatalog { get; init; } = [];
        public IReadOnlyList<ApplicationWorkspaceLookupOption> Regions { get; init; } = [];
        public IReadOnlyList<ApplicationWorkspaceLookupOption> ResidenceTypes { get; init; } = [];
        public IReadOnlyList<SiteCatalogOption> Lodgings { get; init; } = [];
        public IReadOnlyList<SiteCatalogOption> Hotels { get; init; } = [];
        public IReadOnlyList<SiteCatalogOption> Hospitals { get; init; } = [];
        public IReadOnlyList<SiteCatalogOption> OtherSites { get; init; } = [];
        public IReadOnlyList<ApplicationWorkspaceLookupOption> BusinessTripAddresses { get; init; } = [];
        public IReadOnlyList<ApplicationWorkspaceLookupOption> CheckPoints { get; init; } = [];
        public IReadOnlyList<string> BorderZoneNames { get; init; } = [];
        public IReadOnlyList<string> WorkPermittedLocationNames { get; init; } = [];

        public static Catalogs Load(IObjectSpace objectSpace)
        {
            var regions = ApplicationProfileWizardLookupData.LoadRegions(objectSpace);
            return new()
            {
                VisaTypes = LoadItems<VisaType>(objectSpace),
                VisaCategories = LoadItems<VisaCategory>(objectSpace),
                VisaPeriods = LoadItems<VisaPeriod>(objectSpace),
                MigrationServices = LoadItems<MigrationService>(objectSpace),
                ProjectContracts = LoadItems<ProjectContract>(objectSpace),
                Urgencies = LoadItems<Urgency>(objectSpace),
                Cities = ApplicationProfileWizardLookupData.LoadCities(objectSpace),
                RegionCatalog = regions,
                Regions = ToLookupOptions(regions),
                ResidenceTypes = LoadResidenceTypes(),
                Lodgings = LoadLodgings(objectSpace),
                Hotels = LoadHotels(objectSpace),
                Hospitals = LoadHospitals(objectSpace),
                OtherSites = LoadOtherSites(objectSpace),
#pragma warning disable CS0618
                BusinessTripAddresses = LoadBusinessTripAddresses(objectSpace),
#pragma warning restore CS0618
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
        }

        private static IReadOnlyList<ApplicationWorkspaceLookupOption> ToLookupOptions(
            IReadOnlyList<ApplicationProfileWizardLookupItem> items) =>
            items.Select(item => new ApplicationWorkspaceLookupOption
            {
                Id = item.Id,
                DisplayName = item.DisplayName,
            }).ToList();

        private static IReadOnlyList<ApplicationWorkspaceLookupOption> LoadResidenceTypes() =>
            Enum.GetValues(typeof(ResidenceType))
                .Cast<ResidenceType>()
                .Select(t => new ApplicationWorkspaceLookupOption
                {
                    Id = ResidenceTypeOptionId(t),
                    DisplayName = t switch
                    {
                        ResidenceType.Lodging => "Lodging",
                        ResidenceType.Hotel => "Hotel",
                        ResidenceType.Hospital => "Hospital",
                        ResidenceType.Other => "Other site",
                        ResidenceType.PrivateHouse => "Private house",
                        _ => t.ToString(),
                    },
                })
                .ToList();

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

        private static IReadOnlyList<SiteCatalogOption> LoadLodgings(IObjectSpace objectSpace) =>
            LoadSites(
                QueryWithCity<Lodging>(objectSpace),
                item => item.FullAddress,
                item => item.City,
                "lodging.json",
                "FullAddress");

        private static IReadOnlyList<SiteCatalogOption> LoadHotels(IObjectSpace objectSpace) =>
            LoadSites(
                QueryWithCity<Hotel>(objectSpace),
                item => item.Name,
                item => item.City,
                "hotel.json",
                "Name");

        private static IReadOnlyList<SiteCatalogOption> LoadHospitals(IObjectSpace objectSpace) =>
            LoadSites(
                QueryWithCity<Hospital>(objectSpace),
                item => item.Name,
                item => item.City,
                "hospital.json",
                "Name");

        private static IReadOnlyList<SiteCatalogOption> LoadOtherSites(IObjectSpace objectSpace) =>
            LoadSites(
                QueryWithCity<OtherSite>(objectSpace),
                item => item.FullAddress,
                item => item.City,
                "other-site.json",
                "FullAddress");

        private static List<T> QueryWithCity<T>(IObjectSpace objectSpace)
            where T : class
        {
            if (objectSpace is EFCoreObjectSpace { DbContext: { } dbContext })
                return dbContext.Set<T>().Include("City").ToList();

            return objectSpace.GetObjects(typeof(T)).Cast<T>().ToList();
        }

        private static IReadOnlyList<SiteCatalogOption> LoadSites<T>(
            IEnumerable<T> items,
            Func<T, string?> title,
            Func<T, City?> city,
            string catalogFile,
            string catalogTitleKey)
            where T : BaseObject
        {
            var cityByTitle = LoadCatalogCityByTitle(catalogFile, catalogTitleKey);
            return items
                .Select(item =>
                {
                    var display = title(item)?.Trim() ?? string.Empty;
                    var cityName = city(item)?.NameTm
                        ?? (cityByTitle.TryGetValue(
                            LookupCatalogMatchHelper.NormalizeKey(display),
                            out var catalogCity)
                            ? catalogCity
                            : null);
                    return new SiteCatalogOption(
                        item.ID,
                        display,
                        city(item)?.ID,
                        cityName);
                })
                .Where(item => !string.IsNullOrWhiteSpace(item.DisplayName))
                .ToList();
        }

        private static IReadOnlyDictionary<string, string> LoadCatalogCityByTitle(
            string catalogFile,
            string titleKey)
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            var file = LookupCatalogResourceLoader.LoadCatalogFile(catalogFile);
            if (file?.Rows == null)
                return map;

            foreach (var row in file.Rows)
            {
                if (!TryReadCatalogString(row, titleKey, out var siteTitle)
                    && !(titleKey == "FullAddress" && TryReadCatalogString(row, "Name", out siteTitle)))
                    continue;
                if (!TryReadCatalogString(row, "City", out var cityName))
                    continue;

                var key = LookupCatalogMatchHelper.NormalizeKey(siteTitle);
                if (key.Length == 0)
                    continue;

                map[key] = cityName;
            }

            return map;
        }

        private static bool TryReadCatalogString(
            Dictionary<string, System.Text.Json.JsonElement> row,
            string key,
            out string value)
        {
            value = string.Empty;
            if (!row.TryGetValue(key, out var element))
                return false;

            var text = element.ValueKind == System.Text.Json.JsonValueKind.String
                ? element.GetString()
                : element.ToString();
            if (string.IsNullOrWhiteSpace(text))
                return false;

            value = text.Trim();
            return true;
        }

#pragma warning disable CS0618
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
#pragma warning restore CS0618
    }
}