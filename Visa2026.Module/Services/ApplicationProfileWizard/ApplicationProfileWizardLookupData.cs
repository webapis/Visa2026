using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;

namespace Visa2026.Module.Services.ApplicationProfileWizard;

/// <summary>One catalog row for wizard default-value dropdowns (not a live ObjectSpace entity).</summary>
public sealed class ApplicationProfileWizardLookupItem
{
    public Guid Id { get; init; }
    public string DisplayName { get; init; } = string.Empty;
}

/// <summary>Lookup catalogs for wizard Results and fields default-value editors.</summary>
public sealed class ApplicationProfileWizardLookupData
{
    public static ApplicationProfileWizardLookupData Empty { get; } = new();

    public IReadOnlyList<ApplicationProfileWizardLookupItem> VisaTypes { get; init; } = [];
    public IReadOnlyList<ApplicationProfileWizardLookupItem> VisaCategories { get; init; } = [];
    public IReadOnlyList<ApplicationProfileWizardLookupItem> VisaPeriods { get; init; } = [];
    public IReadOnlyList<ApplicationProfileWizardLookupItem> MigrationServices { get; init; } = [];
    public IReadOnlyList<ApplicationProfileWizardLookupItem> ProjectContracts { get; init; } = [];
    public IReadOnlyList<ApplicationProfileWizardLookupItem> Urgencies { get; init; } = [];
    public IReadOnlyList<ApplicationProfileWizardLookupItem> CheckPoints { get; init; } = [];

    public static ApplicationProfileWizardLookupData Load(IObjectSpace objectSpace)
    {
        if (objectSpace == null)
            return Empty;

        return new ApplicationProfileWizardLookupData
        {
            VisaTypes = LoadItems<VisaType>(objectSpace),
            VisaCategories = LoadItems<VisaCategory>(objectSpace),
            VisaPeriods = LoadItems<VisaPeriod>(objectSpace),
            MigrationServices = LoadItems<MigrationService>(objectSpace),
            ProjectContracts = LoadItems<ProjectContract>(objectSpace),
            Urgencies = LoadItems<Urgency>(objectSpace),
            CheckPoints = LoadItems<CheckPoint>(objectSpace),
        };
    }

    private static IReadOnlyList<ApplicationProfileWizardLookupItem> LoadItems<T>(IObjectSpace objectSpace)
        where T : LookupBase
    {
        return objectSpace.GetObjects(typeof(T))
            .Cast<T>()
            .Select(item => new ApplicationProfileWizardLookupItem
            {
                Id = item.ID,
                DisplayName = FormatDisplayName(item)
            })
            .OrderBy(item => item.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private static string FormatDisplayName(LookupBase item)
    {
        var localized = LookupLocalization.GetDisplayName(item);
        if (!string.IsNullOrWhiteSpace(localized))
            return localized;
        if (!string.IsNullOrWhiteSpace(item.NameTm))
            return item.NameTm;
#pragma warning disable CS0618
        if (!string.IsNullOrWhiteSpace(item.Name))
            return item.Name;
#pragma warning restore CS0618
        return string.IsNullOrWhiteSpace(item.Code) ? item.ID.ToString("D") : item.Code;
    }
}