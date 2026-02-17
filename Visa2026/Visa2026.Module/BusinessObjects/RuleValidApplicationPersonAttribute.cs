using System;
using System.Linq;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class RuleValidApplicationPersonAttribute : RuleBaseAttribute
    {
        public RuleValidApplicationPersonAttribute() : base("RuleValidApplicationPerson", DefaultContexts.Save) { }

        protected override bool IsValidInternal(object target, out string errorMessageTemplate)
        {
            if (target is IApplicationItemChild item)
            {
                var personToValidate = item.Person;
                var parentApplication = item.Application;

                // If person or application is not set, other rules (like RuleRequiredField) will handle it.
                if (personToValidate == null || parentApplication == null)
                {
                    errorMessageTemplate = null;
                    return true;
                }

                // Check if the person exists in the Application's ApplicationItems list by comparing IDs.
                if (!parentApplication.ApplicationItems.Any(ai => ai.Person?.ID == personToValidate.ID))
                {
                    errorMessageTemplate = $"The selected person '{personToValidate.FullName}' is not part of the parent application.";
                    return false;
                }
            }

            errorMessageTemplate = null;
            return true;
        }
    }
}