using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Reusable ministry approval chain (ordered legs + SLA). Owns many <see cref="ProjectContract"/> rows;
/// selected on <see cref="Application"/> when <see cref="ApplicationType.ShowApprovalLegProfile"/> applies.
/// </summary>
[DefaultClassOptions]
[DefaultProperty(nameof(MinistriesLabel))]
[NavigationItem(false)]
[Appearance(
    "ApprovalLegProfile_HideCatalogScalars",
    AppearanceItemType = "ViewItem",
    TargetItems = "IsDefault;NameTm;LocalizationKey",
    Visibility = ViewItemVisibility.Hide,
    Context = "DetailView,ListView,LookupListView")]
public class ApprovalLegProfile : LookupBase
{
    public ApprovalLegProfile()
    {
        MinistryLegs = new ObservableCollection<ApprovalLegProfileMinistryLeg>();
        ProjectContracts = new ObservableCollection<ProjectContract>();
        ContractLinks = new ObservableCollection<ProjectContractApprovalLegProfile>();
    }

    public virtual bool IsActive { get; set; } = true;

    /// <summary>Ordered ministry short names, e.g. Türkmenenergo-Energetika-Gurluşyk.</summary>
    [NotMapped]
    [VisibleInDetailView(false)]
    [VisibleInListView(true)]
    [VisibleInLookupListView(true)]
    public string MinistriesLabel =>
        string.Join(
            "-",
            MinistryLegs?
                .Where(l => l.ApprovingMinistry != null && !string.IsNullOrWhiteSpace(l.ApprovingMinistry.ShortNameTm))
                .OrderBy(l => l.Sequence)
                .Select(l => l.ApprovingMinistry.ShortNameTm.Trim())
            ?? []);

    [Aggregated]
    [InverseProperty(nameof(ApprovalLegProfileMinistryLeg.ApprovalLegProfile))]
    public virtual IList<ApprovalLegProfileMinistryLeg> MinistryLegs { get; set; }

    [InverseProperty(nameof(ProjectContract.ApprovalLegProfile))]
    public virtual IList<ProjectContract> ProjectContracts { get; set; }

    /// <summary>Legacy many-to-many join — use <see cref="ProjectContracts"/> instead.</summary>
    [Obsolete("Use ProjectContracts (one-to-many). Retained for migration backfill only.")]
    [Browsable(false)]
    [InverseProperty(nameof(ProjectContractApprovalLegProfile.ApprovalLegProfile))]
    public virtual IList<ProjectContractApprovalLegProfile> ContractLinks { get; set; }

    public override void OnSaving()
    {
        ApprovalLegProfileMinistryHelper.WireMinistryLegs(this);
        base.OnSaving();
    }
}
