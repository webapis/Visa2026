using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using Visa2026.Module.Editors;
using Visa2026.Module.Services;
using Visa2026.Module.Services.ApplicationPersonRoster;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("WorkPermit")]
    [DefaultProperty(nameof(WorkPermitNumber))]
    [SupportsOptionalDetailFields]
    public class WorkPermit : BaseObject, IOptionalDetailFields
    {
        [RuleRequiredField]
        public virtual string WorkPermitNumber { get; set; }

        [RuleRequiredField]
        [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
        [ModelDefault("EditMask", "dd.MM.yyyy")]
        [XafDisplayName("Issued Date")]
        [Column("StartDate")]
        public virtual DateTime IssuedDate { get; set; }

        [NotMapped]
        [ImmediatePostData]
        [Index(-1000)]
        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        [EditorAlias(OptionalDetailFieldsEditorAliases.Toggle)]
        [ModelDefault("CustomCSSClassName", "xaf-optional-fields-toggle")]
        [XafDisplayName(" ")]
        public bool ShowOptionalFields { get; set; }

        /// <summary>
        /// Application Profile Instance that issued this work permit (1:N from instance Issued records).
        /// Set at create from instance workspace; read-only on detail. Import sets via Path B.
        /// </summary>
        [ExcludeFromOptionalDetailFields]
        [ModelDefault("AllowEdit", "False")]
        [VisibleInListView(false)]
        [VisibleInDetailView(true)]
        [VisibleInLookupListView(false)]
        [DataSourceProperty(nameof(AvailableApplicationProfileInstances))]
        [ToolTip("Application Profile Instance that produced this work permit. Set when created from Issued records on that case.")]
        [InverseProperty(nameof(ApplicationProfileInstance.WorkPermits))]
        [XafDisplayName("Issuing profile instance")]
        public virtual ApplicationProfileInstance ApplicationProfileInstance { get; set; }

        /// <summary>
        /// Candidate applications for <see cref="Application"/> (types that may produce a work permit).
        /// </summary>
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
                        (a.ApplicationProfile != null && a.ApplicationProfile.ProduceWorkPermit)
                        || (a.ApplicationType != null && a.ApplicationType.CanIssueWorkPermit))
                    .OrderByDescending(a => a.ApplicationDate)
                    .ThenBy(a => a.FullApplicationNumber)
                    .ToList();
            }
        }

        [RuleFromBoolProperty(
            "WorkPermit_ApplicationProfileInstanceRequired",
            DefaultContexts.Save,
            "Create the work permit from Application Profile Instance → Issued records (New work permit), not from the Work Permit list.")]
        [Browsable(false)]
        public bool IsApplicationProfileInstanceRequired =>
            WorkPermitIssuingOriginPolicy.HasRequiredApplicationProfileInstance(this);

        [RuleFromBoolProperty(
            "WorkPermit_ApplicationTypeAllowed",
            DefaultContexts.Save,
            "ApplicationProfileInstance must be a type that can issue a work permit.")]
        [Browsable(false)]
        public bool IsApplicationTypeAllowed
        {
            get
            {
                if (ApplicationProfileInstance == null)
                    return true;
                return ApplicationTypeCapabilities.CanIssueWorkPermit(ApplicationProfileInstance);
            }
        }

        [RuleFromBoolProperty(
            "WorkPermit_ApplicationProfileInstanceSingleUse",
            DefaultContexts.Save,
            "This issuing application is already linked to another work permit.")]
        [Browsable(false)]
        public bool IsApplicationProfileInstanceSingleUse
        {
            get
            {
                if (ApplicationProfileInstance == null)
                    return true;

                var objectSpace = ObjectSpaceHelper.Get(this);
                if (objectSpace == null)
                    return true;

                var appId = ApplicationProfileInstance.ID;
                return !objectSpace.GetObjectsQuery<WorkPermit>()
                    .Any(w => w.ID != ID
                        && w.ApplicationProfileInstance != null
                        && w.ApplicationProfileInstance.ID == appId);
            }
        }

        public override void OnCreated()
        {
            base.OnCreated();
            if (IssuedDate == default)
                IssuedDate = DateTime.Today;
        }

        public override void OnSaving()
        {
            WorkPermitIssuedRosterItemsHelper.EnsureRosterWorkPermitItems(this);
            base.OnSaving();
        }

        [Aggregated]
        public virtual IList<WorkPermitItem> WorkPermitItems { get; set; } = new ObservableCollection<WorkPermitItem>();

        [XafDisplayName("Person count")]
        [VisibleInDetailView(false)]
        [VisibleInListView(true)]
        [NotMapped]
        public int TotalPersonCount => listViewTotalPersonCount ?? WorkPermitItems?.Count ?? 0;

        private int? listViewTotalPersonCount;

        public void SetListViewTotalPersonCount(int count) => listViewTotalPersonCount = count;

        [Aggregated]
        public virtual IList<WorkPermitDocument> Documents { get; set; } = new ObservableCollection<WorkPermitDocument>();

        [Aggregated]
        [InverseProperty(nameof(WorkPermitImage.WorkPermit))]
        [VisibleInDetailView(false)]
        [VisibleInListView(false)]
        public virtual IList<WorkPermitImage> Images { get; set; } = new ObservableCollection<WorkPermitImage>();

        [NotMapped]
        [Browsable(false)]
        public virtual IList<Person> AvailableEmployees
        {
            get
            {
                var roster = ApplicationRosterHelper.GetRosterPeople(ApplicationProfileInstance)
                    .Where(p => p.IsEmployee)
                    .ToList();
                if (roster.Count > 0)
                    return roster;

                IObjectSpace? objectSpace = ObjectSpaceHelper.Get(this);
                if (objectSpace == null)
                {
                    return Array.Empty<Person>();
                }

                return objectSpace.GetObjectsQuery<Person>()
                    .Where(p => !p.IsArchived && p.IsEmployee)
                    .OrderBy(p => p.LastName)
                    .ThenBy(p => p.FirstName)
                    .ToList();
            }
        }

        /// <summary>ListView link column that opens header document copies in the preview slot.</summary>
        [NotMapped]
        [VisibleInDetailView(false)]
        [VisibleInLookupListView(false)]
        [ModelDefault("AllowEdit", "False")]
        public string DocumentCopiesListLink =>
            Visa2026.Module.Localization.VisaUiMessages.Get("WorkPermitDocumentCopies.List.ColumnLink");

    }
}