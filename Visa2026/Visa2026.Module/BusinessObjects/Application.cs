using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Specialized;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.Persistent.Base;
using System.Linq;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Application")]
    [DefaultProperty(nameof(ApplicationNumber))]
   
    public class Application : BaseObject
    {
        public Application()
        {
            ApplicationItems = new ObservableCollection<ApplicationItem>();
            Invitations = new ObservableCollection<Invitation>();
            Rejections = new ObservableCollection<Rejection>();
            WorkPermits = new ObservableCollection<WorkPermit>();

            var progressHistoryCollection = new ObservableCollection<ApplicationProgress>();
            progressHistoryCollection.CollectionChanged += ProgressHistory_CollectionChanged;
            progressHistory = progressHistoryCollection;
        }

        [MaxLength(50)]
        [RuleUniqueValue]
        public virtual string ApplicationNumber { get; set; }

        [RuleRequiredField]
        public virtual DateTime ApplicationDate { get; set; }

        private bool isForFamily;
        [ImmediatePostData]
        public virtual bool IsForFamily
        {
            get => isForFamily;
            set
            {
                if (isForFamily != value)
                {
                    isForFamily = value;
                    ApplicationType = null;
                }
            }
        }

        private OrganizationType organizationType;
        [ImmediatePostData]
        public virtual OrganizationType OrganizationType
        {
            get => organizationType;
            set
            {
                if (organizationType != value)
                {
                    organizationType = value;
                    ApplicationType = null;
                }
            }
        }

        [ImmediatePostData, RuleRequiredField]
     [DataSourceCriteria("OrganizationType = '@This.OrganizationType'")]
        public virtual ApplicationType ApplicationType { get; set; }

        [ModelDefault("AllowEdit", "False")]
        public virtual ApplicationProgress CurrentState { get; set; }


        [Appearance("ProjectContractVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "ApplicationType is null or !ApplicationType.ShowProjectContract", Context = "DetailView")]
        public virtual ProjectContract ProjectContract { get; set; }

        public virtual Urgency Urgency { get; set; }

        [Appearance("VisaPeriodVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "ApplicationType is null or !ApplicationType.ShowVisaPeriod", Context = "DetailView")]
        public virtual VisaPeriod VisaPeriod { get; set; }

        [Appearance("VisaCategoryVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "ApplicationType is null or !ApplicationType.ShowVisaCategory", Context = "DetailView")]
        public virtual VisaCategory VisaCategory { get; set; }

        [Appearance("MinistryVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "ApplicationType is null or !ApplicationType.ShowMinistry", Context = "DetailView")]
        public virtual Ministry Ministry { get; set; }

        [Aggregated]
        [InverseProperty(nameof(ApplicationItem.Application))]
        public virtual IList<ApplicationItem> ApplicationItems { get; set; }

        [Aggregated]
        [InverseProperty(nameof(Invitation.Application))]
        public virtual IList<Invitation> Invitations { get; set; }

        [Aggregated]
        [InverseProperty(nameof(Rejection.Application))]
        public virtual IList<Rejection> Rejections { get; set; }

        [Aggregated]
        [InverseProperty(nameof(WorkPermit.Application))]
        public virtual IList<WorkPermit> WorkPermits { get; set; }

        private void ProgressHistory_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateCurrentState();
        }

        private IList<ApplicationProgress> progressHistory;
        [Aggregated]
        [InverseProperty(nameof(ApplicationProgress.Application))]
        public virtual IList<ApplicationProgress> ProgressHistory
        {
            get => progressHistory;
            set
            {
                if (progressHistory is INotifyCollectionChanged oldCollection)
                {
                    oldCollection.CollectionChanged -= ProgressHistory_CollectionChanged;
                }
                progressHistory = value;
                if (progressHistory is INotifyCollectionChanged newCollection)
                {
                    newCollection.CollectionChanged += ProgressHistory_CollectionChanged;
                }
            }
        }

        public void UpdateCurrentState(ApplicationProgress changedProgress = null)
        {
            IEnumerable<ApplicationProgress> query = ProgressHistory;
            if (query == null)
            {
                if (changedProgress != null) query = new[] { changedProgress };
                else return;
            }
            else if (changedProgress != null && !query.Contains(changedProgress))
            {
                query = query.Concat(new[] { changedProgress });
            }

            if (ObjectSpace != null)
            {
                query = query.Where(p => !ObjectSpace.IsObjectToDelete(p));
            }

            var latestProgress = query
                .OrderByDescending(p => p.Date)
                .FirstOrDefault();

            if (CurrentState != latestProgress)
            {
                CurrentState = latestProgress;
            }
        }

        public override void OnLoaded()
        {
            base.OnLoaded();
            if (progressHistory is INotifyCollectionChanged collection)
            {
                collection.CollectionChanged -= ProgressHistory_CollectionChanged;
                collection.CollectionChanged += ProgressHistory_CollectionChanged;
            }
            UpdateCurrentState();
        }
    }
}