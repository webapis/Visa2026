using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Lookup/Person")]
        [DefaultProperty(nameof(DocumentNumber))]
    [RuleCriteria("MedicalRecord_DateRange", DefaultContexts.Save, "ExpirationDate > IssueDate", "Expiration Date must be later than Issue Date.")]
    public class MedicalRecord : SingleActiveBaseObject<Person, MedicalRecord>, IExpirationLogic
    {
        [MaxLength(50)]
        [RuleRequiredField]
        public virtual string DocumentNumber { get; set; }

        private DateTime issueDate;
        [RuleRequiredField]
        [ImmediatePostData]
        public virtual DateTime IssueDate
        {
            get => issueDate;
            set
            {
                if (issueDate != value)
                {
                    issueDate = value;
                    UpdateExpirationDate();
                }
            }
        }

        private ValidityDuration validityDuration;
        [ImmediatePostData]
         [RuleRequiredField]
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

        public virtual DateTime? ExpirationDate { get; protected set; }

        [RuleRequiredField]
        public virtual Person Person { get; set; }

        [InverseProperty(nameof(MedicalRecordDocument.MedicalRecord))]
        [Aggregated]
        public virtual IList<MedicalRecordDocument> Documents { get; set; } = new ObservableCollection<MedicalRecordDocument>();

        [InverseProperty(nameof(MedicalRecordImage.MedicalRecord))]
        [Aggregated]
        public virtual IList<MedicalRecordImage> Images { get; set; } = new ObservableCollection<MedicalRecordImage>();

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
                return ExpirationLogicHelper.CalculateExpirationState(this, IssueDate, ObjectSpace);
            }
        }

        #endregion

        private void UpdateExpirationDate()
        {
            if (ValidityDuration != null && IssueDate != default)
            {
                ExpirationDate = IssueDate.AddDays(ValidityDuration.NumberOfDays);
            }
            else
            {
                ExpirationDate = null;
            }
        }

        public override Person GetParent()
        {
            return Person;
        }

        public override IList<MedicalRecord> GetSiblings(Person parent)
        {
            return parent?.MedicalRecords;
        }

        public override void SetParentActiveItem(Person parent, MedicalRecord item)
        {
            parent.CurrentMedicalRecord = item;
        }

        public override bool IsParentActiveItem(Person parent, MedicalRecord item)
        {
            return parent.CurrentMedicalRecord == item;
        }

        public override void OnCreated()
        {
            base.OnCreated();
            IssueDate = DateTime.Today;
        }
    }
}