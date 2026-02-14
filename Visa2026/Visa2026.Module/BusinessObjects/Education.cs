using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [RuleCriteria("EducationDateRange", DefaultContexts.Save, "EducationEndDate is null or EducationStartDate is null or EducationEndDate > EducationStartDate", "End Date must be greater than Start Date")]
    public class Education : SingleActiveBaseObject<Person, Education>
    {
        [RuleRequiredField]
        public virtual EducationLevel EducationLevel { get; set; }

        [RuleRequiredField]
        public virtual EducationInstitution EducationInstitution { get; set; }

        [RuleRequiredField]
        public virtual Country EducationCountry { get; set; }

        public virtual Specialty Specialty { get; set; }

        public virtual bool HasEducationPeriod { get; set; }

        [Appearance("EducationStartDateVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "!HasEducationPeriod", Context = "DetailView")]
        public virtual DateTime? EducationStartDate { get; set; }

        [Appearance("EducationEndDateVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "!HasEducationPeriod", Context = "DetailView")]
        public virtual DateTime? EducationEndDate { get; set; }

        [RuleRequiredField]
        public virtual Person Person { get; set; }

        [InverseProperty(nameof(EducationDocument.Education))]
        public virtual IList<EducationDocument> DiplomaDocuments { get; set; } = new ObservableCollection<EducationDocument>();

        public override Person GetParent()
        {
            return Person;
        }

        public override IList<Education> GetSiblings(Person parent)
        {
            return parent?.Educations;
        }

        public override void SetParentActiveItem(Person parent, Education item)
        {
            parent.CurrentEducation = item;
        }

        public override bool IsParentActiveItem(Person parent, Education item)
        {
            return parent.CurrentEducation == item;
        }
    }
}