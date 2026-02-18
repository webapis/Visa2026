using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using DevExpress.ExpressApp.DC;
namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Employee")]
    public class Employee : Person
    {
        public virtual Position Position { get; set; }

        public virtual Department Department { get; set; }

        public virtual bool IsSubcontractorEmployee { get; set; }

        [Appearance("SubcontractorVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "!IsSubcontractorEmployee", Context = "DetailView")]
        public virtual Subcontractor Subcontractor { get; set; }

        [MaxLength(255)]
        [RuleRegularExpression("EmployeeEmailFormat", DefaultContexts.Save, @"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$", CustomMessageTemplate = "Invalid email format.")]
        public virtual string Email { get; set; }

        public virtual WorkPermit CurrentWorkPermit { get; set; }

        public virtual EmployeePositionHistory CurrentPositionHistory { get; set; }

        public virtual DateTime HireDate { get; set; }

        public virtual ProjectContract ProjectContract { get; set; }

        // Relationships defined in Employee.md
        
        [InverseProperty(nameof(WorkPermit.Employee))]
        [Aggregated]
        public virtual IList<WorkPermit> WorkPermits { get; set; } = new ObservableCollection<WorkPermit>();

        [InverseProperty(nameof(FamilyMember.Employee))]
            [Aggregated]
        public virtual IList<FamilyMember> FamilyMembers { get; set; } = new ObservableCollection<FamilyMember>();

        [InverseProperty(nameof(EmployeePositionHistory.Employee))]
        [Aggregated]
        public virtual IList<EmployeePositionHistory> PositionHistory { get; set; } = new ObservableCollection<EmployeePositionHistory>();
    }
}