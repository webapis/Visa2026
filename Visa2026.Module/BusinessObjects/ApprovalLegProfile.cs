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

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Reusable ministry approval chain (ordered legs + SLA).
/// Chosen on an application profile instance. Independent of <see cref="ProjectContract"/>.
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

    public override void OnSaving()
    {
        ApprovalLegProfileMinistryHelper.WireMinistryLegs(this);
        base.OnSaving();
    }
}
