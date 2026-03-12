using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using System.Linq;
namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Lookup/Person")]
    [DefaultProperty(nameof(EducationDescription))]
    [RuleCriteria("GraduationYearRange", DefaultContexts.Save, "GraduationYear >= 1950 AND GraduationYear <= GetYear(LocalDateTimeToday()) + 10", "Graduation Year must be between 1950 and 10 years from now.")]
    public class Education : SingleActiveBaseObject<Person, Education>,ISoftDelete
    {
        [RuleRequiredField]
        public virtual EducationLevel EducationLevel { get; set; }

        [RuleRequiredField]
        public virtual EducationInstitution EducationInstitution { get; set; }

        [RuleRequiredField]
        public virtual Country EducationCountry { get; set; }
        [RuleRequiredField]
        public virtual Specialty Specialty { get; set; }

    
        [RuleRequiredField]
        public virtual int? GraduationYear { get; set; }

        [RuleRequiredField]
        public virtual Person Person { get; set; }

        [InverseProperty(nameof(EducationImage.Education))]
        [Aggregated]
        public virtual IList<EducationImage> Images { get; set; } = new ObservableCollection<EducationImage>();

        [InverseProperty(nameof(EducationDocument.Education))]
        [Aggregated]
        public virtual IList<EducationDocument> Documents { get; set; } = new ObservableCollection<EducationDocument>();

        [NotMapped]
        public string EducationDescription
        {
            get
            {
                var parts = new List<string>();
                if (EducationLevel != null) parts.Add(EducationLevel.Name);
                if (EducationCountry != null) parts.Add(EducationCountry.Name);
                if (Specialty != null) parts.Add(Specialty.Name);
                if (GraduationYear.HasValue) parts.Add(GraduationYear.Value.ToString());

                return string.Join(", ", parts);
            }
        }

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

        public override void OnCreated()
        {
            base.OnCreated();
            if (ObjectSpace != null)
            {
                EducationLevel = ObjectSpace.GetObjectsQuery<EducationLevel>().FirstOrDefault(e => e.IsDefault);
                EducationCountry = ObjectSpace.GetObjectsQuery<Country>().FirstOrDefault(c => c.IsDefault);
                Specialty = ObjectSpace.GetObjectsQuery<Specialty>().FirstOrDefault(s => s.IsDefault);
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