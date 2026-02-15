using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using DevExpress.ExpressApp.Model;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [DefaultProperty(nameof(LetterNumber))]
    [NavigationItem("WorkPermit")]
    public class WorkPermitLetter : BaseObject
    {
        [MaxLength(50)]
        [RuleRequiredField]
        public virtual string LetterNumber { get; set; }

        public virtual DateTime LetterDate { get; set; }

        [MaxLength(255)]
        public virtual string Description { get; set; }
    }
}