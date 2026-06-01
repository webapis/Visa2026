using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using DevExpress.ExpressApp.Model;
using System.ComponentModel;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.BaseImpl.EF;
using Visa2026.Module.Editors;
using Visa2026.Module.Services.StateEvaluation;
using Visa2026.Module.Services.StateEvaluation.Evaluators;
namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [DefaultProperty(nameof(PassportNumber))]
    [NavigationItem("Lookup/Passport")]
    [RuleCriteria("Passport_DateRange", DefaultContexts.Save, "ExpirationDate > IssueDate", "Expiration Date must be later than Issue Date.")]
    [Appearance("PassportStateWarning", Priority = 200, AppearanceItemType = "ViewItem", TargetItems = "*",
        Criteria = "IsDeleted = false And StateSeverityLevel = 2", Context = "ListView", BackColor = "LightSalmon")]
    [Appearance("PassportStateCritical", Priority = 300, AppearanceItemType = "ViewItem", TargetItems = "*",
        Criteria = "IsDeleted = false And StateSeverityLevel >= 3", Context = "ListView", BackColor = "LightCoral")]
    [SupportsOptionalDetailFields]
    public class Passport : BaseObject, IExpirationLogic, ISoftDelete, IOptionalDetailFields
    {
        public Passport()
        {
            Images = new ObservableCollection<PassportImage>();
            Documents = new ObservableCollection<PassportDocument>();
            Visas = new ObservableCollection<Visa>();
        }

        [MaxLength(20)]
        [RuleRequiredField]
        public virtual string PassportNumber { get; set; }

        /// <summary>
        /// Enforces uniqueness of <see cref="PassportNumber"/> among non-deleted passports (trimmed, case-insensitive).
        /// Replaces <see cref="RuleUniqueValueAttribute"/>, which does not align with soft-delete and exact-string semantics.
        /// </summary>
        [RuleFromBoolProperty("Passport_PassportNumberUniqueAmongActive", DefaultContexts.Save, "Another active passport already uses this passport number.")]
        [Browsable(false)]
        public bool IsPassportNumberUniqueAmongActive
        {
            get
            {
                if (string.IsNullOrWhiteSpace(PassportNumber))
                {
                    return true;
                }

                var objectSpace = ObjectSpaceHelper.Get(this);
                if (objectSpace == null)
                {
                    return true;
                }

                var normalized = PassportNumber.Trim().ToUpperInvariant();
                var currentId = ID;

                return !objectSpace.GetObjectsQuery<Passport>()
                    .Where(p => !p.IsDeleted && p.ID != currentId && p.PassportNumber != null)
                    .Any(p => p.PassportNumber.Trim().ToUpper() == normalized);
            }
        }

        /// <summary>
        /// Legacy per-document copy; prefer <see cref="Person.PersonalNumber"/>. Hidden — use Person for edits.
        /// </summary>
        [MaxLength(50)]
        [Browsable(false)]
        public virtual string PersonalNumber { get; set; }
        [RuleRequiredField]
        public virtual PassportType PassportType { get; set; }
        [RuleRequiredField]
        [ImmediatePostData]
        [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
        [ModelDefault("EditMask", "dd.MM.yyyy")]
        public virtual DateTime? IssueDate { get; set; }
        [RuleRequiredField]
        [ImmediatePostData]
        [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
        [ModelDefault("EditMask", "dd.MM.yyyy")]
        public virtual DateTime? ExpirationDate { get; set; }

        [MaxLength(100)]
        [RuleRequiredField]
        public virtual string Authority { get; set; }
        [RuleRequiredField]
        public virtual Country IssuedCountry { get; set; }

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

        [InverseProperty(nameof(PassportImage.Passport))]
        [Aggregated]
        [VisibleInDetailView(false)]
        [VisibleInListView(false)]
        public virtual IList<PassportImage> Images { get; set; }

        /// <summary>Scanned passport files (detail tab <c>Documents</c>); each row is a <see cref="PassportDocument"/> with <see cref="DocumentBase.File"/>.</summary>
        [InverseProperty(nameof(PassportDocument.Passport))]
        [Aggregated]
        public virtual IList<PassportDocument> Documents { get; set; }

        [InverseProperty(nameof(Visa.Passport))]
        [Aggregated]
        public virtual IList<Visa> Visas { get; set; }

        public int DaysRemaining
        {
            get
            {
                if (!ExpirationDate.HasValue)
                {
                    return 0;
                }
                return (ExpirationDate.Value.Date - DateTime.Today).Days;
            }
        }

        /// <summary>Optional; hidden behind detail-view gear when not expanded.</summary>
        [VisibleInListView(false)]
        public virtual bool IsCancelled { get; set; }

        [Browsable(false)]
        public virtual bool IsDeleted { get; set; }

        [Browsable(false)]
        public virtual DateTime? DateDeleted { get; set; }

        [Browsable(false)]
        public virtual ApplicationUser DeletedBy { get; set; }

        [NotMapped]
        [Browsable(false)]
        public int StateSeverityLevel
        {
            get
            {
                var objectSpace = ObjectSpaceHelper.Get(this);
                return objectSpace != null
                    ? (int)PassportStateEvaluator.Evaluate(
                        this,
                        StateEvaluationSettings.FromObjectSpace(objectSpace)
                      ).Severity
                    : 0;
            }
        }

        public override void OnCreated()
        {
            base.OnCreated();
            var objectSpace = ObjectSpaceHelper.Get(this);
            if (objectSpace != null)
            {
                PassportType = objectSpace.GetObjectsQuery<PassportType>().FirstOrDefault(pt => pt.IsDefault);
                IssuedCountry = objectSpace.GetObjectsQuery<Country>().FirstOrDefault(c => c.IsDefault);
            }
        }
    }
}