using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.ApplicationProfileWizard;

/// <summary>Lookup catalogs for wizard step 2 default-value editors.</summary>
public sealed class ApplicationProfileWizardLookupData
{
    public IReadOnlyList<VisaType> VisaTypes { get; init; } = [];
    public IReadOnlyList<VisaCategory> VisaCategories { get; init; } = [];
    public IReadOnlyList<VisaPeriod> VisaPeriods { get; init; } = [];
    public IReadOnlyList<MigrationService> MigrationServices { get; init; } = [];
    public IReadOnlyList<ProjectContract> ProjectContracts { get; init; } = [];
    public IReadOnlyList<Urgency> Urgencies { get; init; } = [];
    public IReadOnlyList<CheckPoint> CheckPoints { get; init; } = [];
    public IReadOnlyList<AuthorizedSignatory> AuthorizedSignatories { get; init; } = [];
    public IReadOnlyList<AuthorizedRepresentative> VisaRepresentatives { get; init; } = [];

    public static ApplicationProfileWizardLookupData Load(IObjectSpace objectSpace)
    {
        if (objectSpace == null)
            return new ApplicationProfileWizardLookupData();

        return new ApplicationProfileWizardLookupData
        {
            VisaTypes = objectSpace.GetObjectsQuery<VisaType>().OrderBy(x => x.NameTm).ToList(),
            VisaCategories = objectSpace.GetObjectsQuery<VisaCategory>().OrderBy(x => x.NameTm).ToList(),
            VisaPeriods = objectSpace.GetObjectsQuery<VisaPeriod>().OrderBy(x => x.NameTm).ToList(),
            MigrationServices = objectSpace.GetObjectsQuery<MigrationService>().OrderBy(x => x.NameTm).ToList(),
            ProjectContracts = objectSpace.GetObjectsQuery<ProjectContract>().OrderBy(x => x.NameTm).ToList(),
            Urgencies = objectSpace.GetObjectsQuery<Urgency>().OrderBy(x => x.NameTm).ToList(),
            CheckPoints = objectSpace.GetObjectsQuery<CheckPoint>().OrderBy(x => x.NameTm).ToList(),
            AuthorizedSignatories = objectSpace.GetObjectsQuery<AuthorizedSignatory>().OrderBy(x => x.FullName).ToList(),
            VisaRepresentatives = objectSpace.GetObjectsQuery<AuthorizedRepresentative>().OrderBy(x => x.FullName).ToList(),
        };
    }
}
