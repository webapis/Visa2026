using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    /// <summary>Legacy local employee roster. Signatory/representative data lives on singleton BOs.</summary>
    [Obsolete("Roster retired; use AuthorizedSignatory and AuthorizedRepresentative. See docs/ORGANIZATION_SINGLETON_REFACTOR_PLAN.md.")]
    [DefaultClassOptions]
    [NavigationItem(false)]
    [DefaultProperty(nameof(FullName))]
    public class LocalEmployee : BaseObject, ISoftDelete
    {
        public virtual Company Company { get; set; }

        [MaxLength(100)]
        [RuleRequiredField]
        public virtual string FirstName { get; set; }

        [MaxLength(100)]
        [RuleRequiredField]
        public virtual string LastName { get; set; }

        [MaxLength(100)] 
        public virtual string MiddleName { get; set; }

        public string FullName => string.Join(" ", new[] { FirstName, MiddleName, LastName }.Where(s => !string.IsNullOrEmpty(s)));

        public virtual DateTime BirthDate { get; set; }

        public virtual Position Position { get; set; }

        public virtual Department Department { get; set; }

        public virtual DateTime HireDate { get; set; }

        [MaxLength(255)]
        [RuleRegularExpression("LocalEmployeeEmailFormat", DefaultContexts.Save, @"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$", CustomMessageTemplate = "Invalid email format.")]
        public virtual string Email { get; set; }

        [Browsable(false)]
        public virtual bool IsDeleted { get; set; }

        [Browsable(false)]
        public virtual DateTime? DateDeleted { get; set; }

        [Browsable(false)]
        public virtual ApplicationUser DeletedBy { get; set; }
    }
}