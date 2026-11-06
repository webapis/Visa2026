using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using Visa2026.Module.Services;
using Visa2026.Module.Services.ApplicationPersonRoster;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Issued permission for additional work-permit places (goşmaça barjak ýeri).
/// Does not mutate <see cref="WorkPermitItem.WorkPermittedLocations"/> on the source permit.
/// </summary>
[DefaultClassOptions]
[NavigationItem("WorkPermit")]
[DefaultProperty(nameof(GrantNumber))]
[XafDisplayName("Work permit location grant")]
public class WorkPermitLocationGrant : BaseObject, IPersonLinkParent
{
    public WorkPermitLocationGrant()
    {
        Items = new ObservableCollection<WorkPermitLocationGrantItem>();
        Documents = new ObservableCollection<WorkPermitLocationGrantDocument>();
    }

    [MaxLength(50)]
    [RuleRequiredField]
    [XafDisplayName("Grant number")]
    public virtual string GrantNumber { get; set; }

    [RuleRequiredField]
    [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
    [ModelDefault("EditMask", "dd.MM.yyyy")]
    [XafDisplayName("Issued date")]
    public virtual DateTime IssuedDate { get; set; }

    [RuleRequiredField]
    [ImmediatePostData]
    [VisibleInListView(false)]
    [DataSourceProperty(nameof(AvailableApplicationProfileInstances))]
    [InverseProperty(nameof(ApplicationProfileInstance.WorkPermitLocationGrants))]
    [ToolTip("Application Profile Instance that produced this location grant.")]
    [XafDisplayName("Issuing profile instance")]
    public virtual ApplicationProfileInstance ApplicationProfileInstance { get; set; }

    /// <summary>Source work permit whose places are being extended (reference only).</summary>
    [VisibleInListView(false)]
    [XafDisplayName("Source work permit")]
    public virtual WorkPermit? SourceWorkPermit { get; set; }

    [NotMapped]
    [Browsable(false)]
    public IList<ApplicationProfileInstance> AvailableApplicationProfileInstances
    {
        get
        {
            var objectSpace = ObjectSpaceHelper.Get(this);
            if (objectSpace == null)
                return new List<ApplicationProfileInstance>();

            return objectSpace.GetObjectsQuery<ApplicationProfileInstance>()
                .Where(a =>
                    a.ApplicationProfile != null && a.ApplicationProfile.ProduceWorkLocation)
                .OrderByDescending(a => a.ApplicationDate)
                .ThenBy(a => a.FullApplicationNumber)
                .ToList();
        }
    }

    [RuleFromBoolProperty(
        "WorkPermitLocationGrant_ApplicationTypeAllowed",
        DefaultContexts.Save,
        "ApplicationProfileInstance must be a type that can produce a work-location grant.")]
    [Browsable(false)]
    public bool IsApplicationTypeAllowed
    {
        get
        {
            if (ApplicationProfileInstance == null)
                return true;
            return ApplicationTypeCapabilities.CanIssueWorkPermitLocationGrant(ApplicationProfileInstance);
        }
    }

    [Aggregated]
    [InverseProperty(nameof(WorkPermitLocationGrantItem.Grant))]
    public virtual IList<WorkPermitLocationGrantItem> Items { get; set; }

    [Aggregated]
    [InverseProperty(nameof(WorkPermitLocationGrantDocument.Grant))]
    public virtual IList<WorkPermitLocationGrantDocument> Documents { get; set; }

    [NotMapped]
    [Browsable(false)]
    public virtual IList<Person> AvailablePeople =>
        ApplicationRosterHelper.GetRosterPeople(ApplicationProfileInstance);

    [NotMapped]
    [VisibleInDetailView(false)]
    [VisibleInLookupListView(false)]
    [ModelDefault("AllowEdit", "False")]
    public string DocumentCopiesListLink =>
        Visa2026.Module.Localization.VisaUiMessages.Get("WorkPermitLocationGrantDocumentCopies.List.ColumnLink");
}