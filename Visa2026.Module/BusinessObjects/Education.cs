using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using Visa2026.Module.Editors;
using System.Linq;
namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Lookup/Education")]
    [DefaultProperty(nameof(EducationDescription))]
    [SupportsOptionalDetailFields]
    public class Education : BaseObject, ISoftDelete, IOptionalDetailFields
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

        [MaxLength(8)]
        public virtual string GraduationYear { get; set; }

        [Browsable(false)]
        [RuleFromBoolProperty("GraduationYearRange", DefaultContexts.Save,
            "Graduation Year must be between 1950 and 10 years from now.")]
        public bool IsGraduationYearInRange => IsValidGraduationYear(GraduationYear);

        [RuleRequiredField]
        public virtual Person Person { get; set; }

        [NotMapped]
        [ImmediatePostData]
        [Index(-1000)]
        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        [EditorAlias(OptionalDetailFieldsEditorAliases.Toggle)]
        [ModelDefault("CustomCSSClassName", "xaf-optional-fields-toggle")]
        [XafDisplayName(" ")]
        public bool ShowOptionalFields { get; set; }

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
                if (!string.IsNullOrWhiteSpace(GraduationYear))
                    parts.Add(GraduationYear.Trim());

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

        internal static bool IsValidGraduationYear(string year)
        {
            if (string.IsNullOrWhiteSpace(year))
                return true;

            if (!int.TryParse(year.Trim(), out int parsedYear))
                return false;

            int maxYear = DateTime.Today.Year + 10;
            return parsedYear >= 1950 && parsedYear <= maxYear;
        }

        public override void OnSaving()
        {
            GraduationYear = string.IsNullOrWhiteSpace(GraduationYear) ? null : GraduationYear.Trim();
            base.OnSaving();
        }

        public override void OnCreated()
        {
            base.OnCreated();
            var objectSpace = ObjectSpaceHelper.Get(this);
            if (objectSpace != null)
            {
                EducationLevel = objectSpace.GetObjectsQuery<EducationLevel>().FirstOrDefault(e => e.IsDefault);
                EducationCountry = objectSpace.GetObjectsQuery<Country>().FirstOrDefault(c => c.IsDefault);
                Specialty = objectSpace.GetObjectsQuery<Specialty>().FirstOrDefault(s => s.IsDefault);
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
