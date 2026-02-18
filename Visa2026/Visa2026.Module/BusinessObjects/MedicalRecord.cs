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
    public class MedicalRecord : SingleActiveBaseObject<Person, MedicalRecord>
    {
        [MaxLength(50)]
        [RuleRequiredField]
        public virtual string DocumentNumber { get; set; }

        [RuleRequiredField]
        public virtual DateTime IssueDate { get; set; }

        [RuleRequiredField]
        public virtual Person Person { get; set; }

        [InverseProperty(nameof(MedicalRecordDocument.MedicalRecord))]
        [Aggregated]
        public virtual IList<MedicalRecordDocument> Documents { get; set; } = new ObservableCollection<MedicalRecordDocument>();

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
    }
}