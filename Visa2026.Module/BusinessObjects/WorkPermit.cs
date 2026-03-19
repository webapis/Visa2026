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
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [Appearance("GrayOutIfDeleted", AppearanceItemType = "ViewItem", TargetItems = "*",
        Criteria = "IsDeleted", Context = "ListView", FontColor = "Gray")]
    [NavigationItem("Lookup/WorkPermit")]
    public class WorkPermit : BaseObject, IObjectSpaceLink, ISoftDelete
    {
        [RuleRequiredField]
        public virtual string WorkPermitNumber { get; set; }

        public virtual DateTime StartDate { get; set; }


        [RuleRequiredField]
        public virtual Application Application { get; set; }

        [Aggregated]
        public virtual IList<WorkPermitItem> WorkPermitItems { get; set; } = new ObservableCollection<WorkPermitItem>();

        [Aggregated]
        public virtual IList<WorkPermitDocument> Documents { get; set; } = new ObservableCollection<WorkPermitDocument>();

        [Aggregated]
        [InverseProperty(nameof(WorkPermitImage.WorkPermit))]
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
        public virtual bool IsCancelled
        {
            get => isCancelled;
            set
            {
                if (isCancelled != value)
                {
                    isCancelled = value;
                    if (ObjectSpace != null)
                    {
                        CrossObjectSyncHelper.SyncOnPropertyChanged(this, nameof(IsCancelled));
                        StateChangeTrackingHelper.TrackOnPropertyChanged(this, nameof(IsCancelled));
                    }
                }
            }
        }

        #region IObjectSpaceLink
        [NotMapped]
        [Browsable(false)]
        public IObjectSpace ObjectSpace { get; set; }
        #endregion

        [Browsable(false)]
        public virtual bool IsDeleted { get; set; }

        [Browsable(false)]
        public virtual DateTime? DateDeleted { get; set; }

        [Browsable(false)]
        public virtual ApplicationUser DeletedBy { get; set; }
    }
}