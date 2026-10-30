using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Tenant-shared shelf tab for the Application Profile instance create picker.
/// Recent and All are virtual; relation/custom shelves are rows here.
/// </summary>
[NavigationItem(false)]
[DefaultProperty(nameof(Name))]
[XafDisplayName("Application Profile picker shelf")]
[ImageName("BO_Category")]
public class ApplicationProfilePickerShelf : BaseObject
{
    public ApplicationProfilePickerShelf()
    {
        Profiles = new ObservableCollection<ApplicationProfile>();
    }

    /// <summary>Stable key for system shelves (e.g. invitations). Null/empty for officer-created shelves.</summary>
    [MaxLength(64)]
    [XafDisplayName("Code")]
    public virtual string? Code { get; set; }

    [RuleRequiredField]
    [MaxLength(100)]
    [XafDisplayName("Name")]
    public virtual string Name { get; set; } = string.Empty;

    [XafDisplayName("Sort order")]
    public virtual int SortOrder { get; set; }

    /// <summary>Seeded system shelf — not deletable from the picker.</summary>
    [XafDisplayName("System")]
    public virtual bool IsSystem { get; set; }

    [VisibleInDetailView(false)]
    [VisibleInListView(false)]
    public virtual IList<ApplicationProfile> Profiles { get; set; }
}