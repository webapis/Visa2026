using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using System.Linq;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Lookup/Medical")]
        [DefaultProperty(nameof(DocumentNumber))]
    [RuleCriteria("MedicalRecord_DateRange", DefaultContexts.Save, "ExpirationDate > IssueDate", "Expiration Date must be later than Issue Date.")]
    public class MedicalRecord : BaseObject, IObjectSpaceLink, IExpirationLogic, ISoftDelete
    {
        public MedicalRecord()
        {
            Documents = new ObservableCollection<MedicalRecordDocument>();
            Images = new ObservableCollection<MedicalRecordImage>();
        }

        [MaxLength(50)]
        [RuleRequiredField]
        public virtual string DocumentNumber { get; set; }

        private DateTime issueDate;
        [RuleRequiredField]
        [ImmediatePostData]
        [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
        [ModelDefault("EditMask", "dd.MM.yyyy")]
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

        [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
        [ModelDefault("EditMask", "dd.MM.yyyy")]
        public virtual DateTime? ExpirationDate { get; protected set; }

        [RuleRequiredField]
        public virtual Person Person { get; set; }

        [InverseProperty(nameof(MedicalRecordDocument.MedicalRecord))]
        [Aggregated]
        public virtual IList<MedicalRecordDocument> Documents { get; set; }

        [InverseProperty(nameof(MedicalRecordImage.MedicalRecord))]
        [Aggregated]
        [VisibleInDetailView(false)]
        [VisibleInListView(false)]
        public virtual IList<MedicalRecordImage> Images { get; set; }

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

        public override void OnCreated()
        {
            base.OnCreated();
            IssueDate = DateTime.Today;
            if (ObjectSpace != null)
            {
                ValidityDuration = ObjectSpace.GetObjectsQuery<ValidityDuration>().FirstOrDefault(v => v.IsDefault);
            }
        }

        public override void OnSaving()
        {
            base.OnSaving();
        }

        [Browsable(false)]
        public virtual bool IsDeleted { get; set; }

        [Browsable(false)]
        public virtual DateTime? DateDeleted { get; set; }

        [Browsable(false)]
        public virtual ApplicationUser DeletedBy { get; set; }

        #region IObjectSpaceLink
        [NotMapped]
        [Browsable(false)]
        public IObjectSpace ObjectSpace { get; set; }
        #endregion
    }
}
