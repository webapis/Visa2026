using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel;
using System.Linq;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using DevExpress.ExpressApp.Model;
using Visa2026.Module.Services.StateEvaluation;
using Visa2026.Module.Services.StateEvaluation.Evaluators;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Employee")]
    [DefaultProperty(nameof(WorkPermitItemName))]
    [RuleCriteria("WorkPermitItem_DateRange", DefaultContexts.Save, "ExpirationDate > StartDate", "Expiration Date must be later than Start Date.")]
    [Appearance("GrayOutIfDeleted", AppearanceItemType = "ViewItem", TargetItems = "*",
        Criteria = "IsDeleted", Context = "ListView", FontColor = "Gray")]
    [Appearance("WPStateInfo", Priority = 100, AppearanceItemType = "ViewItem", TargetItems = "*",
        Criteria = "IsDeleted = false And StateSeverityLevel = 1", Context = "ListView", BackColor = "LightSkyBlue")]
    [Appearance("WPStateWarning", Priority = 200, AppearanceItemType = "ViewItem", TargetItems = "*",
        Criteria = "IsDeleted = false And StateSeverityLevel = 2", Context = "ListView", BackColor = "LightSalmon")]
    [Appearance("WPStateCritical", Priority = 300, AppearanceItemType = "ViewItem", TargetItems = "*",
        Criteria = "IsDeleted = false And StateSeverityLevel >= 3", Context = "ListView", BackColor = "LightCoral")]
    public class WorkPermitItem : SingleActiveBaseObject<Person, WorkPermitItem>, IExpirationLogic, ISoftDelete
    {
        [RuleRequiredField]
        [DataSourceCriteria("IsEmployee = true")]
        public virtual Person Person { get; set; }

        [RuleRequiredField]
        public virtual Passport Passport { get; set; }

        [RuleRequiredField]
        public virtual EmployeePositionHistory CurrentPositionHistory { get; set; }

        [RuleRequiredField]
        public virtual DateTime StartDate { get; set; }

        [RuleRequiredField]
        public virtual DateTime ExpirationDate { get; set; }

        [RuleRequiredField]
        public virtual string WorkPermitNumber { get; set; }

        

        public virtual string ASNumber { get; set; }

        public virtual WorkPermit WorkPermit { get; set; }


        public virtual IList<City> WorkPermitedCities { get; set; } = new ObservableCollection<City>();

        public string WorkPermittedLocations
        {
            get
            {
                if (WorkPermitedCities == null || !WorkPermitedCities.Any())
                {
                    return string.Empty;
                }
                return string.Join(", ", WorkPermitedCities.Select(c => c.Name));
            }
        }


        [RuleFromBoolProperty("WorkPermitItem_EmployeeIsValid", DefaultContexts.Save, "The selected employee is not part of the parent application.")]
        [Browsable(false)]
        public bool IsEmployeeValid
        {
            get
            {
                if (Person == null || WorkPermit?.Application == null) return true;
                return WorkPermit.Application.ApplicationItems.Any(ai => ai.Person?.ID == Person.ID);
            }
        }

        public override Person GetParent()
        {
            return Person;
        }

        public override IList<WorkPermitItem> GetSiblings(Person parent)
        {
            return parent?.WorkPermitItems;
        }

        public override void SetParentActiveItem(Person parent, WorkPermitItem item)
        {
            parent.CurrentWorkPermitItem = item;
        }

        public override bool IsParentActiveItem(Person parent, WorkPermitItem item)
        {
            return parent.CurrentWorkPermitItem == item;
        }

        DateTime? IExpirationLogic.ExpirationDate => ExpirationDate;

        public int DaysRemaining
        {
            get
            {
                return (ExpirationDate.Date - DateTime.Today).Days;
            }
        }

        public ExpirationState ExpirationState
        {
            get
            {
                return ExpirationLogicHelper.CalculateExpirationState(this, StartDate, ObjectSpace);
            }
        }

        public string WorkPermitItemName => $"{Person?.FullName} - {WorkPermitNumber}";

        #region Report [NotMapped] properties — WorkPermitListReport fields
        [NotMapped, VisibleInDetailView(false), VisibleInListView(false)]
        public string Person_FullName => Person?.FullName;

        [NotMapped, VisibleInDetailView(false), VisibleInListView(false)]
        public string Person_DateOfBirthText => Person?.DateOfBirth is DateTime dob ? dob.ToString("dd.MM.yyyy") : string.Empty;

        [NotMapped, VisibleInDetailView(false), VisibleInListView(false)]
        public string Person_NationalityCode => Person?.Nationality?.Code;

        [NotMapped, VisibleInDetailView(false), VisibleInListView(false)]
        public string Education_LevelTm => Person?.CurrentEducation?.EducationLevel?.NameTm;

        [NotMapped, VisibleInDetailView(false), VisibleInListView(false)]
        public string Passport_Number => Passport?.PassportNumber;

        [NotMapped, VisibleInDetailView(false), VisibleInListView(false)]
        public string Passport_ExpirationDateText => Passport?.ExpirationDate is DateTime exp ? exp.ToString("dd.MM.yyyy") : string.Empty;

        [NotMapped, VisibleInDetailView(false), VisibleInListView(false)]
        public string Position_NameTm => CurrentPositionHistory?.Position?.NameTm;

        [NotMapped, VisibleInDetailView(false), VisibleInListView(false)]
        public string Address_FullAddress => Person?.CurrentAddressOfResidence?.FullAddress;

        [NotMapped, VisibleInDetailView(false), VisibleInListView(false)]
        public string WP_StartDateText => StartDate.ToString("dd.MM.yyyy");

        [NotMapped, VisibleInDetailView(false), VisibleInListView(false)]
        public string WP_ExpirationDateText => ExpirationDate.ToString("dd.MM.yyyy");

        [NotMapped, VisibleInDetailView(false), VisibleInListView(false)]
        public string Visa_Number => Person?.CurrentVisa?.VisaNumber;

        [NotMapped, VisibleInDetailView(false), VisibleInListView(false)]
        public string Visa_StartDateText => Person?.CurrentVisa?.StartDate is DateTime vs ? vs.ToString("dd.MM.yyyy") : string.Empty;

        [NotMapped, VisibleInDetailView(false), VisibleInListView(false)]
        public string Visa_ExpirationDateText => Person?.CurrentVisa?.ExpirationDate is DateTime ve ? ve.ToString("dd.MM.yyyy") : string.Empty;

        [NotMapped, VisibleInDetailView(false), VisibleInListView(false)]
        public string Company_Name => WorkPermit?.Application?.Company?.Name;

        [NotMapped, VisibleInDetailView(false), VisibleInListView(false)]
        public string CompanyHead_PositionTm => WorkPermit?.Application?.CompanyHead?.Position?.NameTm;

        [NotMapped, VisibleInDetailView(false), VisibleInListView(false)]
        public string CompanyHead_FullName => WorkPermit?.Application?.CompanyHead?.FullName;
        #endregion

        public override void OnSaving()
        {
            base.OnSaving();
            CrossObjectSyncHelper.SyncOnSave(this);
        }

		public virtual bool IsCancelled { get; set; }

		public virtual bool IsChanged { get; set; }
        
        public virtual bool IsExtended { get; set; }

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
                ? (int)WorkPermitItemStateEvaluator.Evaluate(
                    this,
                    StateEvaluationSettings.FromSystemSettings(SystemSettings.TryGetInstance(ObjectSpace))
                  ).Severity
                : 0;
    }
}