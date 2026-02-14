using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Main")]
    public class Employee : Person
    {
        public virtual Position Position { get; set; }

        public virtual Department Department { get; set; }

        public virtual bool IsSubcontractorEmployee { get; set; }

        [Appearance("SubcontractorVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "!IsSubcontractorEmployee", Context = "DetailView")]
        public virtual Subcontractor Subcontractor { get; set; }

        [MaxLength(255)]
        [RuleRegularExpression("EmployeeEmailFormat", @"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$", CustomMessageTemplate = "Invalid email format.")]
        public virtual string Email { get; set; }

        public virtual Visa CurrentVisa { get; set; }

        public virtual WorkPermit CurrentWorkPermit { get; set; }

        public virtual EmployeePositionHistory CurrentPositionHistory { get; set; }

        // Relationships defined in Employee.md
        
        public virtual IList<Visa> Visas { get; set; } = new ObservableCollection<Visa>();

        public virtual IList<WorkPermit> WorkPermits { get; set; } = new ObservableCollection<WorkPermit>();

        public virtual IList<FamilyMember> FamilyMembers { get; set; } = new ObservableCollection<FamilyMember>();

        public virtual IList<EmployeePositionHistory> PositionHistory { get; set; } = new ObservableCollection<EmployeePositionHistory>();
    }
}