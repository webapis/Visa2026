using System.IO;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.Persistent.Base;
using Microsoft.Extensions.DependencyInjection;
using Visa2026.Module.BusinessObjects;
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
            _extractPlaceholdersAction.Caption = "Extract Placeholders";
            _extractPlaceholdersAction.ImageName = "Action_Find";
            _extractPlaceholdersAction.Execute += ExtractPlaceholdersAction_Execute;

            // Validate placeholders action
            _validatePlaceholdersAction = new SimpleAction(this, "ValidatePlaceholders", PredefinedCategory.Edit);
            _validatePlaceholdersAction.Caption = "Validate Placeholders";
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

            _extractPlaceholdersAction.Enabled["NoFile"] = hasFile;
            _validatePlaceholdersAction.Enabled["NoFile"] = hasFile;
        }

        private async void ExtractPlaceholdersAction_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
            var template = (UserReportTemplate)e.CurrentObject;
            if (template?.TemplateFile == null)
            {
                Application.ShowViewStrategy.ShowMessage("Please upload a template file first.", InformationType.Warning);
                return;
            }

            try
            {
                var extractor = Application.ServiceProvider.GetRequiredService<IUserReportPlaceholderExtractor>();

                var content = template.TemplateFile.Content;
                if (content == null || content.Length == 0)
                {
                    Application.ShowViewStrategy.ShowMessage("Template file is empty or not loaded.", InformationType.Warning);
                    return;
                }

                using var fileStream = new MemoryStream(content, 0, content.Length, writable: false, publiclyVisible: true);
                var placeholders = await extractor.ExtractPlaceholdersAsync(fileStream);

                // Clear existing placeholders
                template.Placeholders.Clear();

                // Add extracted placeholders
                foreach (var placeholder in placeholders)
                {
                    var placeholderObj = ObjectSpace.CreateObject<UserReportPlaceholder>();
                    placeholderObj.Template = template;
                    placeholderObj.PlaceholderKey = placeholder;
                    placeholderObj.IsValid = false; // Will be validated separately
                    template.Placeholders.Add(placeholderObj);
                }

                ObjectSpace.CommitChanges();

                Application.ShowViewStrategy.ShowMessage(
                    $"Extracted {placeholders.Count} placeholder(s). Run validation to check against BO properties.",
                    InformationType.Success);
            }
            catch (Exception ex)
            {
                Application.ShowViewStrategy.ShowMessage(
                    $"Error extracting placeholders: {ex.Message}",
                    InformationType.Error);
            }
        }

        private async void ValidatePlaceholdersAction_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
            var template = (UserReportTemplate)e.CurrentObject;
            if (template?.Placeholders?.Any() != true)
            {
                Application.ShowViewStrategy.ShowMessage("No placeholders to validate. Extract placeholders first.", InformationType.Warning);
                return;
            }

            try
            {
                var validationService = Application.ServiceProvider.GetRequiredService<IUserReportValidationService>();

                var placeholderKeys = template.Placeholders.Select(p => p.PlaceholderKey).ToList();
                var validationResults = await validationService.ValidatePlaceholdersAsync(placeholderKeys, template.RootBoType);

                // Update placeholders with validation results
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

                ObjectSpace.CommitChanges();

                var validCount = validationResults.Count(r => r.IsValid);
                var invalidCount = validationResults.Count(r => !r.IsValid);

                if (invalidCount == 0)
                {
                    Application.ShowViewStrategy.ShowMessage(
                        $"All {validCount} placeholders are valid!",
                        InformationType.Success);
                }
                else
                {
                    Application.ShowViewStrategy.ShowMessage(
                        $"{validCount} valid, {invalidCount} invalid placeholders. Check validation errors.",
                        InformationType.Warning);
                }
            }
            catch (Exception ex)
            {
                Application.ShowViewStrategy.ShowMessage(
                    $"Error validating placeholders: {ex.Message}",
                    InformationType.Error);
            }
        }
    }
}
