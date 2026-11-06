using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using Visa2026.Module.Editors;
using Visa2026.Module.Services;
using Visa2026.Module.Services.ApplicationPersonRoster;

namespace Visa2026.Module.BusinessObjects;

[DefaultClassOptions]
[NavigationItem("WorkPermit")]
[XafDisplayName("Work permit location grant item")]
public class WorkPermitLocationGrantItem : BaseObject
{
    [RuleRequiredField]
    public virtual WorkPermitLocationGrant Grant { get; set; }

    private Person person;

    [RuleRequiredField]
    [DataSourceProperty(nameof(Grant) + "." + nameof(IPersonLinkParent.AvailablePeople))]
    public virtual Person Person
    {
        get => person;
        set
        {
            if (person != value)
            {
                person = value;
                if (person != null)
                    Passport = ApplicationProfileInstancePersonValidItems.ResolvePassport(person);
            }
        }
    }

    [RuleRequiredField]
    public virtual Passport Passport { get; set; }

    /// <summary>Newly granted places only — never written onto the source WorkPermitItem.</summary>
    [MaxLength(500)]
    [VisibleInListView(true)]
    [EditorAlias(CommaSeparatedMultiSelectEditorAliases.WorkPermittedLocation)]
    [CommaSeparatedMultiSelect(CatalogEntityType = typeof(WorkPermittedLocationName))]
    [XafDisplayName("Granted locations")]
    public virtual string GrantedLocations { get; set; }

    /// <summary>Source WorkPermitItem lineage (read-only reference).</summary>
    [VisibleInListView(false)]
    [ModelDefault("AllowEdit", "False")]
    [XafDisplayName("Source work permit item")]
    public virtual WorkPermitItem? SourceWorkPermitItem { get; set; }

    public override void OnSaving()
    {
        base.OnSaving();
        CrossObjectSyncHelper.SyncOnSave(this);
    }
}