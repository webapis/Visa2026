using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("FamilyMember")]
    public class FamilyMember : Person
    {
        [RuleRequiredField]
        public virtual Employee Employee { get; set; }

        [RuleRequiredField]
        public virtual Relationship Relationship { get; set; }

        [InverseProperty(nameof(FamilyMemberImage.FamilyMember))]
        [Aggregated]
        public virtual IList<FamilyMemberImage> Images { get; set; } = new ObservableCollection<FamilyMemberImage>();
    }
}