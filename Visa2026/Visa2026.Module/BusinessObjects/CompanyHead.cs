using System.Collections.Generic;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using System.ComponentModel;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [DisplayName("Authorized Signatory")]
    public class CompanyHead : SingleActiveBaseObject<Company, CompanyHead>
    {
        public virtual Company Company { get; set; }

        [RuleRequiredField(DefaultContexts.Save)]
        public virtual Employee Employee { get; set; }

        public virtual Position Position { get; set; }

        public override Company GetParent()
        {
            return Company;
        }

        public override IList<CompanyHead> GetSiblings(Company parent)
        {
            return parent?.Heads;
        }

        public override void SetParentActiveItem(Company parent, CompanyHead item)
        {
            parent.CurrentAuthorizedSignatory = item;
        }

        public override bool IsParentActiveItem(Company parent, CompanyHead item)
        {
            return parent.CurrentAuthorizedSignatory == item;
        }
    }
}