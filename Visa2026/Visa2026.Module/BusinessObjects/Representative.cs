using System.Collections.Generic;
using System.ComponentModel;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [DisplayName("Authorized Representative")]
    [RuleCriteria("RepresentativeSource", DefaultContexts.Save, "LocalEmployee is not null or Employee is not null", "Please select a Local Employee or an Expat Employee.")]
    public class Representative : SingleActiveBaseObject<Company, Representative>
    {
        public virtual Company Company { get; set; }

        public virtual LocalEmployee LocalEmployee { get; set; }

        public override Company GetParent()
        {
            return Company;
        }

        public override IList<Representative> GetSiblings(Company parent)
        {
            return parent?.Representatives;
        }

        public override void SetParentActiveItem(Company parent, Representative item)
        {
            parent.CurrentRepresentative = item;
        }

        public override bool IsParentActiveItem(Company parent, Representative item)
        {
            return parent.CurrentRepresentative == item;
        }
    }
}