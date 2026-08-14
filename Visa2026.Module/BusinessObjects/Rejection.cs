using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Linq;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using Visa2026.Module.Localization;
using Visa2026.Module.Services.ApplicationPersonRoster;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Rejection")]
    [DefaultProperty(nameof(RejectionTitle))]
    public class Rejection : BaseObject, IPersonLinkParent
    {
        public Rejection()
        {
            Images = new ObservableCollection<RejectionImage>();
            Documents = new ObservableCollection<RejectionDocument>();
            RejectionItems = new ObservableCollection<RejectionItem>();
        }

        [RuleRequiredField]
        [ImmediatePostData]
        [VisibleInListView(false)]
        [DataSourceProperty(nameof(AvailableApplicationProfileInstances))]
        [InverseProperty(nameof(ApplicationProfileInstance.Rejections))]
        [ToolTip("Link this rejection to the application that produced it.")]
        public virtual ApplicationProfileInstance ApplicationProfileInstance { get; set; }

        /// <summary>Candidate applications whose profile may produce a rejection.</summary>
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
                        (a.ApplicationProfile != null && a.ApplicationProfile.ProduceRejection)
                        || (a.ApplicationType != null && a.ApplicationType.ShowRejections))
                    .OrderByDescending(a => a.ApplicationDate)
                    .ThenBy(a => a.FullApplicationNumber)
                    .ToList();
            }
        }

        [RuleFromBoolProperty(
            "Rejection_ApplicationTypeAllowed",
            DefaultContexts.Save,
            "ApplicationProfileInstance must be a type that can produce a rejection.")]
        [Browsable(false)]
        public bool IsApplicationTypeAllowed
        {
            get
            {
                if (ApplicationProfileInstance == null)
                    return true;
                return ApplicationTypeCapabilities.CanIssueRejection(ApplicationProfileInstance);
            }
        }

        [MaxLength(50)]
        public virtual string RejectedDocNumber { get; set; }

        public virtual string Reason { get; set; }

        [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
        [ModelDefault("EditMask", "dd.MM.yyyy")]
        public virtual DateTime Date { get; set; }

        [InverseProperty(nameof(RejectionImage.Rejection))]
        [DevExpress.ExpressApp.DC.Aggregated]
        [VisibleInDetailView(false)]
        [VisibleInListView(false)]
        public virtual IList<RejectionImage> Images { get; set; }

        [InverseProperty(nameof(RejectionDocument.Rejection))]
        [DevExpress.ExpressApp.DC.Aggregated]
        public virtual IList<RejectionDocument> Documents { get; set; }
        
        [InverseProperty(nameof(RejectionItem.Rejection))]
        [DevExpress.ExpressApp.DC.Aggregated]
        public virtual IList<RejectionItem> RejectionItems { get; set; }

        [NotMapped]
        [Browsable(false)]
        public virtual IList<Person> AvailablePeople
        {
            get
            {
                return ApplicationRosterHelper.GetRosterPeople(ApplicationProfileInstance);
            }
        }

        [NotMapped]
        public string RejectionTitle => VisaUiMessages.Format(
            "Rejection.DisplayTitle",
            RejectedDocNumber ?? string.Empty,
            Date.ToString("d", CultureInfo.CurrentUICulture));

        /// <summary>ListView link column that opens header document copies in the preview slot.</summary>
        [NotMapped]
        [VisibleInDetailView(false)]
        [VisibleInLookupListView(false)]
        [ModelDefault("AllowEdit", "False")]
        public string DocumentCopiesListLink =>
            VisaUiMessages.Get("RejectionDocumentCopies.List.ColumnLink");
    }
}