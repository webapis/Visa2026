using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using DevExpress.ExpressApp.Model;
using System.ComponentModel;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.DC;
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
    public class Passport : SingleActiveBaseObject<Person, Passport>, IExpirationLogic, ISoftDelete
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

                if (ObjectSpace == null)
                {
                    return true;
                }

                var normalized = PassportNumber.Trim().ToUpperInvariant();
                var currentId = ID;

                return !ObjectSpace.GetObjectsQuery<Passport>()
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

        [ModelDefault("AllowEdit", "False")]
        public virtual Visa CurrentVisa { get; set; }



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

        public override Person GetParent()
        {
            return Person;
        }

        public override IList<Passport> GetSiblings(Person parent)
        {
            return parent?.Passports;
        }

        public override void SetParentActiveItem(Person parent, Passport item)
        {
            parent.CurrentPassport = item;
        }

        public override bool IsParentActiveItem(Person parent, Passport item)
        {
            return parent.CurrentPassport == item;
        }

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

        public ExpirationState ExpirationState
        {
            get
            {
                return ExpirationLogicHelper.CalculateExpirationState(this, IssueDate, ObjectSpace);
            }
        }

        [Browsable(false)]
        [NotMapped]
        protected override DateTime? ChronologicalSortDate => this.IssueDate;

        [Browsable(false)]
        public virtual bool IsDeleted { get; set; }

        [Browsable(false)]
        public virtual DateTime? DateDeleted { get; set; }

        [Browsable(false)]
        public virtual ApplicationUser DeletedBy { get; set; }

        [NotMapped]
        [Browsable(false)]
        public int StateSeverityLevel =>
            ObjectSpace != null
                ? (int)PassportStateEvaluator.Evaluate(
                    this,
                    StateEvaluationSettings.FromSystemSettings(SystemSettings.TryGetInstance(ObjectSpace))
                  ).Severity
                : 0;

        public override void OnCreated()
        {
            base.OnCreated();
            if (ObjectSpace != null)
            {
                PassportType = ObjectSpace.GetObjectsQuery<PassportType>().FirstOrDefault(pt => pt.IsDefault);
                IssuedCountry = ObjectSpace.GetObjectsQuery<Country>().FirstOrDefault(c => c.IsDefault);
            }
        }
    }
}