using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Tenant lookup: migration-service SLA tier (max working days at
/// <c>PROCESS_STARTED</c> @ <c>AT_MIGRATION_SERVICE</c>). Assigned per <see cref="ApplicationType"/>.
/// </summary>
[DefaultClassOptions]
[DefaultProperty(nameof(NameTm))]
[NavigationItem("Lookup/Application/Config")]
[Appearance(
    "ApplicationMigrationSlaProfile_HideCatalogScalars",
    AppearanceItemType = "ViewItem",
    TargetItems = "Code;IsDefault",
    Visibility = ViewItemVisibility.Hide,
    Context = "DetailView,ListView,LookupListView")]
[RuleCriteria(
    "ApplicationMigrationSlaProfileWarningBeforeMax",
    DefaultContexts.Save,
    "WarningDaysBeforeMax is null OR MaxDaysInReview is null OR WarningDaysBeforeMax < MaxDaysInReview",
    CustomMessageTemplate = "Warning days must be less than the maximum review days.")]
public class ApplicationMigrationSlaProfile : LookupBase
{
    public ApplicationMigrationSlaProfile()
    {
        ApplicationTypes = new ObservableCollection<ApplicationType>();
    }

    /// <summary>Max working days allowed at migration service for types using this profile.</summary>
    [XafDisplayName("Maks. iş günleri")]
    public virtual int? MaxDaysInReview { get; set; }

    /// <summary>Optional early warning when working days exceed this value (must be &lt; <see cref="MaxDaysInReview"/>).</summary>
    [XafDisplayName("Duýduryş (iş günleri)")]
    public virtual int? WarningDaysBeforeMax { get; set; }

    /// <summary>Application types that use this migration SLA tier (maintained via Link / Unlink).</summary>
    [XafDisplayName("Arza görnüşleri")]
    [InverseProperty(nameof(ApplicationType.MigrationSlaProfile))]
    [VisibleInListView(false)]
    [ModelDefault("AllowNew", "False")]
    [ModelDefault("AllowDelete", "False")]
    public virtual IList<ApplicationType> ApplicationTypes { get; set; }
}
