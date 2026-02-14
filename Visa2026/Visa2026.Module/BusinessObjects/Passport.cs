using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    public class Passport : SingleActiveBaseObject<Person, Passport>
    {
        [MaxLength(20)]
        [RuleRequiredField]
        public virtual string PassportNumber { get; set; }

        public virtual DateTime IssueDate { get; set; }

        public virtual DateTime ExpirationDate { get; set; }

        [MaxLength(100)]
        public virtual string Authority { get; set; }

        [RuleRequiredField]
        public virtual Person Person { get; set; }

        public override Person GetParent()
        {
            return Person;
        }

        public override IList<Passport> GetSiblings(Person parent)
        {
            return parent?.Passports;
        }

        public override void SetParentActiveItem(Person parent, Passport item)
        {
            parent.ActivePassport = item;
            if (item != null)
            {
                parent.PassportNumber = item.PassportNumber;
            }
        }

        public override bool IsParentActiveItem(Person parent, Passport item)
        {
            return parent.ActivePassport == item;
        }
    }
}