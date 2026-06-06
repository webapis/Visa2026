using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.Persistent.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ExcelReports;
using Visa2026.Module.Localization;
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

        public UserReportTemplateController()
        {
            TargetObjectType = typeof(UserReportTemplate);
            TargetViewType = ViewType.DetailView;

            // Extract placeholders action
            _extractPlaceholdersAction = new SimpleAction(this, "ExtractPlaceholders", PredefinedCategory.Edit);
            _extractPlaceholdersAction.ImageName = "Action_Find";
            _extractPlaceholdersAction.Execute += ExtractPlaceholdersAction_Execute;

            // Validate placeholders action
            _validatePlaceholdersAction = new SimpleAction(this, "ValidatePlaceholders", PredefinedCategory.Edit);
            _validatePlaceholdersAction.ImageName = "Action_Validation";
            _validatePlaceholdersAction.Execute += ValidatePlaceholdersAction_Execute;
        }

        protected override void OnActivated()
        {
            base.OnActivated();
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
                var content = templateShell.TemplateFile.Content;
                if (content == null || content.Length == 0)
                {
                    Application.ShowViewStrategy.ShowMessage(VisaUiMessages.Get("UserReport.TemplateFileEmpty"), InformationType.Warning);
                    return;
                }

                using var fileStream = new MemoryStream(content, 0, content.Length, writable: false, publiclyVisible: true);
                IList<string> placeholders;
                if (templateShell.GetEffectiveOutputFormat() == TemplateOutputFormat.Excel)
                {
                    var extractor = Application.ServiceProvider.GetRequiredService<IExcelTemplatePlaceholderExtractor>();
                    placeholders = await extractor.ExtractPlaceholdersAsync(fileStream);
                }
                else
                {
                    var extractor = Application.ServiceProvider.GetRequiredService<IUserReportPlaceholderExtractor>();
                    placeholders = await extractor.ExtractPlaceholdersAsync(fileStream);
                }

                var templateId = (Guid)ObjectSpace.GetKeyValue(templateShell);
                using (var os = CreateTemplateMaintenanceObjectSpace())
                {
                    var template = LoadTemplateForMaintenance(os, templateId);
                    if (template == null)
                    {
                        Application.ShowViewStrategy.ShowMessage(VisaUiMessages.Get("UserReport.TemplateNotFound"), InformationType.Error);
                        return;
                    }

                    // Do not call ObservableCollection.Clear() — EF Core change tracking rejects the Reset notification.
                    foreach (var existing in template.Placeholders.ToList())
                        os.Delete(existing);

                    foreach (var placeholder in placeholders)
                    {
                        var placeholderObj = os.CreateObject<UserReportPlaceholder>();
                        placeholderObj.Template = template;
                        placeholderObj.PlaceholderKey = placeholder;
                        placeholderObj.IsValid = false;
                        template.Placeholders.Add(placeholderObj);
                    }

                    os.CommitChanges();
                }

                ObjectSpace.Refresh();

                Application.ShowViewStrategy.ShowMessage(
                    VisaUiMessages.Format("UserReport.ExtractedPlaceholders", placeholders.Count),
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
                var placeholderKeys = templateShell.Placeholders.Select(p => p.PlaceholderKey).ToList();
                IList<PlaceholderValidationResult> validationResults;
                if (templateShell.GetEffectiveOutputFormat() == TemplateOutputFormat.Excel)
                {
                    var validationService = Application.ServiceProvider.GetRequiredService<IExcelReportValidationService>();
                    validationResults = await validationService.ValidatePlaceholdersAsync(
                        placeholderKeys, templateShell.RootBoType, templateShell.ExcelMergeMode);
                }
                else
                {
                    var validationService = Application.ServiceProvider.GetRequiredService<IUserReportValidationService>();
                    validationResults = await validationService.ValidatePlaceholdersAsync(placeholderKeys, templateShell.RootBoType);
                }

                var templateId = (Guid)ObjectSpace.GetKeyValue(templateShell);
                using (var os = CreateTemplateMaintenanceObjectSpace())
                {
                    var template = LoadTemplateForMaintenance(os, templateId);
                    if (template == null)
                    {
                        Application.ShowViewStrategy.ShowMessage(VisaUiMessages.Get("UserReport.TemplateNotFound"), InformationType.Error);
                        return;
                    }

                    foreach (var placeholder in template.Placeholders)
                    {
                        var result = validationResults.FirstOrDefault(r => r.PlaceholderKey == placeholder.PlaceholderKey);
                        if (result != null)
                        {
                            placeholder.IsValid = result.IsValid;
                            placeholder.ResolvedPropertyPath = result.ResolvedPath;
                            placeholder.ExampleValue = result.ExampleValue;
                            placeholder.ValidationError = result.ErrorMessage;
                        }
                    }

                    os.CommitChanges();
                }

                ObjectSpace.Refresh();

                var validCount = validationResults.Count(r => r.IsValid);
                var invalidCount = validationResults.Count(r => !r.IsValid);

                if (invalidCount == 0)
                {
                    Application.ShowViewStrategy.ShowMessage(
                        VisaUiMessages.Format("UserReport.AllPlaceholdersValid", validCount),
                        InformationType.Success);
                }
                else
                {
                    Application.ShowViewStrategy.ShowMessage(
                        VisaUiMessages.Format("UserReport.SomePlaceholdersInvalid", validCount, invalidCount),
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

        /// <summary>
        /// Placeholder extract/validate replaces child rows (delete + create). Officers need template write;
        /// use a non-secured object space so stale <see cref="UserReportPlaceholder"/> role grants cannot block maintenance.
        /// </summary>
        private IObjectSpace CreateTemplateMaintenanceObjectSpace()
        {
            var factory = Application.ServiceProvider.GetRequiredService<INonSecuredObjectSpaceFactory>();
            return factory.CreateNonSecuredObjectSpace<UserReportTemplate>();
        }

        private static UserReportTemplate? LoadTemplateForMaintenance(IObjectSpace objectSpace, Guid templateId) =>
            objectSpace.GetObjectsQuery<UserReportTemplate>()
                .Include(t => t.TemplateFile)
                .Include(t => t.Placeholders)
                .FirstOrDefault(t => t.ID == templateId);

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
