using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Employee")]
    [DefaultProperty(nameof(Title))]
    public class EmployeePositionHistory : SingleActiveBaseObject<Person, EmployeePositionHistory>, ISoftDelete
    {
        [Index(1)]
        [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
        [ModelDefault("EditMask", "dd.MM.yyyy")]
        public virtual DateTime StartDate { get; set; }

        [Index(2)]
        [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
        [ModelDefault("EditMask", "dd.MM.yyyy")]
        public virtual DateTime? EndDate { get; set; }

        [Index(0)]
        [RuleRequiredField]
        public virtual Position Position { get; set; }

        //[RuleRequiredField]
        [VisibleInListView(false)]
        public virtual Department Department { get; set; }

        [RuleRequiredField]
        [Index(3)]
        [DataSourceCriteria("IsEmployee = true")]
        public virtual Person Person { get; set; }

        [NotMapped]
        [VisibleInListView(false)]
        public string Title => $"{Position?.Name} from {StartDate:d}";

        [Browsable(false)]
        [NotMapped]
        protected override DateTime? ChronologicalSortDate => this.StartDate;

        public override Person GetParent()
        {
            return Person;
        }

        public override IList<EmployeePositionHistory> GetSiblings(Person parent)
        {
            return parent?.PositionHistory;
        }

        public override void SetParentActiveItem(Person parent, EmployeePositionHistory item)
        {
            parent.CurrentPositionHistory = item;
        }

        public override bool IsParentActiveItem(Person parent, EmployeePositionHistory item)
        {
            return parent.CurrentPositionHistory == item;
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

              [Browsable(false)]
        public virtual bool IsDeleted { get; set; }

        [Browsable(false)]
        public virtual DateTime? DateDeleted { get; set; }

        [Browsable(false)]
        public virtual ApplicationUser DeletedBy { get; set; }

    }
}