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
    [NavigationItem("Lookup/Education")]
    [DefaultProperty(nameof(EducationDescription))]
    [RuleCriteria("GraduationYearRange", DefaultContexts.Save, "GraduationYear >= 1950 AND GraduationYear <= GetYear(LocalDateTimeToday()) + 10", "Graduation Year must be between 1950 and 10 years from now.")]
    public class Education : SingleActiveBaseObject<Person, Education>,ISoftDelete
    {
        public Education()
        {
            Images = new ObservableCollection<EducationImage>();
            Documents = new ObservableCollection<EducationDocument>();
        }

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
        [VisibleInDetailView(false)]
        [VisibleInListView(false)]
        public virtual IList<EducationImage> Images { get; set; }

        [InverseProperty(nameof(EducationDocument.Education))]
        [Aggregated]
        public virtual IList<EducationDocument> Documents { get; set; }

        [NotMapped]
        public string EducationDescription
        {
            get
            {
                var parts = new List<string>();
                void AddLookup(LookupBase entity)
                {
                    var text = FormatLookupDisplay(entity);
                    if (!string.IsNullOrWhiteSpace(text))
                        parts.Add(text);
                }

                AddLookup(EducationLevel);
                AddLookup(EducationInstitution);
                AddLookup(EducationCountry);
                AddLookup(Specialty);
                if (GraduationYear.HasValue)
                    parts.Add(GraduationYear.Value.ToString());

                return string.Join(", ", parts);
            }
        }

        /// <summary>Matches list UI (<see cref="LookupBase.NameTm"/> is default display).</summary>
        private static string FormatLookupDisplay(LookupBase entity)
        {
            if (entity == null) return null;
            var tm = entity.NameTm?.Trim();
            if (!string.IsNullOrEmpty(tm)) return tm;
            var name = entity.Name?.Trim();
            return string.IsNullOrEmpty(name) ? null : name;
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