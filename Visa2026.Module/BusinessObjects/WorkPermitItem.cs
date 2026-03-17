using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using DevExpress.ExpressApp.Model;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Employee")]
    [DefaultProperty(nameof(WorkPermitItemName))]
    [RuleCriteria("WorkPermitItem_DateRange", DefaultContexts.Save, "ExpirationDate > StartDate", "Expiration Date must be later than Start Date.")]
    [Appearance("GrayOutIfDeleted", AppearanceItemType = "ViewItem", TargetItems = "*",
        Criteria = "IsDeleted", Context = "ListView", FontColor = "Gray")]
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


        public virtual IList<City> Cities { get; set; } = new ObservableCollection<City>();

        public string WorkPermittedLocations
        {
            get
            {
                if (Cities == null || !Cities.Any())
                {
                    return string.Empty;
                }
                return string.Join(", ", Cities.Select(c => c.Name));
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
    }
}