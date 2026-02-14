using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    public class PersonInApplication : BaseObject
    {
        [RuleRequiredField]
        public virtual Application Application { get; set; }

        [RuleRequiredField]
        public virtual Person Person { get; set; }

        public virtual Invitation Invitation { get; set; }

        public virtual Rejection Rejection { get; set; }

        public virtual CheckPoint CheckPoint { get; set; }

        public virtual VisaIssuedPlace VisaIssuedPlace { get; set; }

        public virtual PurposeOfTravel PurposeOfTravel { get; set; }
    }
}