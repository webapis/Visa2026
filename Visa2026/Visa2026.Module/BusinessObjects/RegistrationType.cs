using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [DefaultProperty(nameof(Name))]
    [NavigationItem("Lookup/Registration")]
    public class RegistrationType : BaseObject
    {
        [MaxLength(100)]
        [RuleRequiredField]
        [RuleUniqueValue]
        public virtual string Name { get; set; }

        public virtual IList<RegistrationReason> RegistrationReasons { get; set; } = new ObservableCollection<RegistrationReason>();
    }
}