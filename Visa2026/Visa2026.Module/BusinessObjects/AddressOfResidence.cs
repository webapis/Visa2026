using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    public class AddressOfResidence : SingleActiveBaseObject<Person, AddressOfResidence>
    {
        public virtual Region Region { get; set; }

        [MaxLength(255)]
        [RuleRequiredField]
        public virtual string AddressLine { get; set; }

        public virtual DateTime StartDate { get; set; }

        public virtual DateTime? EndDate { get; set; }

        [RuleRequiredField]
        public virtual Person Person { get; set; }

        public override Person GetParent()
        {
            return Person;
        }

        public override IList<AddressOfResidence> GetSiblings(Person parent)
        {
            return parent?.AddressesOfResidence;
        }

        public override void SetParentActiveItem(Person parent, AddressOfResidence item)
        {
            parent.CurrentAddressOfResidence = item;
        }

        public override bool IsParentActiveItem(Person parent, AddressOfResidence item)
        {
            return parent.CurrentAddressOfResidence == item;
        }
    }
}