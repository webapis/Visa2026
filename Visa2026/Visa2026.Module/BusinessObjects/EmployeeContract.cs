using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Employee")]
    [DefaultProperty(nameof(Title))]
    [RuleCriteria("EmployeeContract_DateRange", DefaultContexts.Save, "ExpirationDate > ContractStartDate", "Expiration Date must be later than Contract Start Date.")]
    public class EmployeeContract : SingleActiveBaseObject<Person, EmployeeContract>, IExpirationLogic
    {
        public EmployeeContract()
        {
            Images = new ObservableCollection<EmployeeContractImage>();
            Documents = new ObservableCollection<EmployeeContractDocument>();
        }

        [RuleRequiredField]
        [DataSourceCriteria("IsEmployee = true")]
        public virtual Person Person
        {
            get => person;
            set
            {
                if (person != value)
                {
                    person = value;
                    if (person != null && PositionHistory == null)
                    {
                        PositionHistory = person.CurrentPositionHistory;
                    }
                }
            }
        }
        private Person person;

        public virtual DateTime ContractStartDate { get; set; }

        public virtual DateTime? ExpirationDate { get; set; }

        [RuleRequiredField]
        public virtual EmployeePositionHistory PositionHistory { get; set; }

        public virtual decimal Salary { get; set; }

        public virtual ContractTemplate ContractTemplate { get; set; }

        [NotMapped]
        public string Title => $"{PositionHistory?.Position?.Name} since {ContractStartDate:d}";

        [InverseProperty(nameof(EmployeeContractImage.EmployeeContract))]
        [Aggregated]
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
                    return int.MaxValue;
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
        #endregion
    }
}