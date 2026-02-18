using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Application")]
    [DefaultProperty(nameof(State))]
    [RuleCriteria("ApplicationProgressDateNotInFuture", DefaultContexts.Save, "Date <= Now()", "Date cannot be in the future.")]
    public class ApplicationProgress : BaseObject
    {
        [RuleRequiredField]
        public virtual Application Application { get; set; }

        [RuleRequiredField]
        public virtual ApplicationState State { get; set; }

        [RuleRequiredField]
        public virtual ApplicationLocation Location { get; set; }

        [RuleRequiredField]
        public virtual DateTime Date { get; set; }

    

        [MaxLength(255)]
        public virtual string Description { get; set; }

        public override void OnCreated()
        {
            base.OnCreated();
            Date = DateTime.Now;
        }

        public override void OnSaving()
        {
            base.OnSaving();
            if (Application != null)
            {
                // Pass the current object to ensure it is included in the calculation even if not yet in the collection.
                Application.UpdateCurrentState(this);
            }
        }
    }
}