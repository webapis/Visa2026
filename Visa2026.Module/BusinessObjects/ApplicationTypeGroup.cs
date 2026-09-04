using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Named set of <see cref="ApplicationType"/> rows for report-template applicability
/// (e.g. Registration). Distinct from UI flags such as <see cref="ApplicationType.ShowRegistrations"/>.
/// </summary>
[DefaultClassOptions]
[NavigationItem("Lookups")]
[DefaultProperty(nameof(NameTm))]
[ModelDefault("Caption", "ApplicationProfileInstance Type Group")]
public class ApplicationTypeGroup : LookupBase
{
    public ApplicationTypeGroup()
    {
        Members = new ObservableCollection<ApplicationTypeGroupMember>();
    }

    public override string ToString() =>
        !string.IsNullOrWhiteSpace(NameTm) ? NameTm : (Name ?? string.Empty);

    [ModelDefault("Caption", "Sort Order")]
    public virtual int SortOrder { get; set; }

    [ModelDefault("Caption", "Is Active")]
    public virtual bool IsActive { get; set; } = true;

    [Aggregated]
    [ModelDefault("Caption", "ApplicationProfileInstance Types")]
    [ToolTip("ApplicationProfileInstance types that belong to this group for User Report Template applicability.")]
    public virtual IList<ApplicationTypeGroupMember> Members { get; set; }
}