using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("WorkPermit")]
    public class WorkPermit : BaseObject, ISoftDelete
    {
        [RuleRequiredField]
        public virtual string WorkPermitNumber { get; set; }

        [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
        [ModelDefault("EditMask", "dd.MM.yyyy")]
        [XafDisplayName("Issued Date")]
        [Column("StartDate")]
        public virtual DateTime IssuedDate { get; set; }


        [RuleRequiredField(TargetCriteria = "Not IsApplicationNotRequired")]
        public virtual Application Application { get; set; }

        [ImmediatePostData]
        [VisibleInListView(false)]
        public virtual bool IsApplicationNotRequired { get; set; }

        [Aggregated]
        public virtual IList<WorkPermitItem> WorkPermitItems { get; set; } = new ObservableCollection<WorkPermitItem>();

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
                return Application?.ApplicationItems.Select(ai => ai.Person).Where(p => p != null && p.IsEmployee).ToList() ?? new List<Person>();
            }
        }

        private bool isCancelled;
        [ImmediatePostData]
        [VisibleInListView(false)]
        public virtual bool IsCancelled
        {
            get => isCancelled;
            set
            {
                if (isCancelled != value)
                {
                    isCancelled = value;
                    if (ObjectSpaceHelper.Get(this) != null)
                    {
                        CrossObjectSyncHelper.SyncOnPropertyChanged(this, nameof(IsCancelled));
                        StateChangeTrackingHelper.TrackOnPropertyChanged(this, nameof(IsCancelled));
                    }
                }
            }
        }

        [Browsable(false)]
        public virtual bool IsDeleted { get; set; }

        [Browsable(false)]
        public virtual DateTime? DateDeleted { get; set; }

        [Browsable(false)]
        public virtual ApplicationUser DeletedBy { get; set; }
    }
}