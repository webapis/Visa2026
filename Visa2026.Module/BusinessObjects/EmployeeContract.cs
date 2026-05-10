using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using System.Linq;
using DevExpress.ExpressApp;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Employee")]
    [DefaultProperty(nameof(Title))]
    [RuleCriteria("EmployeeContract_DateRange", DefaultContexts.Save, "ExpirationDate > ContractStartDate", "Expiration Date must be later than Contract Start Date.")]
    public class EmployeeContract : SingleActiveBaseObject<Person, EmployeeContract>, IExpirationLogic, ISoftDelete
    {
        public EmployeeContract()
        {
            Images = new ObservableCollection<EmployeeContractImage>();
            Documents = new ObservableCollection<EmployeeContractDocument>();
        }

        [RuleRequiredField]
        [DataSourceCriteria("IsEmployee = true")]
        [ImmediatePostData]
        public virtual Person Person
        {
            get => person;
            set
            {
                if (person != value)
                {
                    person = value;
                    SetDefaultPositionHistory();
                }
            }
        }
        private Person person;

        private DateTime contractStartDate;
        [RuleRequiredField, ImmediatePostData]
        [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
        [ModelDefault("EditMask", "dd.MM.yyyy")]
        public virtual DateTime ContractStartDate
        {
            get => contractStartDate;
            set
            {
                if (contractStartDate != value)
                {
                    contractStartDate = value;
                    UpdateExpirationDate();
                }
            }
        }

        [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
        [ModelDefault("EditMask", "dd.MM.yyyy")]
        public virtual DateTime? ExpirationDate { get; protected set; }

        private ValidityDuration validityDuration;
        [RuleRequiredField, ImmediatePostData]
        public virtual ValidityDuration ValidityDuration
        {
            get => validityDuration;
            set
            {
                if (validityDuration != value)
                {
                    validityDuration = value;
                    UpdateExpirationDate();
                }
            }
        }

        [RuleRequiredField]
        [DataSourceCriteria("Person = '@This.Person'")]
        public virtual EmployeePositionHistory PositionHistory { get; set; }

        public virtual decimal Salary { get; set; }

       // public virtual ContractTemplate ContractTemplate { get; set; }

        [NotMapped]
        public string Title => $"{PositionHistory?.Position?.Name} since {ContractStartDate:d}";

        [InverseProperty(nameof(EmployeeContractImage.EmployeeContract))]
        [Aggregated]
        [VisibleInDetailView(false)]
        [VisibleInListView(false)]
        public virtual IList<EmployeeContractImage> Images { get; set; }

        [InverseProperty(nameof(EmployeeContractDocument.EmployeeContract))]
        [Aggregated]
        public virtual IList<EmployeeContractDocument> Documents { get; set; }

        #region IExpirationLogic
        [NotMapped]
        public int DaysRemaining
        {
            get
            {
                if (!ExpirationDate.HasValue)
                {
                    // If there is no expiration date, for display purposes, it's better to show 0
                    // than a confusing large number like int.MaxValue.
                    return 0;
                }
                return (ExpirationDate.Value.Date - DateTime.Today).Days;
            }
        }

        [NotMapped]
        public ExpirationState ExpirationState
        {
            get
            {
                return ExpirationLogicHelper.CalculateExpirationState(this, ContractStartDate, ObjectSpace);
            }
        }
        #endregion

        private void SetDefaultPositionHistory()
        {
            // If the contract is being created for a person and the position history isn't set yet,
            // default it to the person's current position history.
            if (Person != null && PositionHistory == null)
            {
                PositionHistory = Person.CurrentPositionHistory;
            }
        }

        private void UpdateExpirationDate()
        {
            if (ValidityDuration != null && ContractStartDate != default)
            {
                ExpirationDate = ContractStartDate.AddDays(ValidityDuration.NumberOfDays);
            }
            else
            {
                ExpirationDate = null;
            }
        }

        [Browsable(false)]
        [NotMapped]
        protected override DateTime? ChronologicalSortDate => this.ContractStartDate;

        public override void OnCreated()
        {
            base.OnCreated();
            ContractStartDate = DateTime.Today;
            if (ObjectSpace != null)
            {
                //ContractTemplate = ObjectSpace.GetObjectsQuery<ContractTemplate>().FirstOrDefault(t => t.IsDefault);
                ValidityDuration = ObjectSpace.GetObjectsQuery<ValidityDuration>().FirstOrDefault(v => v.IsDefault);
            }
            SetDefaultPositionHistory();
        }

        public override void OnSaving()
        {
            base.OnSaving();
            CrossObjectSyncHelper.SyncOnSave(this);
        }

        #region SingleActiveBaseObject
        public override Person GetParent()
        {
            return Person;
        }

        public override IList<EmployeeContract> GetSiblings(Person parent)
        {
            return parent?.EmployeeContracts;
        }

        public override void SetParentActiveItem(Person parent, EmployeeContract item)
        {
            parent.CurrentEmployeeContract = item;
        }

        public override bool IsParentActiveItem(Person parent, EmployeeContract item)
        {
            return parent.CurrentEmployeeContract == item;
        }

              [Browsable(false)]
        public virtual bool IsDeleted { get; set; }

        [Browsable(false)]
        public virtual DateTime? DateDeleted { get; set; }

        [Browsable(false)]
        public virtual ApplicationUser DeletedBy { get; set; }
        #endregion
    }
}
