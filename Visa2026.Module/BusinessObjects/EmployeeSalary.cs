using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using Visa2026.Module.Localization;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Employee")]
    [DefaultProperty(nameof(Title))]
    public class EmployeeSalary : SingleActiveBaseObject<Person, EmployeeSalary>, ISoftDelete
    {
        [Index(0)]
        [RuleRequiredField]
        [DataSourceCriteria("IsEmployee = true")]
        public virtual Person Person { get; set; }

        [Index(1)]
        [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
        [ModelDefault("EditMask", "dd.MM.yyyy")]
        public virtual DateTime StartDate { get; set; }

        [Index(2)]
        [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
        [ModelDefault("EditMask", "dd.MM.yyyy")]
        public virtual DateTime? EndDate { get; set; }

        [Index(3)]
        [RuleRequiredField]
        [MaxLength(32)]
        public virtual string Amount { get; set; }

        [Index(4)]
        public virtual EmployeeCurrency Currency { get; set; }

        [NotMapped]
        [VisibleInListView(false)]
        public string Title => VisaUiMessages.Format(
            "EmployeeSalary.DisplayTitle",
            $"{Amount} {Currency}",
            StartDate.ToString("d", CultureInfo.CurrentUICulture));

        [Browsable(false)]
        [NotMapped]
        protected override DateTime? ChronologicalSortDate => this.StartDate;

        public override void OnCreated()
        {
            base.OnCreated();
            StartDate = DateTime.Today;
        }

        protected override void UpdateActiveState()
        {
            if (IsActive)
            {
                var parent = GetParent();
                if (parent != null)
                {
                    var siblings = GetSiblings(parent);
                    if (siblings != null)
                    {
                        foreach (var sibling in siblings)
                        {
                            if (sibling != this && sibling.IsActive)
                            {
                                sibling.EndDate = StartDate;
                            }
                        }
                    }
                }
            }
            base.UpdateActiveState();
        }

        public override Person GetParent()
        {
            return Person;
        }

        public override IList<EmployeeSalary> GetSiblings(Person parent)
        {
            return parent?.Salaries;
        }

        public override void SetParentActiveItem(Person parent, EmployeeSalary item)
        {
            parent.CurrentSalary = item;
        }

        public override bool IsParentActiveItem(Person parent, EmployeeSalary item)
        {
            return parent.CurrentSalary == item;
        }

        [Browsable(false)]
        public virtual bool IsDeleted { get; set; }

        [Browsable(false)]
        public virtual DateTime? DateDeleted { get; set; }

        [Browsable(false)]
        public virtual ApplicationUser DeletedBy { get; set; }
    }
}
