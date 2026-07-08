using System;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.Persistent.Base;
using Microsoft.Extensions.DependencyInjection;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;
using Visa2026.Module.Services.PreviewSlot;
using Visa2026.Module.Services.PreviewSlot;
using Visa2026.Module.Services.UserReports;

namespace Visa2026.Module.Controllers
{
    /// <summary>
    /// Controller for UserReportTemplate DetailView.
    /// Provides actions to extract and validate placeholders when a .docx file is uploaded.
    /// </summary>
    public class UserReportTemplateController : ViewController<DetailView>
    {
        private SimpleAction _extractPlaceholdersAction;
        private SimpleAction _validatePlaceholdersAction;
        private SimpleAction _placeholderManualAction;

        public UserReportTemplateController()
        {
            TargetObjectType = typeof(UserReportTemplate);
            TargetViewType = ViewType.DetailView;

            _extractPlaceholdersAction = new SimpleAction(this, "ExtractPlaceholders", PredefinedCategory.Edit);
            _extractPlaceholdersAction.ImageName = "Action_Find";
            _extractPlaceholdersAction.Execute += ExtractPlaceholdersAction_Execute;

            _validatePlaceholdersAction = new SimpleAction(this, "ValidatePlaceholders", PredefinedCategory.Edit);
            _validatePlaceholdersAction.ImageName = "Action_Validation";
            _validatePlaceholdersAction.Execute += ValidatePlaceholdersAction_Execute;

            _placeholderManualAction = new SimpleAction(this, "OpenPlaceholderManual", PredefinedCategory.View);
            _placeholderManualAction.ImageName = "Action_Help";
            _placeholderManualAction.Execute += PlaceholderManualAction_Execute;
        }

        protected override void OnActivated()
        {
            base.OnActivated();
            _placeholderManualAction.Caption = VisaUiMessages.Get("UserReport.PlaceholderManual");
            UpdateActionStates();
            View.CurrentObjectChanged += (_, _) => UpdateActionStates();
        }

        private void UpdateActionStates()
        {
            var template = View?.CurrentObject as UserReportTemplate;
            var hasFile = template?.TemplateFile != null;
            var canEdit = UserReportTemplateEditAccess.CanEditTemplates();

            _extractPlaceholdersAction.Enabled["NoFile"] = hasFile;
            _extractPlaceholdersAction.Enabled["NoWrite"] = canEdit;
            _validatePlaceholdersAction.Enabled["NoFile"] = hasFile;
            _validatePlaceholdersAction.Enabled["NoWrite"] = canEdit;
        }

        private async void ExtractPlaceholdersAction_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
            var templateShell = (UserReportTemplate)e.CurrentObject;
            if (!TryEnsureTemplateEditAccess())
                return;

            if (templateShell?.TemplateFile == null)
            {
                Application.ShowViewStrategy.ShowMessage(VisaUiMessages.Get("UserReport.UploadTemplateFirst"), InformationType.Warning);
                return;
            }

            try
            {
                var templateId = (Guid)ObjectSpace.GetKeyValue(templateShell);
                var maintenance = Application.ServiceProvider.GetRequiredService<IUserReportTemplateMaintenanceService>();
                var result = await maintenance.ExtractPlaceholdersAsync(templateId).ConfigureAwait(true);

                ObjectSpace.Refresh();

                if (!result.Success)
                {
                    Application.ShowViewStrategy.ShowMessage(
                        VisaUiMessages.Format("UserReport.ExtractError", result.ErrorMessage ?? "Unknown error"),
                        InformationType.Error);
                    return;
                }

                Application.ShowViewStrategy.ShowMessage(
                    VisaUiMessages.Format("UserReport.ExtractedPlaceholders", result.PlaceholderCount),
                    InformationType.Success);
            }
            catch (Exception ex)
            {
                Application.ShowViewStrategy.ShowMessage(
                    VisaUiMessages.Format("UserReport.ExtractError", ex.Message),
                    InformationType.Error);
            }
        }

        private async void ValidatePlaceholdersAction_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
            var templateShell = (UserReportTemplate)e.CurrentObject;
            if (!TryEnsureTemplateEditAccess())
                return;

            if (templateShell?.Placeholders?.Any() != true)
            {
                Application.ShowViewStrategy.ShowMessage(VisaUiMessages.Get("UserReport.NoPlaceholdersToValidate"), InformationType.Warning);
                return;
            }

            try
            {
                var templateId = (Guid)ObjectSpace.GetKeyValue(templateShell);
                var maintenance = Application.ServiceProvider.GetRequiredService<IUserReportTemplateMaintenanceService>();
                var result = await maintenance.ValidatePlaceholdersAsync(templateId).ConfigureAwait(true);

                ObjectSpace.Refresh();

                if (!result.Success)
                {
                    Application.ShowViewStrategy.ShowMessage(
                        VisaUiMessages.Format("UserReport.ValidateError", result.ErrorMessage ?? "Unknown error"),
                        InformationType.Error);
                    return;
                }

                if (result.InvalidCount == 0)
                {
                    Application.ShowViewStrategy.ShowMessage(
                        VisaUiMessages.Format("UserReport.AllPlaceholdersValid", result.ValidCount),
                        InformationType.Success);
                }
                else
                {
                    Application.ShowViewStrategy.ShowMessage(
                        VisaUiMessages.Format("UserReport.SomePlaceholdersInvalid", result.ValidCount, result.InvalidCount),
                        InformationType.Warning);
                }
            }
            catch (Exception ex)
            {
                Application.ShowViewStrategy.ShowMessage(
                    VisaUiMessages.Format("UserReport.ValidateError", ex.Message),
                    InformationType.Error);
            }
        }

        private void PlaceholderManualAction_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
            var template = (UserReportTemplate)e.CurrentObject;
            var slotService = Application.ServiceProvider.GetService<IVisaPreviewSlotService>();
            if (slotService == null)
            {
                Application.ShowViewStrategy.ShowMessage(
                    VisaUiMessages.Get("PlaceholderManual.Title"),
                    InformationType.Warning);
                return;
            }

            slotService.OpenPlaceholderManualAsync(
                new PlaceholderManualSlotRequest { FilterRootBoType = template?.RootBoType },
                VisaPreviewSlotViewHelper.ResolveOwnerViewId(View)).GetAwaiter().GetResult();
        }

        private bool TryEnsureTemplateEditAccess()
        {
            if (UserReportTemplateEditAccess.CanEditTemplates())
                return true;

            Application.ShowViewStrategy.ShowMessage(
                VisaUiMessages.Get("UserReport.TemplateEditDenied"),
                InformationType.Error);
            return false;
        }
    }
}
