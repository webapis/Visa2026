using System;
using System.ComponentModel.DataAnnotations;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Application")]
    public class ApplicationProgress : BaseObject
    {
        [RuleRequiredField]
        public virtual Application Application { get; set; }

        [RuleRequiredField]
        public virtual ApplicationState State { get; set; }

        [RuleRequiredField]
        public virtual ApplicationLocation Location { get; set; }

        [RuleRequiredField]
        public virtual DateTime Date { get; set; } = DateTime.Now;

    

        [MaxLength(255)]
        public virtual string Description { get; set; }

        public override void OnSaving()
        {
            base.OnSaving();
            if (Application != null)
            {
                // Ensure the current object is in the parent's collection so it is considered in the UpdateCurrentState calculation.
                if (!Application.ProgressHistory.Contains(this))
                {
                    Application.ProgressHistory.Add(this);
                }
                else
                {
                    Application.UpdateCurrentState();
                }
            }
        }
    }
}