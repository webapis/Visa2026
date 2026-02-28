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
//    [RuleUniqueValue("UniqueAppNumberPerPrefix", DefaultContexts.Save, "AppNumberPrefix;ApplicationNumber;Year", CustomMessageTemplate = "An application with this prefix, number, and year already exists.")]
    public class Application : BaseObject, IExpirationLogic, IObjectSpaceLink
    {
        public Application()
        {
            ApplicationItems = new ObservableCollection<ApplicationItem>();
            Invitations = new ObservableCollection<Invitation>();
            Rejections = new ObservableCollection<Rejection>();
            WorkPermits = new ObservableCollection<WorkPermit>();
            Registrations = new ObservableCollection<Registration>();
            BusinessTrips = new ObservableCollection<BusinessTrip>();
            Visas = new ObservableCollection<Visa>();

            var progressHistoryCollection = new ObservableCollection<ApplicationProgress>();
            progressHistoryCollection.CollectionChanged += ProgressHistory_CollectionChanged;
            progressHistory = progressHistoryCollection;
        }

        [MaxLength(50)]
        [ModelDefault("AllowEdit", "False")]
        public virtual string ApplicationNumber { get; set; }
[ModelDefault("AllowEdit", "False")]
        public virtual string AppNumberPrefix { get; set; }

        [NotMapped]
        public string FullApplicationNumber => $"{AppNumberPrefix}{ApplicationNumber}";

        [ModelDefault("AllowEdit", "False")]
        public virtual int Year { get; set; }

        private DateTime applicationDate;
        [RuleRequiredField]
        public virtual DateTime ApplicationDate
        {
            get => applicationDate;
            set
            {
                if (applicationDate != value)
                {
                    applicationDate = value;
                    if (ApplicationType != null && ApplicationType.DurationInDays > 0)
                    {
                        ExpirationDate = applicationDate.AddDays(ApplicationType.DurationInDays);
                    }
                }
            }
        }

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

        private ApplicationTypeFilter applicationTypeFilter;
        [ImmediatePostData]
        public virtual ApplicationTypeFilter ApplicationTypeFilter
        {
            get => applicationTypeFilter;
            set
            {
                if (applicationTypeFilter != value)
                {
                    applicationTypeFilter = value;
                    ApplicationType = null;
                }
            }
        }

        private ApplicationType applicationType;
        [ImmediatePostData, RuleRequiredField]
        [DataSourceCriteria("OrganizationType = '@This.OrganizationType' And (Category = 'Both' Or (Category = 'FamilyMember' And '@This.IsForFamily' = true) Or (Category = 'Employee' And '@This.IsForFamily' = false)) And ('@This.ApplicationTypeFilter' Is Null Or ApplicationTypeFilter = '@This.ApplicationTypeFilter')")]
        public virtual ApplicationType ApplicationType
        {
            get => applicationType;
            set
            {
                if (applicationType != value)
                {
                    applicationType = value;
                    if (applicationType != null && applicationType.DurationInDays > 0)
                    {
                        ExpirationDate = ApplicationDate.AddDays(applicationType.DurationInDays);
                    }
                }
            }
        }

        [ModelDefault("AllowEdit", "False")]
        public virtual ApplicationProgress CurrentState { get; set; }


        [Appearance("ProjectContractVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "ApplicationType is null or !ApplicationType.ShowProjectContract", Context = "DetailView")]
        public virtual ProjectContract ProjectContract { get; set; }

        private Company company;
        [ImmediatePostData]
        public virtual Company Company
        {
            get => company;
            set
            {
                if (company != value)
                {
                    company = value;
                    CompanyHead = company?.CurrentAuthorizedSignatory;
                    Representative = company?.CurrentRepresentative;
                }
            }
        }

        [DataSourceCriteria("Company = '@This.Company'")]
        public virtual CompanyHead CompanyHead { get; set; }

        [DataSourceCriteria("Company = '@This.Company'")]
        public virtual Representative Representative { get; set; }

        [Appearance("UrgencyVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "ApplicationType is null or !ApplicationType.ShowUrgency", Context = "DetailView")]
        public virtual Urgency Urgency { get; set; }

        [Appearance("VisaPeriodVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "ApplicationType is null or !ApplicationType.ShowVisaPeriod", Context = "DetailView")]
        public virtual VisaPeriod VisaPeriod { get; set; }

        [Appearance("VisaCategoryVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "ApplicationType is null or !ApplicationType.ShowVisaCategory", Context = "DetailView")]
        public virtual VisaCategory VisaCategory { get; set; }

        [Appearance("MigrationServiceVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "ApplicationType is null or !ApplicationType.ShowMigrationService", Context = "DetailView")]
        public virtual MigrationService MigrationService { get; set; }

        [Aggregated]
        [Appearance("BusinessTripPlanVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "ApplicationType is null or !ApplicationType.ShowBusinessTripPlan", Context = "DetailView")]
        public virtual BusinessTripPlan BusinessTripPlan { get; set; }

        [Aggregated]
        [InverseProperty(nameof(ApplicationItem.Application))]
        [Appearance("ApplicationItemsVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "ApplicationType is null or !ApplicationType.ShowApplicationItems", Context = "DetailView")]
        public virtual IList<ApplicationItem> ApplicationItems { get; set; }

        [Aggregated]
        [InverseProperty(nameof(Invitation.Application))]
        [Appearance("InvitationsVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "ApplicationType is null or !ApplicationType.ShowInvitations", Context = "DetailView")]
        public virtual IList<Invitation> Invitations { get; set; }

        [Aggregated]
        [InverseProperty(nameof(Rejection.Application))]
        [Appearance("RejectionsVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "ApplicationType is null or !ApplicationType.ShowRejections", Context = "DetailView")]
        public virtual IList<Rejection> Rejections { get; set; }

        [Aggregated]
        [InverseProperty(nameof(WorkPermit.Application))]
        [Appearance("WorkPermitsVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "ApplicationType is null or !ApplicationType.ShowWorkPermits", Context = "DetailView")]
        public virtual IList<WorkPermit> WorkPermits { get; set; }

        [Aggregated]
        [InverseProperty(nameof(Registration.Application))]
        [Appearance("RegistrationsVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "ApplicationType is null or !ApplicationType.ShowRegistrations", Context = "DetailView")]
        public virtual IList<Registration> Registrations { get; set; }

        [Aggregated]
        [InverseProperty(nameof(BusinessTrip.Application))]
        [Appearance("BusinessTripsVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "ApplicationType is null or !ApplicationType.ShowBusinessTrips", Context = "DetailView")]
        public virtual IList<BusinessTrip> BusinessTrips { get; set; }

        [InverseProperty(nameof(Visa.Application))]
        [Appearance("VisasVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "ApplicationType is null or !ApplicationType.ShowVisas", Context = "DetailView")]
        public virtual IList<Visa> Visas { get; set; }

        [RuleRequiredField]
        [DataSourceCriteria("ApplicationType.ID = '@This.ApplicationType.ID'")]
        [Appearance("ApplicationReasonVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "ApplicationType is null or !ApplicationType.ShowApplicationReason", Context = "DetailView")]
        public virtual ApplicationReason ApplicationReason { get; set; }

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

        public virtual bool IsActive { get; set; } = true;
        [ModelDefault("AllowEdit", "False")]
        public virtual DateTime? ExpirationDate { get; set; }

        [NotMapped]
        public int DaysRemaining
        {
            get
            {
                if (!ExpirationDate.HasValue) return int.MaxValue;
                return (ExpirationDate.Value.Date - DateTime.Today).Days;
            }
        }

        [NotMapped]
        public ExpirationState ExpirationState
        {
            get
            {
                if (!IsActive) return ExpirationState.Archived;
                if (DaysRemaining < 0) return ExpirationState.Expired;
                if (ExpirationDate.HasValue)
                {
                    double totalDays = (ExpirationDate.Value.Date - ApplicationDate.Date).Days;
                    if (totalDays > 0)
                    {
                        double elapsedDays = (DateTime.Today - ApplicationDate.Date).Days;
                        if (ObjectSpace != null)
                        {
                            var threshold = (double)SystemSettings.GetInstance(ObjectSpace).ExpirationWarningThreshold;
                            if (elapsedDays / totalDays >= threshold) return ExpirationState.ExpiringSoon;
                        }
                    }
                    else
                    {
                        return ExpirationState.ExpiringSoon;
                    }
                }
                return ExpirationState.Active;
            }
        }

        #region IObjectSpaceLink
        [NotMapped]
        [Browsable(false)]
        public IObjectSpace ObjectSpace { get; set; }
        #endregion

        public override void OnCreated()
        {
            base.OnCreated();
            if (ObjectSpace != null)
            {
                ApplicationDate = DateTime.Now;
                Company = ObjectSpace.GetObjectsQuery<Company>().FirstOrDefault(c => c.IsDefault);
            }
        }

        public override void OnSaving()
        {
            base.OnSaving();
            // This logic should only run when creating a new Application.
            if (ObjectSpace != null && ObjectSpace.IsNewObject(this))
            {
                // Set the year from the application date
                Year = ApplicationDate.Year;

                // If a company is selected and the prefix is not yet set,
                // default the prefix from the company's default.
                if (Company != null && string.IsNullOrEmpty(AppNumberPrefix))
                {
                    AppNumberPrefix = Company.AppNumberPrefix;
                }

                // Only auto-generate the number if it has not been set manually.
                if (string.IsNullOrEmpty(ApplicationNumber))
                {
                    // Find the highest existing application number for the given prefix.
                    var lastAppNumberForPrefix = ObjectSpace.GetObjectsQuery<Application>()
                        .Where(a => a.AppNumberPrefix == this.AppNumberPrefix && a.Year == this.Year)
                        .Select(a => a.ApplicationNumber)
                        .ToList() // Switch to LINQ to Objects for parsing
                        .Select(numStr => int.TryParse(numStr, out int number) ? number : 0)
                        .DefaultIfEmpty(0)
                        .Max();

                    // Determine the padding from the company, with a fallback to 4.
                    int padding = Company?.ApplicationNumberPadding > 0 ? Company.ApplicationNumberPadding : 4;
                    string format = $"D{padding}";

                    ApplicationNumber = (lastAppNumberForPrefix + 1).ToString(format);
                }
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