using System;
using System.ComponentModel.DataAnnotations;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    public class WorkPermitDocument : BaseObject
    {
        [RuleRequiredField]
        public virtual FileData File { get; set; }

        [MaxLength(255)]
        public virtual string Description { get; set; }

        [RuleRequiredField]
        public virtual WorkPermit WorkPermit { get; set; }
    }
}