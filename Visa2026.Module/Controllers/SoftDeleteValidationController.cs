using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Validation;
using DevExpress.Persistent.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Controllers
{
    /// <summary>
    /// Skips <see cref="RuleRequiredField"/> checks when the target implements
    /// <see cref="ISoftDelete"/> and <see cref="ISoftDelete.IsDeleted"/> is true
    /// (including rules inherited from non-soft-delete base classes).
    /// </summary>
    public sealed class SoftDeleteValidationController : WindowController
    {
        private readonly IOptionsSnapshot<ValidationOptions>? validationOptions;

        public SoftDeleteValidationController()
        {
            TargetWindowType = WindowType.Main;
        }

        [ActivatorUtilitiesConstructor]
        public SoftDeleteValidationController(IOptionsSnapshot<ValidationOptions> validationOptions)
            : this()
        {
            this.validationOptions = validationOptions;
        }

        protected override void OnActivated()
        {
            base.OnActivated();
            if (validationOptions?.Value.Events != null)
                validationOptions.Value.Events.OnCustomNeedToValidateRule += OnCustomNeedToValidateRule;
        }

        protected override void OnDeactivated()
        {
            if (validationOptions?.Value.Events != null)
                validationOptions.Value.Events.OnCustomNeedToValidateRule -= OnCustomNeedToValidateRule;
            base.OnDeactivated();
        }

        private static void OnCustomNeedToValidateRule(CustomNeedToValidateRuleContext context)
        {
            if (context.Handled)
                return;

            if (context.Target is not ISoftDelete { IsDeleted: true })
                return;

            if (context.Rule.Properties is not RuleRequiredFieldProperties)
                return;

            context.NeedToValidateRule = false;
            context.Handled = true;
        }
    }
}
