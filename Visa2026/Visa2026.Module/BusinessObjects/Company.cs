using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    public class Company : BaseObject
    {
        public Company()
        {
            Heads = new ObservableCollection<CompanyHead>();
        }

        [RuleRequiredField(DefaultContexts.Save)]
        public virtual string Name { get; set; }

        public virtual string Address { get; set; }

        public virtual string PhoneNumber { get; set; }

        public virtual string Email { get; set; }

        public virtual string TaxInformation { get; set; }

        [Aggregated]
        [InverseProperty(nameof(CompanyHead.Company))]
        public virtual IList<CompanyHead> Heads { get; set; }

        public virtual CompanyHead DefaultAuthorized { get; set; }
    }
}