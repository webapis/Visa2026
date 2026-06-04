using System.ComponentModel;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [DisplayName("Image")]
    [NavigationItem(false)]
    public class FamilyMemberImage : ImageBase
    {
        [RuleRequiredField]
        [DataSourceCriteria("PersonRole = ##Enum#Visa2026.Module.BusinessObjects.PersonRecordRole,FamilyMember#")]
        public virtual Person Person { get; set; }
    }
}