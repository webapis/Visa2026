using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Main")]
    public class Employee : Person
    {
        public virtual Position Position { get; set; }

        [MaxLength(255)]
        [RuleRegularExpression("EmployeeEmailFormat", @"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$", CustomMessageTemplate = "Invalid email format.")]
        public virtual string Email { get; set; }

        // Relationships defined in Employee.md

        public virtual IList<Visa> Visas { get; set; } = new ObservableCollection<Visa>();

        public virtual IList<WorkPermit> WorkPermits { get; set; } = new ObservableCollection<WorkPermit>();
    }
}